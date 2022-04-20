using Shared;

namespace SocketApp.Server.Events;

/// <summary>
/// Called when a message received from the Client
/// </summary>
/// <param name="responsePacket">Used only for reading the buffer. if writing happens will throw <see cref="InvalidOperationException"/></param>
/// <param name="eventArgs">Contains Data</param>
/// <typeparam name="TSender">Implementation</typeparam>
public delegate void OnMessageReceived<in TSender>(TSender sender, MessageReceivedEventArgs eventArgs, NetworkPacket responsePacket);

public class MessageReceivedEventArgs
{
    public System.Net.Sockets.Socket ClientSocket { get; set; }
    public int TotalBytesContaining { get; set; }
    public int TotalBytesContainingInBody { get; set; }
    public uint Header { get; set; }
    public ReadOnlyMemory<byte> Body { get; set; }
    
    /// <summary>
    /// Used only for reading the buffer. if writing happens will throw <see cref="InvalidOperationException"/>
    /// </summary>
    public NetworkPacket NetworkPacket { get; set; }
}