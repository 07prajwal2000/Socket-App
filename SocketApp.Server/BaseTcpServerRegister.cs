using Socket.Server.Events;

namespace Socket.Server;

public abstract class BaseTcpServerRegister
{
    public abstract void RegisterEvents(TcpServer server);

    /// <summary>
    /// Called When a message comes to the server.
    /// </summary>
    /// <param name="sender">Tcp Server as a sender</param>
    /// <param name="args">Useful arguments that contains the data and some others...</param>
    public abstract void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args);

    /// <summary>
    /// Called When a client Connected to the Server
    /// </summary>
    /// <param name="sender">Tcp server as a sender</param>
    /// <param name="args">Useful arguments that contains some data.</param>
    public abstract void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args);
}