using System.Text;
using Shared;
using SocketApp.Server;
using SocketApp.Server.Events;

namespace Socket.Server.Test;

public class TestServer
{
    public async Task Start()
    {
        Console.WriteLine("Server Started");

        var server = new TcpServer();
        server.RegisterForClientConnected<SendClientDetails>();
        server.Register<ReceiveMessage>(HeaderConstants.ClientDetails);
        await server.Start();
        Console.ReadLine();

        server.Stop();

        Console.WriteLine("Server Closed");
    }
}

public class ReceiveMessage : BaseTcpServerRegister
{
    public override void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args)
    {
        var body = Encoding.UTF8.GetString(args.Body.Span);
        Console.WriteLine("Body: " + body);
    }
}

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
        // await sender.SendBytes(args.ClientSocket, HeaderConstants.ClientDetails, Encoding.UTF8.GetBytes(args.TotalConnections.ToString()));
    }
}