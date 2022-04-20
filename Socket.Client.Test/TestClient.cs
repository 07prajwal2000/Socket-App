using Shared;

namespace Socket.Client.Test;

public class TestClient
{
    public async Task Start()
    {
        var client = new ClientTcp();
        client.RegisterOnServerConnected<OnServerConnected>();
        client.Register<GetIdFromServer>(HeaderConstants.TestMessage);
        
        await client.Start();
        Console.WriteLine("Press any key to close.");
        Console.ReadKey();
        client.Stop();
    }
}