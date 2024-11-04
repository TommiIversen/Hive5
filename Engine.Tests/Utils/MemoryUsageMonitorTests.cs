using System.Threading;
using System.Threading.Tasks;
using Engine.Utils;
using Xunit;

public class MemoryUsageMonitorTests
{
    [Fact]
    public async Task GetTotalMemoryAsync_ShouldReturnAValue()
    {
        // Arrange
        var memoryUsageMonitor = new MemoryUsageMonitor();

        // Act
        var totalMemory = await memoryUsageMonitor.GetTotalMemoryAsync(CancellationToken.None);

        // Assert
        Assert.True(totalMemory >= 0, "Total memory should be a non-negative value.");
    }

    [Fact]
    public async Task GetAvailableMemoryAsync_ShouldReturnAValue()
    {
        // Arrange
        var memoryUsageMonitor = new MemoryUsageMonitor();

        // Act
        var availableMemory = await memoryUsageMonitor.GetAvailableMemoryAsync(CancellationToken.None);

        // Assert
        Assert.True(availableMemory >= 0, "Available memory should be a non-negative value.");
    }

    [Fact]
    public void GetCurrentProcessMemoryUsage_ShouldReturnAValue()
    {
        // Arrange
        var memoryUsageMonitor = new MemoryUsageMonitor();

        // Act
        var processMemoryUsage = memoryUsageMonitor.GetCurrentProcessMemoryUsage();

        // Assert
        Assert.True(processMemoryUsage >= 0, "Current process memory usage should be a non-negative value.");
    }
}