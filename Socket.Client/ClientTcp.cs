using System.Net;
using System.Net.Sockets;
using System.Text;
using Socket.Client.Events;

namespace Socket.Client;

public class ClientTcp
{
    private readonly IPAddress _ipAddress = IPAddress.Loopback;
    private const int Port = 2500;
    private const int BufferLength = 1024;
    private readonly byte[] _buffer = new byte[BufferLength];
    private bool _started;
    
    private readonly TcpClient _client = new();
    private NetworkStream _networkStream;

    public static OnServerConnected<ClientTcp>? OnServerConnected;
    public static OnMessageReceived<ClientTcp>? OnMessageReceived;
    public static OnExceptionRaised? OnExceptionRaised;

    public async Task Start()
    {
        if (!_started)
        {
            await _client.ConnectAsync(_ipAddress, Port);
            _networkStream = _client.GetStream();
        
            OnServerConnected?.Invoke(this, new ServerConnectedEventArgs
            {
                IpAddress = _ipAddress,
                Port = Port,
                ServerNetworkStream = _networkStream
            });
        
            _networkStream.BeginRead(_buffer, 0, BufferLength, BeginRead, null);
            _started = true;
        }
    }
    
    public void Stop()
    {
        _client.Client.Close();
        _client.Dispose();
    }
    
    void BeginRead(IAsyncResult result)
    {
        var bytesRead = 0;
        try
        {
            bytesRead = _networkStream.EndRead(result);
            if (bytesRead <= 0)
            {
                return;
            }

            ReadOnlyMemory<byte> memoryBuffer = _buffer;
            
            var headerLengthAsBytes = memoryBuffer.Slice(0, 10).Span;
            var hs = Encoding.UTF8.GetString(headerLengthAsBytes);
            int.TryParse(hs, out var headerLength);

            var headerData = memoryBuffer.Slice(20, headerLength);
            var bodyData = memoryBuffer.Slice(20 + headerLength);

            OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs
            {
                Bytes = _buffer,
                TotalBytesRead = bytesRead,
                NetworkStream = _networkStream,
                Header = headerData,
                Body = bodyData
            });
        }
        catch (Exception e)
        {
            OnExceptionRaised?.Invoke(e);
        }
        finally
        {
            if (bytesRead > 0)
            {
                _networkStream.BeginRead(_buffer, 0, BufferLength, BeginRead, null);
            }
        }
    }
    
    /// <summary>
    /// Send Data to Client Socket.<br/>
    /// first 10 bytes of data is Assigned to read headerLength, 
    /// next 10 bytes of data is Assigned to read BodyLength,
    /// after that starts the Header Data and ranges to headerLength
    /// same followed for Buffer
    /// </summary>
    /// <param name="socket">Extension for Socket Class</param>
    /// <param name="headerSize">Length of header</param>
    /// <param name="bodySize">Length of body</param>
    /// <param name="header">Header Data</param>
    /// <param name="body">Body Data.</param>
    public async Task SendBytes(
        byte[] header, byte[] body)
    {
        long headerSize = header.Length;
        long bodySize = BufferLength;

        ArraySegment<byte> bytes = new byte[headerSize + bodySize];

        // take 10 bytes
        var headerToArray = Encoding.UTF8.GetBytes(headerSize.ToString());
        Array.Copy(headerToArray,
            0, bytes.Array!, 0, headerToArray.Length);
        
        // take 10 bytes
        var bodyLengthToArray = Encoding.UTF8.GetBytes(body.Length.ToString());
        Array.Copy(bodyLengthToArray,
            0, bytes.Array!, 10, bodyLengthToArray.Length);
        
        // starts from 20 to headerLength
        Array.Copy(header, 0, bytes.Array!,
            20, header.Length);
        
        // starts from 20 + headerLength to bodyLength
        Array.Copy(body, 0, bytes.Array!,
            20 + headerSize, body.Length);
        
        await SendBytes(bytes.Array!);
    }
    
    private async Task SendBytes(byte[] bufferWithHeader) => 
        await _client.GetStream().WriteAsync(bufferWithHeader, 0, bufferWithHeader.Length);
}