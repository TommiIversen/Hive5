namespace Common.Models;

public class WorkerOperationMessage : BaseMessage
{
    public string WorkerId { get; set; }
    public WorkerOperationMessage(Guid engineId, string workerId)
    {
        EngineId = engineId;
        WorkerId = workerId;
    }
}