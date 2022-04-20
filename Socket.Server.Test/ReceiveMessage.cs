using System.Text;
using Shared;
using SocketApp.Server;
using SocketApp.Server.Events;

namespace Socket.Server.Test;

public class ReceiveMessage : BaseTcpServerRegister
{
    public override async void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args, NetworkPacket responsePacket)
    {
        
    }
}