using Engine.Utils;
using Moq;
using System.Net.NetworkInformation;
using Xunit;

namespace Engine.Tests.Utils
{
    public class NetworkUsageMonitorTest
    {
        [Fact]
        public void GetNetworkUsage_ReturnsCorrectUsage()
        {
            // Arrange
            var mockProvider = new Mock<INetworkInterfaceProvider>();
            var mockInterface = new Mock<NetworkInterface>();
            var mockStats = new Mock<IPv4InterfaceStatistics>();

            mockInterface.Setup(i => i.OperationalStatus).Returns(OperationalStatus.Up);
            mockInterface.Setup(i => i.NetworkInterfaceType).Returns(NetworkInterfaceType.Ethernet);
            mockInterface.Setup(i => i.Name).Returns("Ethernet0");
            mockInterface.Setup(i => i.Description).Returns("Test Ethernet Adapter");
            mockInterface.Setup(i => i.Id).Returns("test-id");
            mockInterface.Setup(i => i.Speed).Returns(1_000_000_000); // 1 Gbps

            // Første måling
            mockStats.SetupSequence(s => s.BytesReceived)
                .Returns(500_000)  // Første måling
                .Returns(1_000_000); // Anden måling med højere værdi for at simulere netværkstrafik
            mockStats.SetupSequence(s => s.BytesSent)
                .Returns(200_000)  // Første måling
                .Returns(500_000); // Anden måling med højere værdi

            mockInterface.Setup(i => i.GetIPv4Statistics()).Returns(mockStats.Object);
            mockProvider.Setup(p => p.GetAllNetworkInterfaces()).Returns(new[] { mockInterface.Object });

            var monitor = new NetworkUsageMonitor(mockProvider.Object);

            // Act
            // Første kald for at initialisere interne målinger
            monitor.GetNetworkUsage();

            // Simuler en tidsforskel på 2 sekunder
            System.Threading.Thread.Sleep(2000);

            // Andet kald for at beregne netværksbrug baseret på opdaterede målinger
            var usage = monitor.GetNetworkUsage();

            // Assert
            Assert.Single(usage);
            var interfaceUsage = usage[0];
            Assert.Equal("Ethernet0", interfaceUsage.InterfaceName);
            Assert.Equal(1.0, interfaceUsage.LinkSpeedGbps, 1); // Hastigheden i Gbps
            Assert.True(interfaceUsage.RxMbps > 0, "RxMbps should be greater than 0.");
            Assert.True(interfaceUsage.TxMbps > 0, "TxMbps should be greater than 0.");
        }
    }
}
