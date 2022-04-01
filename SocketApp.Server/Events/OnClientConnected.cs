namespace SocketApp.Server.Events;

public delegate void OnClientConnected(TcpServer sender, ClientConnectedEventArgs eventArgs);

public class ClientConnectedEventArgs
{
    public int TotalConnections { get; set; }
    public System.Net.Sockets.Socket ClientSocket { get; set; }
    public List<System.Net.Sockets.Socket> ConnectedClients { get; set; }
}