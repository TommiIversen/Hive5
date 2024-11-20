namespace Engine.Models;

public class GStreamerConfig
{
    private GStreamerConfig()
    {
    } // Privat konstruktor for at forhindre direkte instansiering

    public static GStreamerConfig Instance { get; } = new();

    public string? ExecutablePath { get; set; }
    public string TempPath { get; private set; }

    private void EnsureTempPathExists()
    {
        try
        {
            if (!Directory.Exists(TempPath)) Directory.CreateDirectory(TempPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Kan ikke oprette temp-mappen: {TempPath}", ex);
        }
    }

    public void SetTempPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("TempPath kan ikke være tom.", nameof(path));

        TempPath = path;
        EnsureTempPathExists();
    }
}