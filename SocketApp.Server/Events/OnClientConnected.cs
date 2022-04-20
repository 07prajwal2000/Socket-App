using Shared;

namespace SocketApp.Server.Events;

public delegate void OnClientConnected(TcpServer sender, ClientConnectedEventArgs eventArgs);

public class ClientConnectedEventArgs
{
    public int TotalConnections { get; set; }
    public System.Net.Sockets.Socket ClientSocket { get; set; }
    /// <summary>
    /// Class is used to write the data and send it to the client, not meant for reading
    /// </summary>
    public NetworkPacket NetworkPacket { get; set; }
}