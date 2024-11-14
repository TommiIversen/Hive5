using System.Runtime.InteropServices;
using System.Threading.Channels;
using Common.DTOs.Enums;
using Common.DTOs.Events;
using Engine.Attributes;
using Engine.Interfaces;
using Engine.Utils;

namespace Engine.Streamers;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using System.IO;


public class GStreamerProcessHandler
{
    private Process GstreamerProcess { get; set; }
    
    
    private readonly Channel<byte[]> _jpegChannel = Channel.CreateUnbounded<byte[]>();
    private readonly int _maxBufferSize = 1024 * 1024; // 1 MB
    private readonly byte[] _startMarker = { 0xFF, 0xD8 };
    private readonly byte[] _endMarker = { 0xFF, 0xD9 };
    
    private readonly Func<string, Task> _logCallback;
    private readonly Func<byte[], Task> _imageCallback;
    
    private byte[] _buffer = Array.Empty<byte>();
    private bool _inJpeg = false;

    
    public GStreamerProcessHandler(Func<string, Task> logCallback, Func<byte[], Task> imageCallback)
    {
        _logCallback = logCallback;
        _imageCallback = imageCallback;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        // Forsøg at stoppe GStreamer-processen ved applikationens afslutning
        StopGStreamerProcess();
    }

    private static string FindGStreamerExecutable(string executableName = "gst-launch-1.0.exe")
    {
        string? pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (pathVariable == null)
        {
            throw new InvalidOperationException("PATH-miljøvariablen er ikke sat.");
        }

        string[] paths = pathVariable.Split(Path.PathSeparator);

        foreach (var path in paths)
        {
            string fullPath = Path.Combine(path, executableName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new FileNotFoundException($"GStreamer-eksekverbar '{executableName}' blev ikke fundet i PATH.");
    }

    public async Task StartGStreamerProcessAsync(string gstreamerArgs, CancellationToken cancellationToken)
    {
        string gstreamerPath = FindGStreamerExecutable();
        Console.WriteLine($"Starter GStreamer fra sti: {gstreamerPath}");
        Console.WriteLine($"Med argumenter: {gstreamerArgs}");

        GstreamerProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = gstreamerPath,
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
        //_gstreamerProcess.StartInfo.Arguments = $"--mytag=GStreamerProcess {gstreamerArgs}";

        GstreamerProcess.Start();

        // Start asynkrone opgaver for at læse stdout og stderr
        _ = Task.Run(() => ReadOutputAsync(GstreamerProcess.StandardOutput, cancellationToken));
        _ = Task.Run(() => ReadStderrAsync(cancellationToken), cancellationToken);
    }

    private async Task ReadOutputAsync(StreamReader output, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !output.EndOfStream)
        {
            var line = await output.ReadLineAsync();
            if (line != null)
            {
                await _logCallback(line);

            }
        }
    }

    private void AppendToBuffer(ReadOnlySpan<byte> data)
    {
        if (_buffer.Length + data.Length > _maxBufferSize)
        {
            _logCallback("Buffer size exceeded, trimming buffer.");
            _buffer = _buffer[^_maxBufferSize..];
        }
        
        byte[] newBuffer = new byte[_buffer.Length + data.Length];
        _buffer.CopyTo(newBuffer, 0);
        data.CopyTo(newBuffer.AsSpan(_buffer.Length));
        _buffer = newBuffer;
    }
    
    
    private int IndexOfSequence(byte[] buffer, byte[] sequence, int start = 0)
    {
        for (int i = start; i <= buffer.Length - sequence.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (buffer[i + j] != sequence[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }
        return -1;
    }
    
    
    private async Task ReadStderrAsync(CancellationToken cancellationToken)
    {
        await using var reader = GstreamerProcess.StandardError.BaseStream;
        var buffer = new byte[8192 * 4]; // Chunk size

        while (!cancellationToken.IsCancellationRequested)
        {
            int bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == 0) break;

            AppendToBuffer(buffer.AsSpan(0, bytesRead));
            await ProcessJpegFromBuffer();
        }
    }
    
private async Task ProcessJpegFromBuffer()
{
    int startMarkerIndex = _inJpeg ? 0 : IndexOfSequence(_buffer, _startMarker);

    while (startMarkerIndex != -1 || _inJpeg)
    {
        int endMarkerIndex = IndexOfSequence(_buffer, _endMarker, startMarkerIndex);

        if (_inJpeg)
        {
            // Vi er i en JPEG, så vi leder efter endemarkøren
            if (endMarkerIndex == -1)
            {
                // Hvis slutmarkøren ikke findes, vent på mere data
                return;
            }

            // Hvis slutmarkøren er fundet, afslut JPEG-billedet
            byte[] jpegImage = _buffer[..(endMarkerIndex + _endMarker.Length)];
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
            {
                // Hvis der ikke findes en startmarkør, vent på mere data
                return;
            }

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
                byte[] textData = _buffer[..startMarkerIndex];
                await _logCallback($"Non-JPEG data received: {System.Text.Encoding.UTF8.GetString(textData)}");
            }

            // Udtræk det komplette JPEG-billede
            byte[] jpegImage = _buffer[startMarkerIndex..(endMarkerIndex + _endMarker.Length)];
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
        await _logCallback($"Remaining non-JPEG data: {System.Text.Encoding.UTF8.GetString(_buffer)}");
        _buffer = Array.Empty<byte>();
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


[FriendlyName("GstStreamer")]
public class GstStreamerService : IStreamerService
{
    private bool _isPauseActive;
    private WorkerState _state = WorkerState.Idle;
    readonly GStreamerProcessHandler _handler;

    public GstStreamerService()
    {
        _handler = new GStreamerProcessHandler(LogCallbackAsync, ImageCallbackAsync);
    }

    public required string WorkerId { get; set; }
    public required string GstCommand { get; set; }

    public event EventHandler<WorkerLogEntry>? LogGenerated;
    public event EventHandler<WorkerImageData>? ImageGenerated;
    public Func<WorkerState, Task>? StateChangedAsync { get; set; }

    
    private async Task LogCallbackAsync(string message)
    {
        Console.WriteLine($"----------Log: {message}");
        CreateAndSendLog(message);
    }

    private async Task ImageCallbackAsync(byte[] imageData)
    {
        Console.WriteLine($"-----------Billede modtaget på {DateTime.Now}");
        var gstImageData = new WorkerImageData
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            ImageBytes = imageData
        };
        ImageGenerated?.Invoke(this, gstImageData);
    }

    public async Task<(WorkerState, string)> StartAsync()
    {
        string msg;

        if (_state == WorkerState.Running || _state == WorkerState.Starting)
        {
            msg = "Streamer is already running or starting.";
            CreateAndSendLog(msg);
            return (_state, msg);
        }

        if (_state == WorkerState.Stopping)
        {
            msg = "Streamer is currently stopping. Please wait.";
            SendLog(msg);
            return (_state, msg);
        }

        _state = WorkerState.Starting;
        await OnStateChangedAsync(_state); // Trigger state change

        msg = $"Starting streamer... with command: {GstCommand}";
        SendLog(msg);

        await _handler.StartGStreamerProcessAsync(GstCommand, CancellationToken.None);

        _state = WorkerState.Running;
        await OnStateChangedAsync(_state);

        msg = "Streamer started successfully.";
        SendLog(msg);
        return (_state, msg);
    }

    public async Task<(WorkerState, string)> StopAsync()
    {
        switch (_state)
        {
            case WorkerState.Idle or WorkerState.Stopping:
                return (_state, "Streamer is not running or is already stopping.");
            case WorkerState.Starting:
                return (_state, "Streamer is starting. Please wait.");
        }

        
        CreateAndSendLog("Streamer stopping", LogLevel.Critical);
        _state = WorkerState.Stopping;
        await OnStateChangedAsync(_state); // Trigger state change
        _handler.StopGStreamerProcess();

        Console.WriteLine("Stopping streamer...");

        _state = WorkerState.Idle;
        await OnStateChangedAsync(_state); // Trigger state change

        Console.WriteLine("Streamer stopped.");
        return (_state, "Streamer stopped successfully.");
    }

    public WorkerState GetState()
    {
        return _state;
    }


    private void SendLog(string logMsg)
    {
        CreateAndSendLog(logMsg);
    }


    private void CreateAndSendLog(string message, LogLevel logLevel = LogLevel.Information)
    {
        var log = new WorkerLogEntry
        {
            WorkerId = WorkerId,
            LogTimestamp = DateTime.UtcNow,
            Message = message,
            LogLevel = logLevel
        };
        LogGenerated?.Invoke(this, log);
    }


    private async Task OnStateChangedAsync(WorkerState newState)
    {
        if (StateChangedAsync != null) await StateChangedAsync.Invoke(newState);
    }
}