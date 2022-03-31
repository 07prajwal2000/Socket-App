using System.Text;
using Shared.Models;
using Socket.Client.Events;

namespace Socket.Client;

public class TestClient
{
    public async Task Start()
    {
        var client = new ClientTcp();
        ClientTcp.OnMessageReceived += OnMessageReceived;

        await client.Start();

        Console.WriteLine("Press any key to stop.");
        Console.ReadLine();

        client.Stop();
    }

    private async void OnMessageReceived(ClientTcp sender, MessageReceivedEventArgs eventArgs)
    {
        Console.WriteLine("Header: " + eventArgs.Header + "\nReceived: " + Encoding.UTF8.GetString(eventArgs.Body.Span));
        await sender.SendBytes(HeaderConstants.ClientDetails, Encoding.UTF8.GetBytes("Received Id"));
    }
}