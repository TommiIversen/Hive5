namespace Engine.Utils;

using System.Net.NetworkInformation;

public interface INetworkInterfaceProvider
{
    NetworkInterface[] GetAllNetworkInterfaces();
}


public class NetworkInterfaceProvider : INetworkInterfaceProvider
{
    public NetworkInterface[] GetAllNetworkInterfaces()
    {
        return NetworkInterface.GetAllNetworkInterfaces();
    }
}

public class NetworkUsageMonitor(INetworkInterfaceProvider networkInterfaceProvider)
{
    private readonly Dictionary<string, long> _lastBytesReceived = new();
    private readonly Dictionary<string, long> _lastBytesSent = new();
    private DateTime _lastChecked = DateTime.UtcNow;

    public class NetworkInterfaceUsage
    {
        public string InterfaceName { get; set; }
        public double LinkSpeedGbps { get; set; }
        public double RxMbps { get; set; }
        public double TxMbps { get; set; }
        public double RxUsagePercent { get; set; }
        public double TxUsagePercent { get; set; }
    }

    public List<NetworkInterfaceUsage> GetNetworkUsage()
    {
        var interfaces = networkInterfaceProvider.GetAllNetworkInterfaces();
        var currentTime = DateTime.UtcNow;
        var elapsedSeconds = (currentTime - _lastChecked).TotalSeconds;

        var networkUsageList = new List<NetworkInterfaceUsage>();

        foreach (var netInterface in interfaces)
        {
            if (netInterface.OperationalStatus != OperationalStatus.Up) continue;
            if (netInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;
            if (netInterface.Name.Contains("WSL") || netInterface.Description.Contains("WSL") || netInterface.Name.Contains("vEthernet")) continue;

            var statistics = netInterface.GetIPv4Statistics();
            var bytesReceived = statistics.BytesReceived;
            var bytesSent = statistics.BytesSent;
            var interfaceSpeed = netInterface.Speed;

            double rxMbps = 0;
            double txMbps = 0;
            double rxUsagePercent = 0;
            double txUsagePercent = 0;

            if (_lastBytesReceived.ContainsKey(netInterface.Id) && _lastBytesSent.ContainsKey(netInterface.Id))
            {
                var deltaBytesReceived = bytesReceived - _lastBytesReceived[netInterface.Id];
                var deltaBytesSent = bytesSent - _lastBytesSent[netInterface.Id];

                rxMbps = (deltaBytesReceived * 8) / (elapsedSeconds * 1_000_000);
                txMbps = (deltaBytesSent * 8) / (elapsedSeconds * 1_000_000);

                if (interfaceSpeed > 0)
                {
                    rxUsagePercent = (rxMbps * 1_000_000) / interfaceSpeed * 100;
                    txUsagePercent = (txMbps * 1_000_000) / interfaceSpeed * 100;
                }
            }

            networkUsageList.Add(new NetworkInterfaceUsage
            {
                InterfaceName = netInterface.Name,
                LinkSpeedGbps = interfaceSpeed / 1_000_000_000.0,
                RxMbps = rxMbps,
                TxMbps = txMbps,
                RxUsagePercent = rxUsagePercent,
                TxUsagePercent = txUsagePercent
            });

            _lastBytesReceived[netInterface.Id] = bytesReceived;
            _lastBytesSent[netInterface.Id] = bytesSent;
        }

        _lastChecked = currentTime;
        return networkUsageList;
    }
}
