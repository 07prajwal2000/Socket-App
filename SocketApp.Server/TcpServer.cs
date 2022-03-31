using System.Net;
using System.Net.Sockets;
using System.Text;
using Shared;
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
                
                _registers.TryGetValue( _buffer[0], out var baseTcpRegister);
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

            var headerData = memoryBuffer.Slice(20, headerLength);
            var bodyData = memoryBuffer.Slice(20 + headerLength);

            var eventArgs = new MessageReceivedEventArgs(_totalConnections)
            {
                Bytes = _buffer,
                ClientSocket = client,
                TotalBytesRead = bytesRead,
                ConnectedClients = _clientSockets,
                Body = bodyData,
                Header = headerData
            };
            
            OnMessageReceived?.Invoke(this, eventArgs);
            _registers.TryGetValue( _buffer[0], out var baseTcpRegister);
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
    /// <param name="dataType">Set the type of Data you want to register with. when any data with that header comes,
    /// the corresponding function will be called.</param>
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

    /// <summary>
    /// Send Data to Client Socket.<br/>
    /// first 10 bytes of data is Assigned to read headerLength, 
    /// next 10 bytes of data is Assigned to read BodyLength,
    /// after that starts the Header Data and ranges to headerLength
    /// same followed for Buffer
    /// </summary>
    /// <param name="client">the client to send the data.</param>
    /// <param name="socket">Extension for Socket Class</param>
    /// <param name="headerSize">Length of header</param>
    /// <param name="bodySize">Length of body</param>
    /// <param name="header">Header Data</param>
    /// <param name="body">Body Data.</param>
    public async Task SendBytes(System.Net.Sockets.Socket client, byte[] header, byte[] body)
    {
        long headerSize = header.Length;
        long bodySize = _packetSize;
            
        long hLengthForString = header.Length;
        long bLengthForString = body.Length;
        
        ArraySegment<byte> bytes = new byte[headerSize + bodySize];

        // take 10 bytes
        var headerToArray = Encoding.UTF8.GetBytes(hLengthForString.ToString());
        Array.Copy(headerToArray,
            0, bytes.Array!, 0, headerToArray.Length);
        
        // take 10 bytes
        var bodyLengthToArray = Encoding.UTF8.GetBytes(bLengthForString.ToString());
        Array.Copy(bodyLengthToArray,
            0, bytes.Array!, 10, bodyLengthToArray.Length);
        
        // starts from 20 to headerLength
        Array.Copy(header, 0, bytes.Array!,
            20, header.Length);
        
        // starts from 20 + headerLength to bodyLength
        Array.Copy(body, 0, bytes.Array!,
            20 + headerSize, body.Length);
        
        await SendBytes(client, bytes);
    }
    
    private async Task SendBytes(System.Net.Sockets.Socket client, ArraySegment<byte> bufferWithHeader) => 
        await client.SendAsync(bufferWithHeader, SocketFlags.None);
}