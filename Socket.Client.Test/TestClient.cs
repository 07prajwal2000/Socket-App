using System.Text;
using Shared;
using Socket.Client.Events;

namespace Socket.Client.Test;

public class TestClient
{
    public async Task Start()
    {
        var client = new ClientTcp();
        client.Register<GetIdFromServer>(HeaderConstants.ClientDetails);
        
        await client.Start();

        Console.WriteLine("Press any key to stop.");
        Console.ReadLine();

        client.Stop();
    }
}

public class GetIdFromServer : BaseTcpClientRegister
{
    public override async void OnServerRespond(ClientTcp sender, MessageReceivedEventArgs args)
    {
        var netPacket = args.NetworkPacket;
        Console.WriteLine(netPacket.ReadInt());
        Console.WriteLine(netPacket.ReadBool());
        Console.WriteLine(netPacket.ReadString());
        await sender.SendBytes(HeaderConstants.ClientDetails, Encoding.UTF8.GetBytes("Received Id"));
    }
}