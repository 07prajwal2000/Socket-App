using System.Net;
using System.Net.Sockets;

namespace Socket.Client.Events;

public delegate void OnServerConnected<in TSender>(TSender sender, ServerConnectedEventArgs eventArgs);

public class ServerConnectedEventArgs
{
    public IPAddress IpAddress { get; set; }
    public int Port { get; set; }
    public NetworkStream ServerNetworkStream { get; set; }
}