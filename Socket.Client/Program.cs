using System.Net;
using System.Net.Sockets;
using System.Text;

IPAddress ipAddress = IPAddress.Loopback;
const int PORT = 2500;
const int BufferLength = 1024;
byte[] buffer = new byte[BufferLength];

TcpClient client = new TcpClient();

await client.ConnectAsync(ipAddress, PORT);

Console.WriteLine("Server Connected");

var stream = client.GetStream();
// var bytesRead = await stream.ReadAsync(buffer, 0, BufferLength);
stream.BeginRead(buffer, 0, BufferLength, BeginRead, null);

string message = "Hello from Client";

Reset:

ArraySegment<byte> sendBytes = Encoding.UTF8.GetBytes(message);

await client.Client.SendAsync(sendBytes, SocketFlags.None);
Console.WriteLine("Type '-r' to Resend and _ ClientID _ Message");
var reset = Console.ReadLine();

if (reset.Contains("-r"))
{
    message = reset.Remove(0, 2);
    goto Reset;
}
client.Client.Close();
client.Dispose();

void BeginRead(IAsyncResult result)
{
    var bytesRead = 0;
    try
    {
        bytesRead = stream.EndRead(result);
        if (bytesRead <= 0)
        {
            return;
        }

        Console.WriteLine($"Bytes Read: {bytesRead} \nData: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");

    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
    }
    finally
    {
        if (bytesRead > 0)
        {
            stream.BeginRead(buffer, 0, BufferLength, BeginRead, null);
        }
    }
}