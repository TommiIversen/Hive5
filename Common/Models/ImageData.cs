// Models/ImageData.cs
namespace Common.Models;

public class ImageData: BaseMessage
{
    public required byte[] ImageBytes { get; set; }
    public required Guid WorkerId { get; set; }
}