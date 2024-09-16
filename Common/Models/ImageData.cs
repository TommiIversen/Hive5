// Models/ImageData.cs
namespace Common.Models;

public class ImageData
{
    public Guid EngineId { get; set; }
    public required Guid WorkerId { get; set; }
    public required DateTime Timestamp { get; set; }
    public required byte[] ImageBytes { get; set; }
}