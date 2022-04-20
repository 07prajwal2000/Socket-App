using Shared;
using Socket.Client.Events;

namespace Socket.Client;

public abstract class BaseTcpClientConnected
{
    /// <summary>
    /// Called On Server Responded
    /// </summary>
    /// <param name="sender">Client Socket</param>
    /// <param name="sendPacket">used for Readonly <exception cref="InvalidOperationException">throws when write.</exception></param>
    /// <param name="header">Header from server.</param>
    /// <param name="bodyLength">Total amount of data contains in body.</param>
    /// <param name="buffer">Actual Data.</param>
    public abstract void OnServerRespond(ClientTcp sender, 
        NetworkPacket sendPacket, uint header, int bodyLength, byte[] buffer);
}