using System.Net.NetworkInformation;

namespace Engine.Utils;

public class NetworkInterfaceProvider : INetworkInterfaceProvider
{
    public NetworkInterface[] GetAllNetworkInterfaces()
    {
        return NetworkInterface.GetAllNetworkInterfaces();
    }
}