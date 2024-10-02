using System.Diagnostics.CodeAnalysis;

namespace Common.Models;

public class WorkerCreate
{
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