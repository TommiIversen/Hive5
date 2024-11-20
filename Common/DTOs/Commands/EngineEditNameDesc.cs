namespace Common.DTOs.Commands;

public class EngineEditNameDesc : WorkerOperationMessage
{
    public required string EngineName { get; init; }
    public required string EngineDescription { get; init; }
}