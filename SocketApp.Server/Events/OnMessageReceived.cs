using Shared;

namespace SocketApp.Server.Events;

public delegate void OnMessageReceived<in TSender>(TSender sender, MessageReceivedEventArgs eventArgs);

public class MessageReceivedEventArgs
{
    public MessageReceivedEventArgs(int totalConnections)
    {
        TotalConnections = totalConnections;
    }
    
    public readonly int TotalConnections;
    public System.Net.Sockets.Socket ClientSocket { get; set; }
    public int TotalBytesContaining { get; set; }
    public int TotalNumberOfDataContainsInBody { get; set; }
    public List<System.Net.Sockets.Socket> ConnectedClients { get; set; }
    public uint Header { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }
    
    /// <summary>
    /// Used only for reading the buffer. if writing happens will throw <see cref="InvalidOperationException"/>
    /// </summary>
    public NetworkPacket NetworkPacket { get; set; }
}