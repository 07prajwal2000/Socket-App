using System.Net;
using System.Net.Sockets;
using System.Text;
// ReSharper disable All

namespace Socket.Server;

public class Server
{
    readonly List<System.Net.Sockets.Socket> _clients = new();
    int _totalConnected;
    static readonly IPAddress IpAddress = IPAddress.Loopback;
    const int Port = 2500;
    
    const int PacketSize = 1024;
    private readonly byte[] _buffer = new byte[PacketSize];

    readonly TcpListener _tcpListener = new(IpAddress, Port);

    public async Task Start()
    {
        _tcpListener.Start();
        await Task.Run(StartServer);
    }

    private async Task StartServer()
    {
        do
        {
            try
            {
                var clientSocket = await Task.Factory.FromAsync(
                    _tcpListener.Server.BeginAccept, _tcpListener.Server.EndAccept, null).ConfigureAwait(false);
            
                _totalConnected++;
                _clients.Add(clientSocket);
            
                ArraySegment<byte> bytes = Encoding.UTF8.GetBytes("Your ID: " + _totalConnected); //Sending Data
                await clientSocket.SendAsync(bytes, SocketFlags.None);

                clientSocket.BeginReceive(_buffer, 0, PacketSize, SocketFlags.None, 
                    ar =>  ReceiveCallback(ar, clientSocket), clientSocket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
        } while (true);
    }

    private void ReceiveCallback(IAsyncResult ar, System.Net.Sockets.Socket client)
    {
        var bytesRead = 0;
        try
        {
            bytesRead = client.EndReceive(ar);
            if (bytesRead is 0) return;

            var data = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
            data = data.Trim();
            Console.WriteLine("Received: " + data);

            #region Testing

            var clientId = int.Parse(data[0].ToString());
            Console.WriteLine(clientId);
            var sendData = Encoding.UTF8.GetBytes(data);
            var c = _clients[clientId];
            
            c.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, null, null);

            #endregion
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            if (bytesRead > 0)
            {
                client.BeginReceive(_buffer, 0, PacketSize, SocketFlags.None, result => ReceiveCallback(result, client), null);
            }
            else
            {
                _clients.Remove(client);
            }
        }
    }
    
    public void Stop() => _tcpListener.Stop();
}