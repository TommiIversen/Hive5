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

            hubConnection.On("EditWorker", async (WorkerCreateAndEdit workerEdit) =>
            {
                var commandResult = await _workerManager.EditWorkerAsync(workerEdit);
                return commandResult;
            });
            

            hubConnection.On("CreateWorker", async (WorkerCreateAndEdit workerCreate) =>
            {
                var workerService = await _workerManager.AddWorkerAsync(workerCreate.EngineId, workerCreate);
                if (workerService == null)
                {
                    return new CommandResult(false, $"Worker with ID {workerCreate.WorkerId} already exists.");
                }
                var result = await _workerManager.StartWorkerAsync(workerService.WorkerId);
                return result;
            });
            
            hubConnection.On("GetWorkerEventsWithLogs", async (WorkerOperationMessage message) =>
            {
                try
                {
                    var eventsWithLogs = await _workerManager.GetWorkerEventsWithLogsAsync(message.WorkerId);

                    foreach (var workerEventWithLogsDto in eventsWithLogs.Events)
                    {
                        Console.WriteLine($"Event: {workerEventWithLogsDto.EventMessage}");
                        foreach (var log in workerEventWithLogsDto.Logs)
                        {
                            Console.WriteLine($"Log: {log.Message}");
                        }
                    }
        
                    // Hvis du ønsker at returnere noget til klienten, brug en separat send eller invoke
                    //await hubConnection.InvokeAsync("ReturnWorkerEventsWithLogs", new CommandResult<WorkerEventWithLogsDto>(true, "Success", eventsWithLogs));
                    return new CommandResult<WorkerEventWithLogsDto>(true, $"OK.", eventsWithLogs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching events with logs for worker {message.WorkerId}: {ex.Message}");
                    return new CommandResult<WorkerEventWithLogsDto>(false, $"Error: {ex.Message}", null);
                }
            });
            
            // hubConnection.On("GetWorkerEventsWithLogs", async (WorkerOperationMessage message) =>
            // {
            //     try
            //     {
            //         var eventsWithLogs = await _workerManager.GetWorkerEventsWithLogsAsync(message.WorkerId);
            //         
            //         // console
            //         
            //         foreach (var workerEventWithLogsDto in eventsWithLogs.Events)
            //         {
            //             Console.WriteLine($"åååååååååååååååEvent: {workerEventWithLogsDto.EventMessage}");
            //             foreach (var log in workerEventWithLogsDto.Logs)
            //             {
            //                 Console.WriteLine($"Log: {log.Message}");
            //             }
            //         }
            //         //await hubConnection.InvokeAsync("ReturnWorkerEventsWithLogs", eventsWithLogs);
            //
            //         return new CommandResult(true, "Worker events and logs retrieved successfully.");
            //     }
            //     catch (Exception ex)
            //     {
            //         _logger.LogError(ex, $"Error fetching events with logs for worker {message.WorkerId} {ex}");
            //         return new CommandResult(false, $"Failed to retrieve worker events and logs {ex}.", null);
            //     }
            // });
            
        }
    }
}
