using System.Net.Sockets;
using Shared;

namespace Socket.Client.Events;

public delegate void OnMessageReceived<in TSender>(TSender sender, MessageReceivedEventArgs eventArgs);

public struct MessageReceivedEventArgs
{
    public int TotalBytesRead { get; set; }
    public int TotalNumberOfBytesContainsInBody { get; set; }
    public NetworkStream NetworkStream { get; set; }
    public uint Header { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }
    /// <summary>
    /// Used only for Reading the data not for Writing.
    /// </summary>
    public NetworkPacket NetworkPacket { get; set; }
}