using Shared;
using SocketApp.Server.Events;

namespace SocketApp.Server;

public abstract class BaseTcpServerRegister
{
    /// <summary>
    /// Called When a message comes to the server.
    /// </summary>
    /// <param name="sender">Tcp Server as a sender</param>
    /// <param name="args">Useful arguments that contains the data and some others...</param>
    /// <param name="responsePacket">Use this packet class to write data and send it to client.</param>
    public abstract void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args, NetworkPacket responsePacket);
}