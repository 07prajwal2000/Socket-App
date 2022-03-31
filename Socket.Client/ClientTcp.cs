using System.Net;
using System.Net.Sockets;
using System.Text;
using Socket.Client.Events;

namespace Socket.Client;

public class ClientTcp
{
    private bool _started;
    private NetworkStream _networkStream;
    private Dictionary<uint, BaseTcpClientRegister> _registers = new();

    private readonly TcpClient _client = new();
    private readonly int _port;
    private readonly IPAddress _ipAddress;
    public readonly int PacketSize;
    private readonly byte[] _buffer;
    
    public static OnServerConnected<ClientTcp>? OnServerConnected;
    public static OnMessageReceived<ClientTcp>? OnMessageReceived;
    public static OnExceptionRaised? OnExceptionRaised;

    public ClientTcp(int packetSize = 1024, int port = 2500)
    {
        PacketSize = packetSize;
        _buffer = new byte[packetSize];
        _port = port;
        _ipAddress = IPAddress.Loopback;
    }

    public ClientTcp(IPAddress ipAddress, int port = 2500, int packetSize = 1024)
    {
        _ipAddress = ipAddress;
        _port = port;
        PacketSize = packetSize;
        _buffer = new byte[packetSize];
    }
    
    public async Task Start()
    {
        if (!_started)
        {
            await _client.ConnectAsync(_ipAddress, _port);
            _networkStream = _client.GetStream();
        
            OnServerConnected?.Invoke(this, new ServerConnectedEventArgs
            {
                IpAddress = _ipAddress,
                Port = _port,
                ServerNetworkStream = _networkStream
            });
        
            _networkStream.BeginRead(_buffer, 0, PacketSize, BeginRead, null);
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
            if (bytesRead <= 0) return;

            ReadOnlyMemory<byte> memoryBuffer = _buffer;
            var headerLengthAsBytes = memoryBuffer.Slice(0, 10).Span;
            var hs = Encoding.UTF8.GetString(headerLengthAsBytes);
            int.TryParse(hs, out var headerLength);

            var bodyData = memoryBuffer.Slice(20 + headerLength);
            var headerData = ReadHeader(_buffer);
            
            var receivedEventArgs = new MessageReceivedEventArgs
            {
                Bytes = _buffer,
                TotalBytesRead = bytesRead,
                NetworkStream = _networkStream,
                Header = headerData,
                Body = bodyData
            };

            _registers.TryGetValue(headerData, out var register);
            register?.OnServerRespond(this, receivedEventArgs);
            
            OnMessageReceived?.Invoke(this, receivedEventArgs);
        }
        catch (Exception e)
        {
            OnExceptionRaised?.Invoke(e);
        }
        finally
        {
            if (bytesRead > 0)
            {
                _networkStream.BeginRead(_buffer, 0, PacketSize, BeginRead, null);
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
    /// <param name="header">Header Data should be uint. This is later used for Calling specific Method When Registered on Start of the server.<see cref="uint"/></param>
    /// <param name="body">Body Data.</param>
    public async Task SendBytes(uint header, byte[] body)
    {
        byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
        
        long headerSize = headerBytes.Length;
        
        long hLengthForString = headerBytes.Length;
        long bLengthForString = body.Length;
        
        ArraySegment<byte> bytes = new byte[PacketSize];

        // take 10 bytes
        var headerToArray = Encoding.UTF8.GetBytes(hLengthForString.ToString());
        Array.Copy(headerToArray,
            0, bytes.Array!, 0, headerToArray.Length);
        
        // take 10 bytes
        var bodyLengthToArray = Encoding.UTF8.GetBytes(bLengthForString.ToString());
        Array.Copy(bodyLengthToArray,
            0, bytes.Array!, 10, bodyLengthToArray.Length);
        
        // starts from 20 to headerLength
        Array.Copy(headerBytes, 0, bytes.Array!,
            20, headerBytes.Length);
        
        // starts from 20 + headerLength to bodyLength
        Array.Copy(body, 0, bytes.Array!,
            20 + headerSize, body.Length);
        
        await SendBytes(bytes.Array!);
    }
    
    public uint ReadHeader(byte[] buf)
    {
        ReadOnlySpan<byte> buffer = buf;

        var headerLength = buffer.Slice(0, 10);
        int.TryParse(Encoding.UTF8.GetString( headerLength ), out var lengthAsNum);

        var headerAsMemory = buffer.Slice(20, lengthAsNum);

        uint.TryParse(Encoding.UTF8.GetString(headerAsMemory), out uint header);

        return header;
    }

    /// <summary>
    /// Use This Method to Register your Events and Get Access to Properties
    /// </summary>
    /// <typeparam name="TClass"> TClass should Implement <see cref="BaseTcpClientRegister"/></typeparam>
    /// <exception cref="ArgumentException">If the Datatype Already Exists.</exception>
    public bool Register<TClass>(uint header) where TClass : BaseTcpClientRegister, new()
    {
        if (_registers.ContainsKey(header))
        {
            throw new ArgumentException($"Already Registered with the type of {header} in Register() method");
        }

        BaseTcpClientRegister register = new TClass();
        return _registers.TryAdd(header, register);
    }
    
    private async Task SendBytes(byte[] bufferWithHeader) => 
        await _client.GetStream().WriteAsync(bufferWithHeader, 0, bufferWithHeader.Length);
}