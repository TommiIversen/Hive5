namespace Common.DTOs.Commands;

// Ikke-generisk version uden Data
public class CommandResult(bool success, string message)
{
    public bool Success { get; } = success;
    public string Message { get; } = message;
}

// Generisk version med Data
public class CommandResult<T>(bool success, string message, T? data = default) : CommandResult(success, message)
{
    public T? Data { get; } = data;
}