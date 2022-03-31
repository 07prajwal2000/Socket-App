using Socket.Server.Events;

namespace Socket.Server;

public abstract class BaseTcpServerRegister
{
    public abstract void RegisterEvents(TcpServer server);

    public abstract void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args);

    public abstract void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args);
}