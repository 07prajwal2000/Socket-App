using System.Net;
using System.Net.Sockets;
using System.Text;
using Socket.Server.Events;

namespace Socket.Server;

public class TcpServer
{
    private readonly List<System.Net.Sockets.Socket> _clientSockets = new();

    private readonly Dictionary<uint, BaseTcpServerRegister> _registers = new();
    private int _totalConnections;

    private readonly int _packetSize;
    private readonly byte[] _buffer;

    public readonly TcpListener TcpListener;
    private bool _serverStarted;

    public static OnClientConnected? OnClientConnected;
    public static OnExceptionRaised? OnExceptionRaised;
    public static OnMessageReceived<TcpServer>? OnMessageReceived;
    
    public TcpServer()
    {
        var port = 2500;
        _packetSize = 1024;
        _buffer = new byte[_packetSize];
        TcpListener = new(IPAddress.Loopback, port);
    }

    public TcpServer(IPAddress ipAddress, int port = 2500, int packetSize = 1024)
    {
        _packetSize = packetSize;
        _buffer = new byte[_packetSize];
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
                
                _registers.TryGetValue( 0, out var baseTcpRegister);
                baseTcpRegister?.OnClientConnected(this, eventArgs);

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

    private void ReceiveCallback(IAsyncResult ar, System.Net.Sockets.Socket client)
    {
        var bytesRead = 0;
        try
        {
            bytesRead = client.EndReceive(ar);
            if (bytesRead is 0) return;
            
            ReadOnlyMemory<byte> memoryBuffer = _buffer;
            
            var headerLengthAsBytes = memoryBuffer.Slice(0, 10).Span;
            var hs = Encoding.UTF8.GetString(headerLengthAsBytes);
            int.TryParse(hs, out var headerLength);

            var bodyData = memoryBuffer.Slice(20 + headerLength);

            var eventArgs = new MessageReceivedEventArgs(_totalConnections)
            {
                Bytes = _buffer,
                ClientSocket = client,
                TotalBytesRead = bytesRead,
                ConnectedClients = _clientSockets,
                Body = bodyData,
                Header = ReadHeader(_buffer)
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
                client.BeginReceive(_buffer, 0, _packetSize, SocketFlags.None, result => ReceiveCallback(result, client), null);
        }
    }

    public void Stop() => TcpListener.Stop();

    /// <summary>
    /// Use This Method to Register your Events and Get Access to Properties
    /// </summary>
    /// <typeparam name="TClass"> TClass should Implement <see cref="BaseTcpServerRegister"/></typeparam>
    /// <exception cref="ArgumentException">If the Datatype Already Exists.</exception>
    public void Register<TClass>(uint dataHeader) where TClass : BaseTcpServerRegister, new()
    {
        if (_registers.ContainsKey(dataHeader))
        {
            throw new ArgumentException($"Already Registered with the type of {dataHeader} in Register() method");
        }
        BaseTcpServerRegister tcpServerRegister = new TClass();
        _registers.TryAdd(dataHeader, tcpServerRegister);
        tcpServerRegister.RegisterEvents(this);
    }

    public uint ReadHeader(byte[] buf)
    {
        ReadOnlyMemory<byte> buffer = buf;

        var headerLength = buffer.Slice(0, 10);
        int.TryParse(Encoding.UTF8.GetString( headerLength.Span ), out var lengthAsNum);

        var headerAsMemory = buffer.Slice(20, lengthAsNum);

        uint.TryParse(Encoding.UTF8.GetString(headerAsMemory.Span), out uint header);

        return header;
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
    public async Task SendBytes(System.Net.Sockets.Socket client, uint header, byte[] body)
    {
        byte[] headerBytes = Encoding.UTF8.GetBytes(header.ToString());
        
        long headerSize = headerBytes.Length;
        
        long hLengthForString = headerBytes.Length;
        long bLengthForString = body.Length;
        
        ArraySegment<byte> bytes = new byte[_packetSize];

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
        
        await SendBytes(client, bytes);
    }
    
    private async Task SendBytes(System.Net.Sockets.Socket client, ArraySegment<byte> bufferWithHeader) => 
        await client.SendAsync(bufferWithHeader, SocketFlags.None);
}