namespace Common.Models;

public class CommandResult(bool success, string message)
{
    public bool Success { get; } = success;
    public string Message { get; } = message;
}
