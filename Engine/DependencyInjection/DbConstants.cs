namespace Engine.DependencyInjection;

public static class DbConstants
{
    public static string GetDatabaseFilePath(string basePath)
    {
        var machineName = Environment.MachineName;
        return Path.Combine(basePath, $"{machineName}.db");
    }
}