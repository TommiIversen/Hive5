using System.Buffers;
using System.Diagnostics;
using System.Text;
using Engine.Models;

namespace Engine.Streamers;

public class GStreamerProcessHandler
{
    private const int MaxBufferSize = 1024 * 1024; // 1 MB

    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    private readonly byte[] _endMarker = { 0xFF, 0xD9 }; // JPEG End
    private readonly Func<byte[], Task> _imageCallback;
    private readonly Func<string, Task> _logCallback;
    private readonly byte[] _startMarker = { 0xFF, 0xD8 }; // JPEG Start
    private byte[] _buffer = [];
    private int _bufferLength;
    private bool _inJpeg;

    public GStreamerProcessHandler(Func<string, Task> logCallback, Func<byte[], Task> imageCallback)
    {
        _logCallback = logCallback;
        _imageCallback = imageCallback;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private Process? GstreamerProcess { get; set; }

    private void OnProcessExit(object sender, EventArgs e)
    {
        StopGStreamerProcess();
    }

    public async Task StartGStreamerProcessAsync(string gstreamerArgs, CancellationToken cancellationToken, bool enableDebug = true)

    {
        if (string.IsNullOrEmpty(GStreamerConfig.Instance.ExecutablePath))
        {
            await _logCallback("Warning: GStreamer executable not found. Cannot start process.");
            return;
        }
        
        await _logCallback($"Starting GStreamer from path: {GStreamerConfig.Instance.ExecutablePath}");

        GstreamerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GStreamerConfig.Instance.ExecutablePath,
                Arguments = gstreamerArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            EnableRaisingEvents = true
        };
        
        if (enableDebug)
        {
            var debugDir = GStreamerDebugHelper.CreateDebugDirectory();
            GstreamerProcess.StartInfo.EnvironmentVariables["GST_DEBUG_DUMP_DOT_DIR"] = debugDir;
        }

        GstreamerProcess.Start();

        _ = Task.Run(() => ReadOutputAsync(GstreamerProcess.StandardOutput, cancellationToken), cancellationToken);
        _ = Task.Run(() => ReadStderrAsync(cancellationToken), cancellationToken);
    }

    // private async Task GetDotAsync()
    // {
    //     if (enableDebug)
    //     {
    //         var debugData = GStreamerDebugHelper.ReadDebugFiles(debugDir);
    //         foreach (var kvp in debugData)
    //         {
    //             await _logCallback($"Debug file {kvp.Key}: {kvp.Value}");
    //         }
    //         await _logCallback($"Debug mode enabled. Dumping DOT files to: {debugDir}");
    //         //GStreamerDebugHelper.CleanupDebugDirectory(debugDir);
    //         await _logCallback("Debug directory and files cleaned up.");
    //     }
    // }
    

