using System.Net;
using System.Net.Sockets;
using Shared;
using SocketApp.Server.Events;

namespace SocketApp.Server;

public class TcpServer
{
    private readonly Dictionary<uint, BaseTcpServerRegister> _registers = new();
    private int _totalConnections;
    private readonly byte[] _buffer;

    private readonly int _packetSize;
    private readonly int _headerSize = 10;
    private readonly int _bodyLengthSizeInBuffer = 10;
    
    /// <summary>
    /// The range starts from HeaderSize to this Size contains the Size of the Body which is sent from the Server.
    /// </summary>
    private int TotalAvailableBodyLength => _packetSize - (_bodyLengthSizeInBuffer + _headerSize);

    private readonly TcpListener _tcpListener;
    private bool _serverStarted;
    private bool _alreadyRegisteredForClientConnectedEvent;
    private BaseTcpRegisterOnClientConnection _onClientConnection;

    public static OnClientConnected? OnClientConnected;
    public static OnExceptionRaised? OnExceptionRaised;
    public static OnMessageReceived<TcpServer>? OnMessageReceived;
    
    public TcpServer()
    {
        const int port = 2500;
        _packetSize = 1024;
        _buffer = new byte[_packetSize];
        _tcpListener = new TcpListener(IPAddress.Loopback, port);
    }

    public TcpServer(IPAddress ipAddress, int port = 2500, int packetSize = 1024, int headerSize = 10, int bodyLengthSizeInBuffer = 10)
    {
        _headerSize = headerSize;
        _bodyLengthSizeInBuffer = bodyLengthSizeInBuffer;
        _packetSize = packetSize;
        _buffer = new byte[_packetSize];
        _tcpListener = new(ipAddress, port);
    }

    public async Task Start()
    {
        if (!_serverStarted)
        {
            _tcpListener.Start();
            await Task.Run(StartServer).ConfigureAwait(false);
            _serverStarted = true;
        }

    }

    private async Task StartServer()
    {
        do
        {
            try
            {
                var clientSocket = await Task.Factory
                    .FromAsync(_tcpListener.Server.BeginAccept, _tcpListener.Server.EndAccept, null)
                    .ConfigureAwait(false);
            
                _totalConnections++;

                var eventArgs = new ClientConnectedEventArgs
                {
                    TotalConnections = _totalConnections,
                    ClientSocket = clientSocket,
                    NetworkPacket = new NetworkPacket(TotalAvailableBodyLength)
                };
                
                OnClientConnected?.Invoke(this, eventArgs);
                
                _onClientConnection?.OnClientConnected(this, eventArgs);

                clientSocket.BeginReceive(_buffer, 0, _packetSize, SocketFlags.None, 
                    ar =>  ReceiveCallback(ar, clientSocket), clientSocket);
            }
            catch (Exception e)
            {
                OnExceptionRaised?.Invoke(e);
            }
            
        } while (true);
        // ReSharper disable once FunctionNeverReturns
    }

    private void ReceiveCallback(IAsyncResult ar, Socket client)
    {
        var bytesRead = 0;
        try
        {
            bytesRead = client.EndReceive(ar);
            if (bytesRead is 0) return;

            var header = ReadHeader(_buffer);
            var bodyLength = ReadBodyLength(_buffer);

            ReadOnlyMemory<byte> bufferMemory = _buffer;
            var body = bufferMemory.Slice(_headerSize + _bodyLengthSizeInBuffer, bodyLength);
            
            var eventArgs = new MessageReceivedEventArgs
            {
                ClientSocket = client,
                TotalBytesContaining = bytesRead,
                Body = body,
                TotalBytesContainingInBody = bodyLength, 
                Header = header,
                NetworkPacket = new NetworkPacket(body.ToArray(), false)
            };
            
            var responsePacket = new NetworkPacket(TotalAvailableBodyLength);
            
            OnMessageReceived?.Invoke(this, eventArgs, responsePacket);
            _registers.TryGetValue( header, out var baseTcpRegister);
            baseTcpRegister?.OnMessageReceived(this, eventArgs, responsePacket);
        }
        catch (Exception e)
        {
            OnExceptionRaised?.Invoke(e);
        }
        finally
        {
            if (bytesRead > 0)
                client.BeginReceive(_buffer, 0, _packetSize, SocketFlags.None, result => ReceiveCallback(result, client), null);
        }
    }

