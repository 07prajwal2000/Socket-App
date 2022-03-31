using System.Text;
using Shared.Models;
using Socket.Server.Events;

namespace Socket.Server.Demo;

public class SendClientDetails : BaseTcpServerRegister
{

    public override void RegisterEvents(TcpServer server)
    {
        
    }

    public override void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args)
    {
        Console.WriteLine("Message Received \n" + Encoding.UTF8.GetString(args.Bytes, 0, args.TotalBytesRead));
    }

    public override async void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args)
    {
        Console.WriteLine("Client Connected. Total Connections: " + args.TotalConnections);
        await sender.SendBytes(args.ClientSocket, HeaderConstants.ClientDetails, Encoding.UTF8.GetBytes(args.TotalConnections.ToString()));
    }

}