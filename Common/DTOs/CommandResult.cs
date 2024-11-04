// Ikke-generisk version uden Data

public class CommandResult
{
    public CommandResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public bool Success { get; }
    public string Message { get; }
}

// Generisk version med Data
public class CommandResult<T> : CommandResult
{
    public CommandResult(bool success, string message, T? data = default)
        : base(success, message)
    {
        Data = data;
    }

    public T? Data { get; }
}