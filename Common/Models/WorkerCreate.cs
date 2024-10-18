using System.Diagnostics.CodeAnalysis;

namespace Common.Models;

public class WorkerCreate: BaseMessage
{
    [SetsRequiredMembers]
    public WorkerCreate(string workerId, string name, string description, string command)
    {
        WorkerId = workerId;
        Name = name;
        Description = description;
        Command = command;
    }

    public required string WorkerId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Command { get; set; }
}