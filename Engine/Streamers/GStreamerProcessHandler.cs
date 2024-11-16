using System.Diagnostics;
using System.Text;
using Engine.Models;
using System.Buffers;


namespace Engine.Streamers;

public class GStreamerProcessHandler
{
    private readonly Func<byte[], Task> _imageCallback;
    private readonly Func<string, Task> _logCallback;
    private const int MaxBufferSize = 1024 * 1024; // 1 MB
    private readonly byte[] _startMarker = { 0xFF, 0xD8 }; // JPEG Start
    private readonly byte[] _endMarker = { 0xFF, 0xD9 };   // JPEG End

    private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;
    private byte[] _buffer = [];
    private int _bufferLength = 0;
    private bool _inJpeg = false;

    private Process? GstreamerProcess { get; set; }

    public GStreamerProcessHandler(Func<string, Task> logCallback, Func<byte[], Task> imageCallback)
    {
        _logCallback = logCallback;
        _imageCallback = imageCallback;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        StopGStreamerProcess();
    }

    public async Task StartGStreamerProcessAsync(string gstreamerArgs, CancellationToken cancellationToken)
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
        GstreamerProcess.StartInfo.EnvironmentVariables["GST_DEBUG_DUMP_DOT_DIR"] = @"C:\temp\"; // Ændre til ønsket sti

        GstreamerProcess.Start();