    public void Stop()
    {
        _tcpListener.Stop();
    }

    /// <summary>
    /// Use This Method to Register your Events and Get Access to Properties
    /// </summary>
    /// <typeparam name="TClass"> TClass should Implement <see cref="BaseTcpServerRegister"/></typeparam>
    /// <exception cref="ArgumentException">If the Datatype Already Exists.</exception>
    public bool Register<TClass>(uint dataHeader) where TClass : BaseTcpServerRegister, new()
    {
        if (_registers.ContainsKey(dataHeader))
        {
            throw new ArgumentException($"Already Registered with the type of {dataHeader} in Register() method");
        }
        BaseTcpServerRegister tcpServerRegister = new TClass();
        return _registers.TryAdd(dataHeader, tcpServerRegister);
    }

    /// <summary>
    /// Register the class which inherits <see cref="BaseTcpRegisterOnClientConnection"/> <br/>
    /// when client connected to server Event is fired. <br/>
    /// Remember : Only once should be Registered. Multiple Registrations will throw Exception.
    /// </summary>
    /// <exception cref="Exception">Throws when Multiple Registration happens</exception>
    public void RegisterForClientConnected<TClass>() where TClass : BaseTcpRegisterOnClientConnection, new()
    {
        if (_alreadyRegisteredForClientConnectedEvent)
        {
            throw new Exception($"Already Registered for {nameof(RegisterForClientConnected)}. You can Register only once for this type of event.");
        }
        _onClientConnection = new TClass();
        _alreadyRegisteredForClientConnectedEvent = true;
    }

    
    /// <summary>
    /// Send Data to Client Socket.<br/>
    /// first 10 bytes of data is Assigned to read headerLength, 
    /// next 10 bytes of data is Assigned to read BodyLength,
    /// after that starts the Header Data and ranges to headerLength
    /// same followed for Buffer
    /// </summary>
    /// <param name="client">the client to send the data.</param>
    /// <param name="header">Header Data should be uint. This is later used for Calling specific Method When Registered on Start of the server.<see cref="uint"/></param>
    /// <param name="body">Body Data.</param>
    public async Task SendBytes(Socket client, uint header, byte[] body)
    {
        byte[] headerBytes = BitConverter.GetBytes(header);

        byte[] bodyLengthBytes = BitConverter.GetBytes(body.Length);
        
        var bytes = CopyArray(body, headerBytes, bodyLengthBytes);
        
        await SendBytes(client, bytes.Array!);
    }

    /// <summary>
    /// Send Data to Client Socket.<br/>
    /// first 10 bytes of data is Assigned to read headerLength, 
    /// next 10 bytes of data is Assigned to read BodyLength,
    /// after that starts the Header Data and ranges to headerLength
    /// same followed for Buffer
    /// </summary>
    /// <param name="client">the client to send the data.</param>
    /// <param name="header">Header Data should be uint. This is later used for Calling specific Method When Registered on Start of the server.<see cref="uint"/></param>
    /// <param name="body">Body Data.</param>
    /// <param name="bodyLength">Total no of bytes written into buffer. or Total bytes containing in body</param>
    public async Task SendBytes(Socket client, uint header, byte[] body, int bodyLength)
    {
        byte[] headerBytes = BitConverter.GetBytes(header);

        byte[] bodyLengthBytes = BitConverter.GetBytes(bodyLength);
        
        var bytes = CopyArray(body, headerBytes, bodyLengthBytes);

        await SendBytes(client, bytes.Array!);
    }

    private ArraySegment<byte> CopyArray(byte[] body, byte[] headerBytes, byte[] bodyLengthBytes)
    {
        ArraySegment<byte> bytes = new byte[_headerSize + _bodyLengthSizeInBuffer + body.Length];

        Array.Copy(headerBytes,
            0, bytes.Array!, 0, headerBytes.Length);

        Array.Copy(bodyLengthBytes, 0, bytes.Array!, _headerSize, bodyLengthBytes.Length);

        Array.Copy(body, 0, bytes.Array!,
            _bodyLengthSizeInBuffer + _headerSize, body.Length);
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
    
    private async Task SendBytes(Socket client, ArraySegment<byte> bufferWithHeader) => 
        await client.SendAsync(bufferWithHeader, SocketFlags.None);
}