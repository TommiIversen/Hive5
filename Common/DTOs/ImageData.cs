namespace Common.DTOs;

public class ImageData : BaseMessage
{
    public required byte[] ImageBytes { get; set; }
    public required string WorkerId { get; set; }
    public int ImageSequenceNumber { get; set; }
}