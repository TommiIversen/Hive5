namespace Common.DTOs.Events;

public class ImageData : BaseMessage
{
    public required byte[] ImageBytes { get; init; }
    public required string WorkerId { get; init; }
    public int ImageSequenceNumber { get; set; }
}