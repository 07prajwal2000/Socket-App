using System.Text;
using Shared.Models;
using Socket.Server.Events;

namespace Socket.Server.Demo;

public class TestServer
{
    public async Task Start()
    {
        Console.WriteLine("Server Started");

        var server = new TcpServer();
        server.Register<SendClientDetails>(0);
        server.Register<Test>(HeaderConstants.ClientDetails);
        
        await server.Start();
        Console.ReadLine();

        server.Stop();

        Console.WriteLine("Server Closed");
    }
}

public class Test : BaseTcpServerRegister
{
    public override void RegisterEvents(TcpServer server)
    {
        
    }

    public override void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args)
    {
        var body = Encoding.UTF8.GetString(args.Body.Span);
        Console.WriteLine(body);
    }

    public override void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args)
    {
        
    }
}