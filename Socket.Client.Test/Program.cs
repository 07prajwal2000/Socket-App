using System.Text;
using Shared;
using Socket.Client.Test;

var testClient = new TestClient();

await testClient.Start();

// using var packet = new NetworkPacket();
// packet.WriteBool(true);
// packet.WriteChar('a');
// packet.WriteString("prajwal");
// packet.WriteInt(1000);
// packet.WriteBytes("Aradhya");
// using var packet2 = new NetworkPacket(packet.ToBytesSegment().Array!);
// Console.WriteLine(packet2.ReadBool()); // true
// Console.WriteLine(packet2.ReadChar()); // 'a'
// Console.WriteLine(packet2.ReadString()); // prajwal
// Console.WriteLine(packet2.ReadInt()); // 1000
// Console.WriteLine(Encoding.UTF8.GetString(packet2.ReadBytes())); // Aradhya
// Console.WriteLine();