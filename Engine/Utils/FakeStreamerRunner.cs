using System.Runtime.InteropServices;
using Common.Models;
using Engine.Interfaces;

namespace Engine.Utils;

public class FakeStreamerRunner : IStreamerRunner
{
    private readonly Timer _logTimer;
    private readonly Timer _imageTimer;
    private int _logCounter = 0;
    private int _imageCounter = 0;
    private bool _isPauseActive = false;

    public string WorkerId { get; set; }

    private readonly ImageGenerator _generator = new();
    private StreamerState _state = StreamerState.Idle;

    public event EventHandler<LogEntry>? LogGenerated;
    public event EventHandler<ImageData>? ImageGenerated;

    public FakeStreamerRunner()
    {
        _logTimer = new Timer(SendLog, null, Timeout.Infinite, 300);
        _imageTimer = new Timer(SendImage, null, Timeout.Infinite, 1000);
    }


    public async Task<(StreamerState, string)> StartAsync()
    {
        if (_state == StreamerState.Running || _state == StreamerState.Starting)
        {
            return (_state, "Streamer is already running or starting.");
        }

        if (_state == StreamerState.Stopping)
        {
            return (_state, "Streamer is currently stopping. Please wait.");
        }

        _state = StreamerState.Starting;
        Console.WriteLine("Starting streamer...");

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund

        _logTimer.Change(0, 300);
        _imageTimer.Change(0, 1000);
        _state = StreamerState.Running;

        Console.WriteLine("Streamer started.");
        return (_state, "Streamer started successfully.");
    }

    public async Task<(StreamerState, string)> StopAsync()
    {
        switch (_state)
        {
            case StreamerState.Idle or StreamerState.Stopping:
                return (_state, "Streamer is not running or is already stopping.");
            case StreamerState.Starting:
                return (_state, "Streamer is starting. Please wait.");
        }

        _state = StreamerState.Stopping;
        Console.WriteLine("Stopping streamer...");

        await Task.Delay(1000); // Simuleret forsinkelse på 1 sekund

        _logTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _imageTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _state = StreamerState.Idle;

        Console.WriteLine("Streamer stopped.");
        return (_state, "Streamer stopped successfully.");
    }

    private void SendLog(object? state)
    {
        if (_state != StreamerState.Running) return;

        var log = new LogEntry
        {
            WorkerId = WorkerId,
            Timestamp = DateTime.UtcNow,
            Message = $"{_logCounter} Fake log message",
            LogSequenceNumber = _logCounter++
        };
        LogGenerated?.Invoke(this, log);
    }

    private void SendImage(object? state)
    {
        if (_state != StreamerState.Running) return;

        // Check for pause hver 30. billede
        if (_imageCounter % 30 == 0 && !_isPauseActive)
        {
            _isPauseActive = true;
            var imageData = new ImageData
            {
                WorkerId = WorkerId,
                Timestamp = DateTime.UtcNow,
                ImageBytes = GenerateFakeImage("Pauseee")
            };
            ImageGenerated?.Invoke(this, imageData);
            SendLog(null);
            Task.Delay(4000).ContinueWith(_ => { _isPauseActive = false; });
            return; // Skip image generation under pause
        }

        if (!_isPauseActive)
        {
            var imageData = new ImageData
            {
                WorkerId = WorkerId,
                Timestamp = DateTime.UtcNow,
                ImageBytes = GenerateFakeImage(WorkerId)
            };
            ImageGenerated?.Invoke(this, imageData);
        }
    }

    private byte[] GenerateFakeImage(string text = "")
    {
        // Fake image data (placeholder)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return _generator.GenerateImageWithNumber(_imageCounter++, text);
        return new byte[] { 0, 0, 0 };
    }

    public StreamerState GetState()
    {
        return _state;
    }
}