// Models/ImageData.cs
namespace StreamHub.Models;

public class ImageData
{
    public Guid EngineId { get; set; }
    public Guid WorkerId { get; set; }
    public DateTime Timestamp { get; set; }
    public byte[] ImageBytes { get; set; }
}