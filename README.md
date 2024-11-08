# Hive5
Set data path via env: HIVE_BASE_PATH


# List pakker
dotnet list package

# EF core
setx PATH "%PATH%;%USERPROFILE%\.dotnet\tools"


dotnet tool install --global dotnet-ef

dotnet ef migrations add workerChangeLog

Debug msgpack:

try
{
    var data = MessagePackSerializer.Serialize(result);
    var deserialized = MessagePackSerializer.Deserialize<CommandResult<string>>(data);
}
catch (Exception ex)
{
    Console.WriteLine($"¤¤¤¤¤¤¤¤¤¤ Serialization/Deserialization Error: {ex.Message}");
}


.ConfigureLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
})