    private async Task ReadOutputAsync(StreamReader output, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !output.EndOfStream)
        {
            var line = await output.ReadLineAsync(cancellationToken);
            if (line != null)
                await _logCallback(line);
        }
    }

    private async Task ReadStderrAsync(CancellationToken cancellationToken)
    {
        await using var reader = GstreamerProcess?.StandardError.BaseStream;
        if (reader == null) return;

        var chunkBuffer = _arrayPool.Rent(8192 * 4); // Chunk size

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await reader.ReadAsync(chunkBuffer, 0, chunkBuffer.Length, cancellationToken);
                if (bytesRead == 0) break;

                AppendToBuffer(chunkBuffer.AsSpan(0, bytesRead));
                await ProcessJpegFromBufferAsync();
            }
        }
        finally
        {
            _arrayPool.Return(chunkBuffer);
        }
    }

    private void AppendToBuffer(ReadOnlySpan<byte> data)
    {
        if (_bufferLength + data.Length > MaxBufferSize)
        {
            _logCallback("Buffer size exceeded, trimming buffer.");
            var excessLength = _bufferLength + data.Length - MaxBufferSize;
            var trimmedBuffer = _arrayPool.Rent(MaxBufferSize);
            _buffer.AsSpan(excessLength, _bufferLength - excessLength).CopyTo(trimmedBuffer);
            _arrayPool.Return(_buffer);
            _buffer = trimmedBuffer;
            _bufferLength -= excessLength;
        }

        if (_buffer.Length < _bufferLength + data.Length)
        {
            var newBuffer = _arrayPool.Rent(_bufferLength + data.Length);
            _buffer.AsSpan(0, _bufferLength).CopyTo(newBuffer);
            _arrayPool.Return(_buffer);
            _buffer = newBuffer;
        }

        data.CopyTo(_buffer.AsSpan(_bufferLength));
        _bufferLength += data.Length;
    }

    private static int IndexOfSequence(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> sequence, int start = 0)
    {
        for (var i = start; i <= buffer.Length - sequence.Length; i++)
            if (buffer.Slice(i, sequence.Length).SequenceEqual(sequence))
                return i;
        return -1;
    }

    private async Task ProcessJpegFromBufferAsync()
    {
        var startMarkerIndex = _inJpeg ? 0 : IndexOfSequence(_buffer.AsSpan(0, _bufferLength), _startMarker);

        while (startMarkerIndex != -1 || _inJpeg)
        {
            var endMarkerIndex = IndexOfSequence(_buffer.AsSpan(0, _bufferLength), _endMarker, startMarkerIndex);

            if (_inJpeg)
            {
                if (endMarkerIndex == -1) return; // Wait for more data

                var jpegImage = _buffer.AsSpan(0, endMarkerIndex + _endMarker.Length).ToArray();
                _inJpeg = false;
                TrimBuffer(endMarkerIndex + _endMarker.Length);
                await _imageCallback(jpegImage);
            }
            else
            {
                if (startMarkerIndex == -1) return; // No start marker found
                endMarkerIndex = IndexOfSequence(_buffer.AsSpan(0, _bufferLength), _endMarker, startMarkerIndex);

                if (endMarkerIndex == -1)
                {
                    _inJpeg = true; // Start of JPEG, wait for more data
                    return;
                }

                if (startMarkerIndex > 0)
                {
                    var textData = _buffer.AsSpan(0, startMarkerIndex).ToArray();
                    await _logCallback($"Non-JPEG data received: {Encoding.UTF8.GetString(textData)}");
                }

                var jpegImage = _buffer.AsSpan(startMarkerIndex, endMarkerIndex + _endMarker.Length - startMarkerIndex)
                    .ToArray();
                TrimBuffer(endMarkerIndex + _endMarker.Length);
                await _imageCallback(jpegImage);
            }

            startMarkerIndex = IndexOfSequence(_buffer.AsSpan(0, _bufferLength), _startMarker);
        }

        if (!_inJpeg && _bufferLength > 0)
        {
            var remainingData = _buffer.AsSpan(0, _bufferLength).ToArray();
            await _logCallback($"Remaining non-JPEG data: {Encoding.UTF8.GetString(remainingData)}");
            _bufferLength = 0;
        }
    }

    private void TrimBuffer(int startIndex)
    {
        var remainingLength = _bufferLength - startIndex;
        if (remainingLength > 0) _buffer.AsSpan(startIndex, remainingLength).CopyTo(_buffer.AsSpan(0, remainingLength));
        _bufferLength = remainingLength;
    }

    public void StopGStreamerProcess()
    {
        if (GstreamerProcess?.HasExited ?? true) return;

        GstreamerProcess.Kill();
        GstreamerProcess.WaitForExit();
        GstreamerProcess.Dispose();
    }
}

public static class GStreamerDebugHelper
{
    public static string CreateDebugDirectory()
    {
        var debugDir = Path.Combine(GStreamerConfig.Instance.TempPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(debugDir);
        return debugDir;
    }

    public static Dictionary<string, string?> ReadDebugFiles(string debugDir)
    {
        var result = new Dictionary<string, string?>
        {
            ["NULL_READY"] = null,
            ["READY_PAUSED"] = null,
            ["PAUSED_PLAYING"] = null
        };

        foreach (var filePath in Directory.EnumerateFiles(debugDir, "*.dot"))
        {
            var fileName = Path.GetFileName(filePath);
            foreach (var key in result.Keys.ToList())
            {
                if (fileName.Contains(key))
                {
                    result[key] = File.ReadAllText(filePath);
                    break;
                }
            }
        }

        return result;
    }

    public static void CleanupDebugDirectory(string debugDir)
    {
        if (Directory.Exists(debugDir))
        {
            Directory.Delete(debugDir, true);
        }
    }
}
