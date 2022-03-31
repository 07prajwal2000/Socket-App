using System.Text;
using Socket.Client.Events;

namespace Socket.Client;

public class TestClient
{
    private int i = 0;
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
        Console.WriteLine("Received\n" + Encoding.UTF8.GetString(eventArgs.Bytes, 0, eventArgs.TotalBytesRead));
        if (i <= 5)
        {
            await sender.SendBytes(Encoding.UTF8.GetBytes("Received Id"), Encoding.UTF8.GetBytes("Received Id"));
            i++;
        }
    }
}