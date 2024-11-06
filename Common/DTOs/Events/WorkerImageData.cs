namespace Common.DTOs.Events;

public class WorkerImageData : BaseMessage
{
    public required byte[] ImageBytes { get; init; }
    public required string WorkerId { get; init; }
    public int ImageSequenceNumber { get; set; }
}