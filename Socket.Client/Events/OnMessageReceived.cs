using System.Net.Sockets;
using Shared;

namespace Socket.Client.Events;

/// <summary>
/// Called When a message comes to the client.
/// </summary>
/// <param name="sender">Tcp client as a sender</param>
/// <param name="args">Useful arguments that contains the data and some others...</param>
/// <param name="responsePacket">Use this packet class to write data and send it to server.</param>
public delegate void OnMessageReceived<in TSender>(TSender sender, MessageReceivedEventArgs args, NetworkPacket responsePacket);

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