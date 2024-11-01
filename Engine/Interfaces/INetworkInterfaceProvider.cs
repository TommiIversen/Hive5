using System.Net.NetworkInformation;

namespace Engine.Utils;

public interface INetworkInterfaceProvider
{
    NetworkInterface[] GetAllNetworkInterfaces();
}