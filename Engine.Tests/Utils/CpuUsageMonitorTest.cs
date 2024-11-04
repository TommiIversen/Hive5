using System.Threading;
using System.Threading.Tasks;
using Engine.Utils;
using Xunit;

namespace Engine.Tests.Utils;

public class CpuUsageMonitorTests
{
    [Fact]
    public async Task GetTotalCpuUsageAsync_ShouldReturnAValue()
    {
        // Arrange
        var cpuUsageMonitor = new CpuUsageMonitor();

        // Act
        var totalCpuUsage = await cpuUsageMonitor.GetTotalCpuUsageAsync(CancellationToken.None);

        // Assert
        Assert.True(totalCpuUsage >= 0, "Total CPU usage should be a non-negative value.");
    }

    [Fact]
    public async Task GetPerCoreCpuUsageAsync_ShouldReturnArrayWithValues()
    {
        // Arrange
        var cpuUsageMonitor = new CpuUsageMonitor();

        // Act
        var perCoreCpuUsage = await cpuUsageMonitor.GetPerCoreCpuUsageAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(perCoreCpuUsage);
        Assert.True(perCoreCpuUsage.Length > 0, "Per-core CPU usage should return an array with at least one value.");
        Assert.All(perCoreCpuUsage,
            usage => Assert.True(usage >= 0, "Each core's CPU usage should be a non-negative value."));
    }

    [Fact]
    public void GetCurrentProcessCpuUsage_ShouldReturnAValue()
    {
        // Arrange
        var cpuUsageMonitor = new CpuUsageMonitor();

        // Act
        var currentProcessCpuUsage = cpuUsageMonitor.GetCurrentProcessCpuUsage();

        // Assert
        Assert.True(currentProcessCpuUsage >= 0, "Current process CPU usage should be a non-negative value.");
    }
}