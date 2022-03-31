using Socket.Client.Events;

namespace Socket.Client;

public abstract class BaseTcpClientRegister
{
    public abstract void OnServerRespond(ClientTcp sender, MessageReceivedEventArgs eventArgs);
}