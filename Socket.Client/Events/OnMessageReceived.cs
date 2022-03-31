using System.Net.Sockets;

namespace Socket.Client.Events;

public delegate void OnMessageReceived<in TSender>(TSender sender, MessageReceivedEventArgs eventArgs);

public struct MessageReceivedEventArgs
{
    public byte[] Bytes { get; set; }
    public int TotalBytesRead { get; set; }
    public NetworkStream NetworkStream { get; set; }
    public ReadOnlyMemory<byte> Header { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }
}