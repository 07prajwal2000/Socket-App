using Shared;
using Socket.Client.Events;

namespace Socket.Client.Test;

public class GetIdFromServer : BaseTcpClientRegister
{
    public override async void OnServerRespond(ClientTcp sender, MessageReceivedEventArgs args,
        NetworkPacket responsePacket)
    {
        var netPacket = args.NetworkPacket;
        Console.WriteLine(netPacket.ReadInt());
        Console.WriteLine(netPacket.ReadBool());
        Console.WriteLine(netPacket.ReadString());
    }
}