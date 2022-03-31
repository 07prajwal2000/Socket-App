using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Shared;
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
        var header = Encoding.UTF8.GetBytes("Your Id");
        
        await sender.SendBytes(args.ClientSocket, header, Encoding.UTF8.GetBytes(args.TotalConnections.ToString()));
    }

}