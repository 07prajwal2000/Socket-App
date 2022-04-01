using System.Net;
using System.Net.Sockets;
using SocketApp.Server.Events;

namespace SocketApp.Server;

public class TcpServer
{
    private readonly List<Socket> _clientSockets = new();

    private readonly Dictionary<uint, BaseTcpServerRegister> _registers = new();
    private int _totalConnections;
    private readonly byte[] _buffer;

    public readonly int PacketSize;
    public readonly int HeaderSize = 10;
    /// <summary>
    /// The range starts from HeaderSize to this Size contains the Size of the Body which is sent from the Server.
    /// </summary>
    public readonly int BodyLengthSizeInBuffer = 10;
    
    public readonly TcpListener TcpListener;
    private bool _serverStarted;
    private bool _alreadyRegisteredForClientConnectedEvent;
    private BaseTcpRegisterOnClientConnection _onClientConnection;

    public static OnClientConnected? OnClientConnected;
    public static OnExceptionRaised? OnExceptionRaised;
    public static OnMessageReceived<TcpServer>? OnMessageReceived;
    
    public TcpServer()
    {
        var port = 2500;
        PacketSize = 1024;
        _buffer = new byte[PacketSize];
        TcpListener = new(IPAddress.Loopback, port);
    }

    public TcpServer(IPAddress ipAddress, int port = 2500, int packetSize = 1024, int headerSize = 10, int bodyLengthSizeInBuffer = 10)
    {
        HeaderSize = headerSize;
        BodyLengthSizeInBuffer = bodyLengthSizeInBuffer;
        PacketSize = packetSize;
        _buffer = new byte[PacketSize];
        TcpListener = new(ipAddress, port);
    }

    public async Task Start()
    {
        if (!_serverStarted)
        {
            TcpListener.Start();
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
                    .FromAsync(TcpListener.Server.BeginAccept, TcpListener.Server.EndAccept, null)
                    .ConfigureAwait(false);
            
                _totalConnections++;
                _clientSockets.Add(clientSocket);

                var eventArgs = new ClientConnectedEventArgs
                {
                    TotalConnections = _totalConnections,
                    ClientSocket = clientSocket,
                    ConnectedClients = _clientSockets
                };
                
                OnClientConnected?.Invoke(this, eventArgs);
                
                _onClientConnection?.OnClientConnected(this, eventArgs);

                clientSocket.BeginReceive(_buffer, 0, PacketSize, SocketFlags.None, 
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
            var body = bufferMemory.Slice(HeaderSize + BodyLengthSizeInBuffer, bodyLength);
            
            var eventArgs = new MessageReceivedEventArgs(_totalConnections)
            {
                ClientSocket = client,
                TotalBytesContaining = bytesRead,
                ConnectedClients = _clientSockets,
                Body = body,
                TotalNumberOfDataContainsInBody = bodyLength, 
                Header = header
            };
            
            OnMessageReceived?.Invoke(this, eventArgs);
            _registers.TryGetValue( ReadHeader(_buffer), out var baseTcpRegister);
            baseTcpRegister?.OnMessageReceived(this, eventArgs);
        }
        catch (Exception e)
        {
            OnExceptionRaised?.Invoke(e);
        }
        finally
        {
            if (bytesRead > 0)
                client.BeginReceive(_buffer, 0, PacketSize, SocketFlags.None, result => ReceiveCallback(result, client), null);
        }
    }

    public void Stop() => TcpListener.Stop();

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
        
        ArraySegment<byte> bytes = new byte[HeaderSize + BodyLengthSizeInBuffer + body.Length];

        Array.Copy(headerBytes,
            0, bytes.Array!, 0, headerBytes.Length);
        
        Array.Copy(bodyLengthBytes, 0, bytes.Array!, HeaderSize, bodyLengthBytes.Length);
        
        Array.Copy(body, 0, bytes.Array!,
            BodyLengthSizeInBuffer + HeaderSize, body.Length);
        
        await SendBytes(client, bytes.Array!);
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