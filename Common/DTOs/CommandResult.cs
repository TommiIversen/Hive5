// Ikke-generisk version uden Data
public class CommandResult
{
    public bool Success { get; }
    public string Message { get; }

    public CommandResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}

// Generisk version med Data
public class CommandResult<T> : CommandResult
{
    public T? Data { get; }

    public CommandResult(bool success, string message, T? data = default) 
        : base(success, message)
    {
        Data = data;
    }
}