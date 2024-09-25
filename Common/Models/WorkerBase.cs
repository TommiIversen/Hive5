using System.Diagnostics.CodeAnalysis;

namespace Common.Models;

public class WorkerOut: BaseMessage
{
    public required Guid WorkerId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Command { get; set; }
    public bool Enabled { get; set; }
    public bool IsRunning { get; set; }
}

public class WorkerCreate
{
    public WorkerCreate()
    {
    }

    [SetsRequiredMembers]
    public WorkerCreate(string name, string description, string command)
    {
        Name = name;
        Description = description;
        Command = command;
    }

    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Command { get; set; }
}
