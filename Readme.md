# Simple Socket Implementation
### Easy to Implement and use 

<p>Still in Development !</p>

### Create Server class Instance and Start the Server
```c#
var server = new TcpServer();
await server.Start();
```

### You can register for callbacks when server receives a connection from a client
#### Note: These Events are Static Delegates and can be Subscribed from anywhere.
```c#
var server = new TcpServer();
TcpServer.OnClientConnected += OnClientConnected;

await server.Start();

Console.ReadLine(); // use this for avoid closing the console window or exit.

server.Stop(); // used to stop server.
```
```c#
var server = new TcpServer();

TcpServer.OnClientConnected += OnClientConnected;
TcpServer.OnMessageReceived += OnMessageReceivedCallback;

await server.Start();
```

### To decouple the logic use Register() and RegisterForClientConnected() method and register a class
#### When message received Registered class methods will automatically invoked.
```c#
// To register any class it must implement the ' BaseTcpRegisterOnClientConnection ' class
server.RegisterForClientConnected<SendClientDetails>(); // Fired when Client Connects.

public class SendClientDetails : BaseTcpRegisterOnClientConnection
{
    public override async void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args)
    {
        Console.WriteLine("Client Connected. Total Connections: " + args.TotalConnections);
    }
}

// For Receiving Message. This method accepts uint header which is specifically called only a method by not disturbing other methods
server.Register<ReceiveMessage>(HeaderConstants.ClientDetails);
public class ReceiveMessage : BaseTcpServerRegister
{
    public override void OnMessageReceived(TcpServer sender, MessageReceivedEventArgs args)
    {
        var body = Encoding.UTF8.GetString(args.Body.Span);
        Console.WriteLine("Body: " + body);
    }
}
```

### For client side
```c#
var client = new ClientTcp();

// Register when client connected to server 
client.RegisterOnServerConnected<OnServerConnected>();

// you can use static Events to register 
ClientTcp.OnMessageReceived += OnMessageReceived;
// or use the modular one
client.Register<GetIdFromServer>(HeaderConstants.ClientDetails);

await client.Start(); // start connecting to server

Console.ReadLine(); // use this for avoid closing the console window or exit.

client.Stop(); // used to Disconnect from server.
```

### Writing/Sending data to client
```c#
public class SendClientDetails : BaseTcpRegisterOnClientConnection // <- Abstract class used for calling method
{
    public override async void OnClientConnected(TcpServer sender, ClientConnectedEventArgs args)
    {
        Console.WriteLine("Client Connected. Total Connections: " + args.TotalConnections);

        var netPacket = args.NetworkPacket; //<- This Network Packet is used for Writing Data to it.
        //Note: Some network Packets are ReadOnly so don't call Read Methods on them. 
        //If called will throw Exception
        
        netPacket.WriteInt(args.TotalConnections);
        netPacket.WriteBool(true);
        netPacket.WriteString("Prajwal Aradhya");
        
        await sender.SendBytes(args.ClientSocket, HeaderConstants.ClientDetails, netPacket.ToArray(out int totalBytes), totalBytes);
    }
}
```

### Reading tha data 
```c#
public class OnServerConnected : BaseTcpClientConnected // <- Abstract class used for calling method
{
    public override async void OnServerRespond(ClientTcp sender, NetworkPacket sendPacket, //<- This packet is Read only
        uint header, int bodyLength, byte[] buffer)
    {
        var id = sendPacket.ReadInt();
        var testBool = sendPacket.ReadBool();
        var name = sendPacket.ReadString();

        Console.WriteLine($"ID {id}");
        Console.WriteLine($"TestBool {testBool}");
        Console.WriteLine($"NAME {name}");
        
        // If you want to send data to server
        // Use the Network Packet class that comes with the sender aka (ClientTcp) class
        // and Fill the Details
        sender.SenderPacket.WriteString("Prajwal Aradhya");
        sender.SenderPacket.WriteInt(21);
        sender.SenderPacket.WriteDouble(3.1413);
        
        // Then send to server
        await sender.SendBytes(HeaderConstants.TestMessage, sender.SenderPacket);
        // The Header Constants is nothing but contains the uint Numbers, Helpful for Readability
        // You can define your own Numbers for the Headers. 
        // Note: Header is the Identifier of every Packet data.
    }
}
```

```c#
// The HeaderConstants Struct
public struct HeaderConstants
{
    public const uint ClientDetails = 1;
    public const uint TestMessage = 2;
}
```