using Engine.Utils;
using Xunit;

public class SystemInfoCollectorTests
{
    [Fact]
    public void GetSystemInfo_ShouldReturnValidSystemInfoModel()
    {
        // Arrange
        var systemInfoCollector = new SystemInfoCollector();

        // Act
        var systemInfo = systemInfoCollector.GetSystemInfo();

        // Assert
        Assert.NotNull(systemInfo);
        Assert.False(string.IsNullOrWhiteSpace(systemInfo.OsName), "OS name should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(systemInfo.OsVersion), "OS version should not be empty.");
        Assert.False(string.IsNullOrWhiteSpace(systemInfo.Architecture), "Architecture should not be empty.");
        Assert.True(systemInfo.Uptime >= 0, "Uptime should be a non-negative value.");
        Assert.True(systemInfo.ProcessCount > 0, "Process count should be greater than zero.");
        Assert.False(string.IsNullOrWhiteSpace(systemInfo.Platform), "Platform should not be empty.");
    }

    [Fact]
    public void GetPlatform_ShouldReturnKnownPlatform()
    {
        // Arrange
        var systemInfoCollector = new SystemInfoCollector();

        // Act
        var platform = systemInfoCollector.GetSystemInfo().Platform;

        // Assert
        Assert.Contains(platform, new[] { "Windows", "Linux", "macOS", "Unknown" });
    }

    [Fact]
    public void GetSystemUptime_ShouldReturnNonNegativeValue()
    {
        // Arrange
        var systemInfoCollector = new SystemInfoCollector();

        // Act
        var uptime = systemInfoCollector.GetSystemInfo().Uptime;

        // Assert
        Assert.True(uptime >= 0, "System uptime should be a non-negative value.");
    }
}