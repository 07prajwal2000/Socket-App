using Shared;

namespace Socket.Client.Test;

public class OnServerConnected : BaseTcpClientConnected
{
    public override async void OnServerRespond(ClientTcp sender, NetworkPacket sendPacket,
        uint header, int bodyLength, byte[] buffer)
    {
        var id = sendPacket.ReadInt();
        var testBool = sendPacket.ReadBool();
        var name = sendPacket.ReadString();

        Console.WriteLine($"ID {id}");
        Console.WriteLine($"TestBool {testBool}");
        Console.WriteLine($"NAME {name}");

        sender.SenderPacket.WriteString("Prajwal Aradhya");
        sender.SenderPacket.WriteInt(21);
        sender.SenderPacket.WriteDouble(3.1413);

        await sender.SendBytes(HeaderConstants.TestMessage, sender.SenderPacket);
    }
}