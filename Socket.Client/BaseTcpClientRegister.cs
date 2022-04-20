using Shared;
using Socket.Client.Events;

namespace Socket.Client;

public abstract class BaseTcpClientRegister
{
    /// <summary>
    /// Called When a message comes to the client.
    /// </summary>
    /// <param name="sender">Tcp Client as a sender</param>
    /// <param name="args">Useful arguments that contains the data and some others...</param>
    /// <param name="responsePacket">Use this packet class to write data and send it to server.</param>
    public abstract void OnServerRespond(ClientTcp sender, MessageReceivedEventArgs args,
        NetworkPacket responsePacket);
}