namespace Common.DTOs;

public class CommandResult(bool success, string message, object data = null)
{
    public bool Success { get; } = success;
    public string Message { get; } = message;
    public object? Data { get; set; }
    
}