        _ = Task.Run(() => ReadOutputAsync(GstreamerProcess.StandardOutput, cancellationToken), cancellationToken);
        _ = Task.Run(() => ReadStderrAsync(cancellationToken), cancellationToken);
    }

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
        for (int i = start; i <= buffer.Length - sequence.Length; i++)
        {
            if (buffer.Slice(i, sequence.Length).SequenceEqual(sequence))
                return i;
        }
        return -1;
    }

    private async Task ProcessJpegFromBufferAsync()
    {
        int startMarkerIndex = _inJpeg ? 0 : IndexOfSequence(_buffer.AsSpan(0, _bufferLength), _startMarker);

        while (startMarkerIndex != -1 || _inJpeg)
        {
            int endMarkerIndex = IndexOfSequence(_buffer.AsSpan(0, _bufferLength), _endMarker, startMarkerIndex);

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

                var jpegImage = _buffer.AsSpan(startMarkerIndex, endMarkerIndex + _endMarker.Length - startMarkerIndex).ToArray();
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
        int remainingLength = _bufferLength - startIndex;
        if (remainingLength > 0)
        {
            _buffer.AsSpan(startIndex, remainingLength).CopyTo(_buffer.AsSpan(0, remainingLength));
        }
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


public class GStreamerProcessHandlerold
{
    private readonly byte[] _endMarker = [0xFF, 0xD9];
    private readonly Func<byte[], Task> _imageCallback;
    
    private readonly Func<string, Task> _logCallback;
    private const int MaxBufferSize = 1024 * 1024; // 1 MB
    private readonly byte[] _startMarker = [0xFF, 0xD8];

    private byte[] _buffer = [];
    private bool _inJpeg;


    public GStreamerProcessHandlerold(Func<string, Task> logCallback, Func<byte[], Task> imageCallback)
    {
        _logCallback = logCallback;
        _imageCallback = imageCallback;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private Process GstreamerProcess { get; set; }

    private void OnProcessExit(object sender, EventArgs e)
    {
        // Forsøg at stoppe GStreamer-processen ved applikationens afslutning
        StopGStreamerProcess();
    }
    
    public async Task StartGStreamerProcessAsync(string gstreamerArgs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(GStreamerConfig.Instance.ExecutablePath))
        {
            await _logCallback("Advarsel: GStreamer-eksekverbar ikke fundet. Kan ikke starte processen.");
            return;
        }
        await _logCallback($"Starter GStreamer fra sti: {GStreamerConfig.Instance.ExecutablePath}");

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
        GstreamerProcess.StartInfo.EnvironmentVariables["MY_APP_TAG"] = "GStreamerProcess";

        GstreamerProcess.Start();

        // Start asynkrone opgaver for at læse stdout og stderr
        _ = Task.Run(() => ReadOutputAsync(GstreamerProcess.StandardOutput, cancellationToken), cancellationToken);
        _ = Task.Run(() => ReadStderrAsync(cancellationToken), cancellationToken);
    }

    private async Task ReadOutputAsync(StreamReader output, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !output.EndOfStream)
        {
            var line = await output.ReadLineAsync();
            if (line != null) await _logCallback(line);
        }
    }

    private void AppendToBuffer(ReadOnlySpan<byte> data)
    {
        if (_buffer.Length + data.Length > MaxBufferSize)
        {
            _logCallback("Buffer size exceeded, trimming buffer.");
            _buffer = _buffer[^MaxBufferSize..];
        }

        var newBuffer = new byte[_buffer.Length + data.Length];
        _buffer.CopyTo(newBuffer, 0);
        data.CopyTo(newBuffer.AsSpan(_buffer.Length));
        _buffer = newBuffer;
    }


    private static int IndexOfSequence(byte[] buffer, byte[] sequence, int start = 0)
    {
        for (var i = start; i <= buffer.Length - sequence.Length; i++)
        {
            var match = true;
            for (var j = 0; j < sequence.Length; j++)
                if (buffer[i + j] != sequence[j])
                {
                    match = false;
                    break;
                }

            if (match) return i;
        }

        return -1;
    }


    private async Task ReadStderrAsync(CancellationToken cancellationToken)
    {
        await using var reader = GstreamerProcess.StandardError.BaseStream;
        var buffer = new byte[8192 * 4]; // Chunk size

        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == 0) break;

            AppendToBuffer(buffer.AsSpan(0, bytesRead));
            await ProcessJpegFromBuffer();
        }
    }

    private async Task ProcessJpegFromBuffer()
    {
        var startMarkerIndex = _inJpeg ? 0 : IndexOfSequence(_buffer, _startMarker);

        while (startMarkerIndex != -1 || _inJpeg)
        {
            var endMarkerIndex = IndexOfSequence(_buffer, _endMarker, startMarkerIndex);

            if (_inJpeg)
            {
                // Vi er i en JPEG, så vi leder efter endemarkøren
                if (endMarkerIndex == -1)
                    // Hvis slutmarkøren ikke findes, vent på mere data
                    return;

                // Hvis slutmarkøren er fundet, afslut JPEG-billedet
                var jpegImage = _buffer[..(endMarkerIndex + _endMarker.Length)];
                _inJpeg = false;
                _buffer = _buffer[(endMarkerIndex + _endMarker.Length)..]; // Fjern behandlet billede fra buffer

                // Kald callback med det komplette JPEG-billede
                await _imageCallback(jpegImage);
            }
            else
            {
                // Leder efter en startmarkør, hvis vi ikke er i en JPEG
                startMarkerIndex = IndexOfSequence(_buffer, _startMarker);
                if (startMarkerIndex == -1)
                    // Hvis der ikke findes en startmarkør, vent på mere data
                    return;

                // Leder efter slutmarkøren fra startmarkørens position
                endMarkerIndex = IndexOfSequence(_buffer, _endMarker, startMarkerIndex);
                if (endMarkerIndex == -1)
                {
                    // Start af et JPEG-billede uden slutmarkør, venter på mere data
                    _inJpeg = true;
                    return;
                }

                // Hvis der er tekstdata før JPEG-billedet, processér det
                if (startMarkerIndex > 0)
                {
                    var textData = _buffer[..startMarkerIndex];
                    await _logCallback($"Non-JPEG data received: {Encoding.UTF8.GetString(textData)}");
                }

                // Udtræk det komplette JPEG-billede
                var jpegImage = _buffer[startMarkerIndex..(endMarkerIndex + _endMarker.Length)];
                _buffer = _buffer[(endMarkerIndex + _endMarker.Length)..]; // Fjern behandlet billede fra buffer

                // Kald callback med det komplette JPEG-billede
                await _imageCallback(jpegImage);
            }

            // Opdater startmarkøren for at finde næste JPEG i buffer
            startMarkerIndex = IndexOfSequence(_buffer, _startMarker);
        }

        // Hvis buffer ikke indeholder en JPEG, logges det som tekstdata
        if (!_inJpeg && _buffer.Length > 0)
        {
            await _logCallback($"Remaining non-JPEG data: {Encoding.UTF8.GetString(_buffer)}");
            _buffer = [];
        }
    }


    public void StopGStreamerProcess()
    {
        if (GstreamerProcess.HasExited) return;
        GstreamerProcess.Kill();
        GstreamerProcess.WaitForExit();
        GstreamerProcess.Dispose();
    }
}