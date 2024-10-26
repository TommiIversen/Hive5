namespace Engine.Services;

public interface IEngineIdProvider
{
    Guid GetEngineId();
}

public class EngineIdProvider(Guid engineId) : IEngineIdProvider
{
    public Guid GetEngineId() => engineId;
}