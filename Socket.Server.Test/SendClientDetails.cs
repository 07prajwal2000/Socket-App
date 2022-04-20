using Shared;
using SocketApp.Server;
using SocketApp.Server.Events;

namespace Socket.Server.Test;

public class SendClientDetails : BaseTcpRegisterOnClientConnection
{
    public override async void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args)
    {
        Console.WriteLine("Client Connected. Total Connections: " + args.TotalConnections);

        var netPacket = args.NetworkPacket;
        netPacket.WriteInt(args.TotalConnections);
        netPacket.WriteBool(true);
        netPacket.WriteString("Prajwal Aradhya");
        
        await sender.SendBytes(args.ClientSocket, HeaderConstants.ClientDetails, netPacket.ToArray(out int totalBytes), totalBytes);
    }
}