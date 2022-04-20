using System.Text;

namespace Shared;

public class NetworkPacket : IDisposable
{
    private ArraySegment<byte> _buffer;
    private readonly ReadOnlyMemory<byte> _memoryBuffer;
    public int PacketSize {get;}
    public int WritePosition { get; private set; }
    public int ReadPosition { get; private set; }
    public readonly bool CanWrite;
    public readonly bool IsReading = false;

    public NetworkPacket(int packetSize = 1004, bool canWrite = true)
    {
        PacketSize = packetSize;
        _buffer = new byte[PacketSize];
        CanWrite = canWrite;
    }
    public NetworkPacket(byte[] data, bool canWrite = true)
    {
        PacketSize = data.Length;
        _buffer = data;
        CanWrite = canWrite;
    }
    /// <summary>
    /// Only for Reading Purpose.
    /// </summary>
    /// <param name="memoryBuffer"></param>
    /// <param name="isReading">Is this instance used for Reading</param>
    public NetworkPacket(ReadOnlyMemory<byte> memoryBuffer, bool isReading = true)
    {
        PacketSize = memoryBuffer.Length;
        _memoryBuffer = memoryBuffer;
        IsReading = isReading;
    }

    public void WriteBytes(byte[] data)
    {
        if (!CanWrite)
            throw new InvalidOperationException("Cannot Write the Data");
        
        if (data.Length > PacketSize)
            throw new IndexOutOfRangeException("Data size exceeds the Packet size");
        WriteInt(data.Length);
        Array.Copy(data, 0, _buffer.Array!, WritePosition, data.Length);
        WritePosition += data.Length - 1;
    }
    public void WriteBytes(ReadOnlySpan<char> data)
    {
        if (!CanWrite)
            throw new InvalidOperationException("Cannot Write the Data");

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
        if (!CanWrite)
            throw new InvalidOperationException("Cannot Write the Data");

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
        if (IsReading)
        {
            bool val = BitConverter.ToBoolean(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition++;
            return val;
        }
        bool value = BitConverter.ToBoolean(_buffer.Array!, ReadPosition);
        ReadPosition++;
        return value;
    }
    public char ReadChar()
    {
        if (IsReading)
        {
            char val = BitConverter.ToChar(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition += 2;
            return val;
        }
        char value = BitConverter.ToChar(_buffer.Array!, ReadPosition);
        ReadPosition += 2;
        return value;
    }
    public int ReadInt()
    {
        if (IsReading)
        {
            int val = BitConverter.ToInt32(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition += 4;
            return val;
        }
        int value = BitConverter.ToInt32(_buffer.Array!, ReadPosition);
        ReadPosition += 4;
        return value;
    }
    public uint ReadUInt()
    {
        if (IsReading)
        {
            uint val = BitConverter.ToUInt32(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition += 4;
            return val;
        }
        uint value = BitConverter.ToUInt32(_buffer.Array!, ReadPosition);
        ReadPosition += 4;
        return value;
    }
    public float ReadFloat()
    {
        if (IsReading)
        {
            float val = BitConverter.ToSingle(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition += 8;
            return val;
        }
        float value = BitConverter.ToSingle(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public double ReadDouble()
    {
        if (IsReading)
        {
            double val = BitConverter.ToDouble(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition += 8;
            return val;
        }
        double value = BitConverter.ToDouble(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public long ReadLong()
    {
        if (IsReading)
        {
            long val = BitConverter.ToInt64(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition += 8;
            return val;
        }
        long value = BitConverter.ToInt64(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public ulong ReadULong()
    {
        if (IsReading)
        {
            ulong val = BitConverter.ToUInt64(_memoryBuffer.Span.Slice(ReadPosition));
            ReadPosition += 8;
            return val;
        }
        ulong value = BitConverter.ToUInt64(_buffer.Array!, ReadPosition);
        ReadPosition += 8;
        return value;
    }
    public string ReadString()
    {
        var strLength = ReadInt();
        if (IsReading)
        {
            string val = Encoding.UTF8.GetString(_memoryBuffer.Span.Slice(ReadPosition, strLength));
            ReadPosition += strLength;
            return val;
        }
        var str = Encoding.UTF8.GetString(_buffer.Array!, ReadPosition, strLength);
        ReadPosition += strLength;
        return str;
    }
    public byte[] ReadBytes()
    {
        var bytesLength = ReadInt();
        if (IsReading)
        {
            byte[] val = _memoryBuffer.Slice(ReadPosition).ToArray();
            ReadPosition += bytesLength;
            return val;
        }
        ReadOnlySpan<byte> spanBytes = _buffer;
        var res = spanBytes.Slice(ReadPosition, bytesLength);
        ReadPosition += bytesLength;
        return res.ToArray();
    }
    public ReadOnlyMemory<byte> ReadBytesAsMemory()
    {
        var bytesLength = ReadInt();
        if (IsReading)
        {
            ReadOnlyMemory<byte> val = _memoryBuffer.Slice(ReadPosition, bytesLength);
            ReadPosition += bytesLength;
            return val;
        }
        ReadOnlyMemory<byte> spanBytes = _buffer;
        var res = spanBytes.Slice(ReadPosition, bytesLength);
        ReadPosition += bytesLength;
        return res;
    }

    public byte[] ToArray() => _buffer.Array!;

    public byte[] ToArray(out int totalBytesWrote)
    {
        totalBytesWrote = WritePosition;
        return _buffer.Array!;
    }
    public ArraySegment<byte> ToBytesSegment() => _buffer;

    public async Task ResetAsync()
    {
        _buffer = await Task.Run(() => ResetToZero(_buffer));
        WritePosition = 0;
        ReadPosition = 0;
    }
    
    public async void Reset()
    {
        _buffer = await Task.Run(() => ResetToZero(_buffer));
        WritePosition = 0;
        ReadPosition = 0;
    }

    ArraySegment<byte> ResetToZero(ArraySegment<byte> bytes)
    {
        for (var i = 0; i < bytes.Count; i++) bytes[i] = 0;
        return bytes;
    }
    
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