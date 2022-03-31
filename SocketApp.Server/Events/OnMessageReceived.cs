namespace Socket.Server.Events;

public delegate void OnMessageReceived<in TSender>(TSender sender, MessageReceivedEventArgs eventArgs);

public class MessageReceivedEventArgs
{
    public MessageReceivedEventArgs(int totalConnections)
    {
        TotalConnections = totalConnections;
    }
    
    public readonly int TotalConnections;
    public System.Net.Sockets.Socket ClientSocket { get; set; }
    public byte[] Bytes { get; set; }
    public int TotalBytesRead { get; set; }
    public List<System.Net.Sockets.Socket> ConnectedClients { get; set; }
    public uint Header { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }
}