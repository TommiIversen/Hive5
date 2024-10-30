using Engine.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Common.DTOs;

namespace Engine.Hubs
{
    public interface IHubWorkerEventHandlers
    {
        void AttachWorkerHandlers(HubConnection hubConnection);
    }

    public class HubWorkerWorkerEventHandlers : IHubWorkerEventHandlers
    {
        private readonly IWorkerManager _workerManager;
        private readonly ILogger<HubWorkerWorkerEventHandlers> _logger;

        public HubWorkerWorkerEventHandlers(IWorkerManager workerManager, ILogger<HubWorkerWorkerEventHandlers> logger)
        {
            _workerManager = workerManager;
            _logger = logger;
        }

        public void AttachWorkerHandlers(HubConnection hubConnection)
        {
            // Handle StopWorker command asynchronously
            hubConnection.On("StopWorker", async (WorkerOperationMessage message) =>
            {
                var commandResult = await _workerManager.StopWorkerAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("StartWorker", async (WorkerOperationMessage message) =>
            {
                var commandResult = await _workerManager.StartWorkerAsync(message.WorkerId);
                return commandResult;
            });

            // Handle RemoveWorker command asynchronously
            hubConnection.On("RemoveWorker", async (WorkerOperationMessage message) =>
            {
                var commandResult = await _workerManager.RemoveWorkerAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("ResetWatchdogEventCount", async (WorkerOperationMessage message) =>
            {
                var commandResult = await _workerManager.ResetWatchdogEventCountAsync(message.WorkerId);
                return commandResult;
            });

            hubConnection.On("EnableDisableWorker", async (WorkerEnableDisableMessage message) =>
            {
                var commandResult = await _workerManager.EnableDisableWorkerAsync(message.WorkerId, message.Enable);
                return commandResult;
            });

            hubConnection.On("EditWorker", async (WorkerCreate workerEdit) =>
            {
                var commandResult = await _workerManager.EditWorkerAsync(workerEdit.WorkerId, workerEdit.Name,
                    workerEdit.Description, workerEdit.Command);
                return commandResult;
            });

            hubConnection.On("CreateWorker", async (WorkerCreate workerCreate) =>
            {
                var workerService = await _workerManager.AddWorkerAsync(workerCreate.EngineId, workerCreate);
                if (workerService == null)
                {
                    return new CommandResult(false, $"Worker with ID {workerCreate.WorkerId} already exists.");
                }
                var result = await _workerManager.StartWorkerAsync(workerService.WorkerId);
                return result;
            });
        }
    }
}
