using System.Text;

namespace Shared;

public class NetworkPacket : IDisposable
{
    private ArraySegment<byte> _buffer;
    public int PacketSize {get;}
    public int WritePosition { get; private set; }
    public int ReadPosition { get; private set; }

    public NetworkPacket(int packetSize = 1024)
    {
        PacketSize = packetSize;
        _buffer = new byte[PacketSize];
    }
    public NetworkPacket(byte[] data)
    {
        PacketSize = data.Length;
        _buffer = data;
    }

    public void WriteBytes(byte[] data)
    {
        if (data.Length > PacketSize)
            throw new IndexOutOfRangeException("Data size exceeds the Packet size");
        WriteInt(data.Length);
        Array.Copy(data, 0, _buffer.Array!, WritePosition, data.Length);
        WritePosition += data.Length - 1;
    }
    public void WriteBytes(ReadOnlySpan<char> data)
    {
        if (data.Length > PacketSize)
            throw new IndexOutOfRangeException("Data size exceeds the Packet size");
        WriteInt(data.Length);
        for (int i = 0; i < data.Length; i++)
        {
            _buffer[WritePosition] = Convert.ToByte(data[i]);
            WritePosition++;
        }
    }
    private void _Write(byte[] converted)
    {
        foreach (var b in converted)
        {
            _buffer[WritePosition] = b;
            WritePosition++;
        }
    }
    
    public void WriteBool(bool value) => _Write(BitConverter.GetBytes(value));
    public void WriteChar(char value) => _Write(BitConverter.GetBytes(value));
    public void WriteInt(int value) => _Write(BitConverter.GetBytes(value));
    public void WriteUnsignedInt(uint value) => _Write(BitConverter.GetBytes(value));
    public void WriteFloat(float value) => _Write(BitConverter.GetBytes(value));
    public void WriteDouble(double value) => _Write(BitConverter.GetBytes(value));
    public void WriteLong(long value) => _Write(BitConverter.GetBytes(value));
    public void WriteUnsignedLong(ulong value) => _Write(BitConverter.GetBytes(value));
    public void WriteString(string value) => WriteBytes(value);

    public bool ReadBool()
    {
        bool value = BitConverter.ToBoolean(_buffer.Array!, ReadPosition);
        ReadPosition++;
        return value;
    }
    public char ReadChar()
    {
        char value = BitConverter.ToChar(_buffer.Array!, ReadPosition);
        ReadPosition += 2;
        return value;
    }
    public int ReadInt()
    {
        int value = BitConverter.ToInt32(_buffer.Array!, ReadPosition);
        ReadPosition += 4;
        return value;
    }
    public uint ReadUInt()
    {
        uint value = BitConverter.ToUInt32(_buffer.Array!, ReadPosition);
        ReadPosition += 4;
        return value;
    }
    public float ReadFloat()
    {
        float value = BitConverter.ToSingle(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public double ReadDouble()
    {
        double value = BitConverter.ToDouble(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public long ReadLong()
    {
        long value = BitConverter.ToInt64(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public ulong ReadULong()
    {
        ulong value = BitConverter.ToUInt64(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public string ReadString()
    {
        var strLength = ReadInt();
        var str = Encoding.UTF8.GetString(_buffer.Array!, ReadPosition, strLength);
        ReadPosition += strLength;
        return str;
    }
    public byte[] ReadBytes()
    {
        var bytesLength = ReadInt();
        ReadOnlySpan<byte> spanBytes = _buffer;
        var res = spanBytes.Slice(ReadPosition, bytesLength);
        ReadPosition += bytesLength;
        return res.ToArray();
    }
    public ReadOnlyMemory<byte> ReadBytesAsMemory()
    {
        var bytesLength = ReadInt();
        ReadOnlyMemory<byte> spanBytes = _buffer;
        var res = spanBytes.Slice(ReadPosition, bytesLength);
        ReadPosition += bytesLength;
        return res;
    }

    public byte[] ToBytes() => _buffer.Array!;
    public ArraySegment<byte> ToBytesSegment() => _buffer;

    #region Disposal

    private bool disposed;
    public void Dispose()
    {
        _Dispose(disposed);
        GC.SuppressFinalize(this);
        
        void _Dispose(bool _disposing)
        {
            if (_disposing) return;
            _buffer = ArraySegment<byte>.Empty;
            WritePosition = 0;
            ReadPosition = 0;
            disposed = true;
        }
    }
    
    #endregion
}

//1 - bool
//2 - char
//8 - float
//8 - double
//4 - int
//4 - uint
//8 - long
//8 - ulong
//1 - bool
//2 - char
//8 - float
//8 - double
//4 - int
//4 - uint
//8 - long
//8 - ulong

// using var packet = new NetworkPacket();
// packet.WriteBool(true);
// packet.WriteChar('a');
// packet.WriteString("prajwal");
// packet.WriteInt(1000);
// packet.WriteBytes("Aradhya");
//
// Console.WriteLine(packet.ReadBool()); // true
// Console.WriteLine(packet.ReadChar()); // 'a'
// Console.WriteLine(packet.ReadString()); // prajwal
// Console.WriteLine(packet.ReadInt()); // 1000
// Console.WriteLine(Encoding.UTF8.GetString(packet.ReadBytes())); // Aradhya
// Console.WriteLine();