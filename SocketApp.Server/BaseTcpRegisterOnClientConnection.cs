using Socket.Server;
using Socket.Server.Events;

namespace SocketApp.Server;

public abstract class BaseTcpRegisterOnClientConnection
{
    /// <summary>
    /// Called When a client Connected to the Server
    /// </summary>
    /// <param name="sender">Tcp server as a sender</param>
    /// <param name="args">Useful arguments that contains some data.</param>
    public abstract void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args);
}