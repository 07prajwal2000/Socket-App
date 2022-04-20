using Shared;
using SocketApp.Server;

namespace Socket.Server.Test;

public class TestServer
{
    public async Task Start()
    {
        Console.WriteLine("Server Started");

        var server = new TcpServer();
        server.RegisterForClientConnected<SendClientDetails>();
        server.Register<ReceiveMessage>(HeaderConstants.TestMessage);
        await server.Start();
        Console.ReadLine();

        server.Stop();
        Console.WriteLine("Server Closed");
    }
}