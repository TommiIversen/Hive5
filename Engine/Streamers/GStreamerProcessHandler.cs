using System.Diagnostics;
using System.Text;
using Engine.Models;

namespace Engine.Streamers;

public class GStreamerProcessHandler
{
    private readonly byte[] _endMarker = [0xFF, 0xD9];
    private readonly Func<byte[], Task> _imageCallback;
    
    private readonly Func<string, Task> _logCallback;
    private const int MaxBufferSize = 1024 * 1024; // 1 MB
    private readonly byte[] _startMarker = [0xFF, 0xD8];

    private byte[] _buffer = [];
    private bool _inJpeg;


    public GStreamerProcessHandler(Func<string, Task> logCallback, Func<byte[], Task> imageCallback)
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