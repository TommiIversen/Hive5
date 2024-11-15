namespace Engine.Models;

public class GStreamerConfig
{
    private GStreamerConfig() { } // Privat konstruktor for at forhindre direkte instansiering

    public static GStreamerConfig Instance { get; } = new GStreamerConfig();

    public string? ExecutablePath { get; set; }
}