using Xunit;
using Moq;
using System.Threading.Tasks;
using Common.DTOs;
using Engine.Hubs;
using Microsoft.AspNetCore.SignalR;

public class MessageRouterTests
{
    [Fact]
    public async Task RouteMessageToClientAsync_Should_Invoke_ReceiveMetric_For_Metric_Message()
    {
        // Arrange
        var mockHubClient = new Mock<IHubClient>();
        var metricMessage = new Metric
        {
            CPUUsage = 0,
            PerCoreCpuUsage = null,
            CurrentProcessCpuUsage = 0,
            MemoryUsage = 0,
            TotalMemory = 0,
            AvailableMemory = 0,
            CurrentProcessMemoryUsage = 0,
            RxMbps = 0,
            TxMbps = 0,
            RxUsagePercent = 0,
            TxUsagePercent = 0,
            NetworkInterfaceName = null,
            LinkSpeedGbps = 0,
            MeasureTimestamp = default
        };

        // Act
        await MessageRouter.RouteMessageToClientAsync(mockHubClient.Object, metricMessage);

        // Assert
        mockHubClient.Verify(client => client.InvokeAsync("ReceiveMetric", metricMessage), Times.Once);
    }

    [Fact]
    public async Task RouteMessageToClientAsync_Should_Invoke_ReceiveWorkerLog_For_WorkerLogEntry_Message()
    {
        // Arrange
        var mockHubClient = new Mock<IHubClient>();
        var workerLog = new WorkerLogEntry
        {
            WorkerId = null,
            Message = null
        };

        // Act
        await MessageRouter.RouteMessageToClientAsync(mockHubClient.Object, workerLog);

        // Assert
        mockHubClient.Verify(client => client.InvokeAsync("ReceiveWorkerLog", workerLog), Times.Once);
    }

    [Fact]
    public async Task RouteMessageToClientAsync_Should_Invoke_HandleUnknownMessage_For_Unknown_Message_Type()
    {
        // Arrange
        var mockHubClient = new Mock<IHubClient>();
        var unknownMessage = new BaseMessage { /* initialiser nødvendige properties her */ };

        // Act
        await MessageRouter.RouteMessageToClientAsync(mockHubClient.Object, unknownMessage);

        // Assert
        mockHubClient.Verify(client => client.InvokeAsync("ReceiveDeadLetter", 
            $"Unknown message type received: {unknownMessage.GetType().Name}, WorkerId: {unknownMessage.EngineId}"), Times.Once);
    }
    
    [Fact]
    public async Task RouteMessageToClientAsync_Should_Call_HandleUnknownMessage_When_HubException_MethodNotFound()
    {
        // Arrange
        var mockHubClient = new Mock<IHubClient>();
        var metricMessage = new Metric
        {
            CPUUsage = 0,
            PerCoreCpuUsage = null,
            CurrentProcessCpuUsage = 0,
            MemoryUsage = 0,
            TotalMemory = 0,
            AvailableMemory = 0,
            CurrentProcessMemoryUsage = 0,
            RxMbps = 0,
            TxMbps = 0,
            RxUsagePercent = 0,
            TxUsagePercent = 0,
            NetworkInterfaceName = null,
            LinkSpeedGbps = 0,
            MeasureTimestamp = default
        };
        
        // Konfigurer mock til at kaste en HubException med den specifikke besked
        mockHubClient
            .Setup(client => client.InvokeAsync("ReceiveMetric", metricMessage))
            .ThrowsAsync(new HubException("Method does not exist"));

        // Act
        await MessageRouter.RouteMessageToClientAsync(mockHubClient.Object, metricMessage);

        // Assert
        // Verificer, at HandleUnknownMessage bliver kaldt med beskeden "ReceiveDeadLetter"
        mockHubClient.Verify(client => client.InvokeAsync("ReceiveDeadLetter", 
            It.Is<string>(msg => msg.Contains("Unknown message type received"))), Times.Once);

        // Verificer at `ReceiveMetric` blev forsøgt kaldt
        mockHubClient.Verify(client => client.InvokeAsync("ReceiveMetric", metricMessage), Times.Once);
    }
}