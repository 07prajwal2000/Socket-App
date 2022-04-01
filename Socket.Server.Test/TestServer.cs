using System.Text;
using Shared;
using Socket.Server.Events;
using SocketApp.Server;

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
        await sender.SendBytes(args.ClientSocket, HeaderConstants.ClientDetails, Encoding.UTF8.GetBytes(args.TotalConnections.ToString()));
    }
}