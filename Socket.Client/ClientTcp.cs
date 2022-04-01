using System.Net;
using System.Net.Sockets;
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
    private readonly byte[] _buffer;
    
    public readonly int PacketSize;
    public readonly int HeaderSize;
    /// <summary>
    /// The range starts from HeaderSize to this Size contains the Size of the Body which is sent from the Server.
    /// </summary>
    public readonly int BodyLengthSizeInBuffer;

    public static OnServerConnected<ClientTcp>? OnServerConnected;
    public static OnMessageReceived<ClientTcp>? OnMessageReceived;
    public static OnExceptionRaised? OnExceptionRaised;

    public ClientTcp(int packetSize = 1024, int port = 2500, int headerSize = 10, int bodyLengthSizeInBuffer = 10)
    {
        HeaderSize = headerSize;
        BodyLengthSizeInBuffer = bodyLengthSizeInBuffer;
        
        PacketSize = packetSize;
        _buffer = new byte[packetSize];
        _port = port;
        _ipAddress = IPAddress.Loopback;
    }

    public ClientTcp(IPAddress ipAddress, int port = 2500, int packetSize = 1024, int headerSize = 10, int bodyLengthSizeInBuffer = 10)
    {
        BodyLengthSizeInBuffer = bodyLengthSizeInBuffer;
        HeaderSize = headerSize;
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


            var headerData = ReadHeader(_buffer);
            var bodyLength = ReadBodyLength(_buffer);
            
            ReadOnlyMemory<byte> memoryBuffer = _buffer;
            var bodyData = memoryBuffer.Slice(HeaderSize + BodyLengthSizeInBuffer, bodyLength);
            
            var receivedEventArgs = new MessageReceivedEventArgs
            {
                TotalBytesRead = bytesRead,
                NetworkStream = _networkStream,
                Header = headerData,
                TotalNumberOfBytesContainInBuffer = bodyLength,
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
        byte[] headerBytes = BitConverter.GetBytes(header);
        
        byte[] bodyLengthBytes = BitConverter.GetBytes(body.Length);
        
        ArraySegment<byte> bytes = new byte[HeaderSize + BodyLengthSizeInBuffer + body.Length];

        Array.Copy(headerBytes,
            0, bytes.Array!, 0, headerBytes.Length);
        
        Array.Copy(bodyLengthBytes, 0, bytes.Array!, HeaderSize, bodyLengthBytes.Length);
        
        Array.Copy(body, 0, bytes.Array!,
            BodyLengthSizeInBuffer + HeaderSize, body.Length);
        
        await SendBytes(bytes.Array!);
    }
    
    public uint ReadHeader(ReadOnlySpan<byte> buffer)
    {
        var headerSpan = buffer.Slice(0, 10);
        var header = BitConverter.ToUInt32(headerSpan);
        return header;
    }

    public int ReadBodyLength(ReadOnlySpan<byte> buffer)
    {
        var bodyLengthSpan = buffer.Slice(10, 20);
        var bodyLength = BitConverter.ToInt32(bodyLengthSpan);
        return bodyLength;
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