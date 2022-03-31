using System.Net.Sockets;
using System.Text;
using Socket.Server;
using Socket.Server.Demo;
using Socket.Server.Events;

var testServer = new TestServer();

await testServer.Start();


// byte[] headerLength = Encoding.UTF8.GetBytes("50");
// byte[] bodyLength = Encoding.UTF8.GetBytes("1024");
//
// byte[] buffer = new byte[2048];
// Array.Copy(headerLength, 0, buffer, 0, headerLength.Length);
// Array.Copy(bodyLength, 0, buffer, 10, bodyLength.Length);
//
// Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, 20));

// var ArraySegment = new ArraySegment<byte>(new byte[10]);
// Array.Copy(Encoding.UTF8.GetBytes("2555"), ArraySegment.Array, 4);
// Console.WriteLine(Encoding.UTF8.GetString(ArraySegment.Array) + "|");

// 10 bytes tells -> header length
// next 10 bytes tells -> body length

// Starts reading Header -> from 20th position to header length
// after that reads Body -> from 20th position + header lenght to BodyLenght length