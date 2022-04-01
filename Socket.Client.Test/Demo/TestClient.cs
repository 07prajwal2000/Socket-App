using System.Text;
using Shared;
using Socket.Client.Events;

namespace Socket.Client.Test.Demo;

public class TestClient
{
    public async Task Start()
    {
        var client = new ClientTcp();
        ClientTcp.OnMessageReceived += OnMessageReceived;
        client.Register<GetIdFromServer>(HeaderConstants.ClientDetails);
        
        await client.Start();

        Console.WriteLine("Press any key to stop.");
        Console.ReadLine();

        client.Stop();
    }

    private async void OnMessageReceived(ClientTcp sender, MessageReceivedEventArgs eventArgs)
    {
        Console.WriteLine("Header: " + eventArgs.Header + "\nReceived Data: " + Encoding.UTF8.GetString(eventArgs.Body.Span));
        await sender.SendBytes(HeaderConstants.ClientDetails, Encoding.UTF8.GetBytes("Received Id"));
    }
}

public class GetIdFromServer : BaseTcpClientRegister
{
    public override void OnServerRespond(ClientTcp sender, MessageReceivedEventArgs eventArgs)
    {
        Console.WriteLine(eventArgs.TotalBytesRead);
    }
}