namespace Engine.Services;

public interface IEngineIdProvider
{
    Guid GetEngineId();
}

public class EngineIdProvider : IEngineIdProvider
{
    private readonly Guid _engineId;

    public EngineIdProvider(Guid engineId)
    {
        _engineId = engineId;
    }

    public Guid GetEngineId() => _engineId;
}