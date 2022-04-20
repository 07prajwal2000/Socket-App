using System.Net;
using System.Net.Sockets;
using Shared;
using Socket.Client.Events;

namespace Socket.Client;

public class ClientTcp
{
    private bool _started;
    private bool _stoped;
    private NetworkStream _networkStream;
    public readonly NetworkPacket SenderPacket;

    private readonly TcpClient _client = new();
    private readonly int _port;
    private readonly IPAddress _ipAddress;
    private readonly byte[] _buffer;
    private Dictionary<uint, BaseTcpClientRegister> _registers = new();
    private BaseTcpClientConnected clientConnected;
    private bool alreadyRegistered = false;
    
    public readonly int PacketSize;
    public readonly int HeaderSize;
    /// <summary>
    /// The range starts from HeaderSize to this Size contains the Size of the Body which is sent from the Server.
    /// </summary>
    public readonly int BodyLengthSizeInBuffer;
    public int TotalAvailableBodyLength => PacketSize - (BodyLengthSizeInBuffer + HeaderSize);

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
        SenderPacket = new NetworkPacket(TotalAvailableBodyLength);
    }

    public ClientTcp(IPAddress ipAddress, int port = 2500, int packetSize = 1024, int headerSize = 10, int bodyLengthSizeInBuffer = 10)
    {
        BodyLengthSizeInBuffer = bodyLengthSizeInBuffer;
        HeaderSize = headerSize;
        _ipAddress = ipAddress;
        _port = port;
        PacketSize = packetSize;
        _buffer = new byte[packetSize];
        SenderPacket = new NetworkPacket(TotalAvailableBodyLength);
    }
    
    public async Task Start()
    {
        if (!_started)
        {
            await _client.ConnectAsync(_ipAddress, _port);
            _networkStream = _client.GetStream();
            
            _networkStream.BeginRead(_buffer, 0, PacketSize, BeginRead, null);

            ReadOnlyMemory<byte> memoryBuffer = _buffer;
            var body = memoryBuffer.Slice(HeaderSize + BodyLengthSizeInBuffer);
            
            clientConnected?.OnServerRespond(this, 
                new NetworkPacket(body),
                ReadHeader(_buffer), 
                ReadBodyLength(_buffer), 
                _buffer);
            
            OnServerConnected?.Invoke(this, new ServerConnectedEventArgs
            {
                IpAddress = _ipAddress,
                Port = _port,
                ServerNetworkStream = _networkStream
            });
            _started = true;
            _stoped = false;
        }
    }
    
    public void Stop()
    {
        if (_stoped)
        {
            return;
        }
        _client.Client.Close();
        _client.Dispose();
        _stoped = true;
        _started = false;
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
                TotalNumberOfBytesContainsInBody = bodyLength,
                Body = bodyData,
                NetworkPacket = new NetworkPacket(bodyData)
            };

            var responsePacket = new NetworkPacket(TotalAvailableBodyLength);
            _registers.TryGetValue(headerData, out var register);
            register?.OnServerRespond(this, receivedEventArgs, responsePacket);
            
            OnMessageReceived?.Invoke(this, receivedEventArgs, responsePacket);
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
        
        var bytes = CopyArray(body, headerBytes, bodyLengthBytes);

        await SendBytes(bytes.Array!);
        await SenderPacket.ResetAsync();
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
    /// <param name="totalBytesInBody">Total no of bytes written into buffer. or Total bytes containing in body</param>
    public async Task SendBytes(uint header, byte[] body, int totalBytesInBody)
    {
        byte[] headerBytes = BitConverter.GetBytes(header);
        
        byte[] bodyLengthBytes = BitConverter.GetBytes(totalBytesInBody);
        
        var bytes = CopyArray(body, headerBytes, bodyLengthBytes);

        await SendBytes(bytes.Array!);
        await SenderPacket.ResetAsync();
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
    /// <param name="totalBytesInBody">Total no of bytes written into buffer. or Total bytes containing in body</param>
    public async Task SendBytes(uint header, NetworkPacket packet)
    {
        var bytes = CopyArray(
            packet.ToArray(out var bodyLength),
            BitConverter.GetBytes(header), BitConverter.GetBytes(bodyLength));
        await SendBytes(bytes.Array!);
        await SenderPacket.ResetAsync();
    }
    
    

    private ArraySegment<byte> CopyArray(byte[] body, byte[] headerBytes, byte[] bodyLengthBytes)
    {
        ArraySegment<byte> bytes = new byte[HeaderSize + BodyLengthSizeInBuffer + body.Length];

        Array.Copy(headerBytes,
            0, bytes.Array!, 0, headerBytes.Length);

        Array.Copy(bodyLengthBytes, 0, bytes.Array!, HeaderSize, bodyLengthBytes.Length);

        Array.Copy(body, 0, bytes.Array!,
            BodyLengthSizeInBuffer + HeaderSize, body.Length);
        return bytes;
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
    
    public void RegisterOnServerConnected<TClass>() where TClass : BaseTcpClientConnected, new()
    {
        if (alreadyRegistered) return;
        clientConnected = new TClass();
        alreadyRegistered = true;
    }
    
    private async Task SendBytes(byte[] bufferWithHeader) => 
        await _client.GetStream().WriteAsync(bufferWithHeader, 0, bufferWithHeader.Length);
}