using Engine.DAL.Repositories;
using Engine.Interfaces;
using Engine.Services;

public interface IWorkerServiceFactory
{
    IWorkerService CreateWorkerService(string workerId, IStreamerService streamerService, WorkerConfiguration config);
}

public class WorkerServiceFactory : IWorkerServiceFactory
{
    private readonly ILoggerService _loggerService;
    private readonly IMessageQueue _messageQueue;
    private readonly IRepositoryFactory _repositoryFactory;
    private readonly IStreamerWatchdogFactory _watchdogFactory;

    public WorkerServiceFactory(
        ILoggerService loggerService,
        IMessageQueue messageQueue,
        IRepositoryFactory repositoryFactory,
        IStreamerWatchdogFactory watchdogFactory)
    {
        _loggerService = loggerService;
        _messageQueue = messageQueue;
        _repositoryFactory = repositoryFactory;
        _watchdogFactory = watchdogFactory;
    }

    public IWorkerService CreateWorkerService(string workerId, IStreamerService streamerService,
        WorkerConfiguration config)
    {
        return new WorkerService(
            _loggerService,
            _messageQueue,
            streamerService,
            _repositoryFactory,
            _watchdogFactory,
            config);
    }
}