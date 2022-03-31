using Shared;
using Shared.Models;

namespace Socket.Server.Demo;

public class TestServer
{
    public async Task Start()
    {
        Console.WriteLine("Server Started");

        var server = new TcpServer();
        server.Register<SendClientDetails>(Constants.ClientDetails);
        
        await server.Start();
        Console.ReadLine();

        server.Stop();

        Console.WriteLine("Server Closed");
    }
}