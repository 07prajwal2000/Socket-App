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

// you can use static Events to register 
ClientTcp.OnMessageReceived += OnMessageReceived;
// or use the modular one
client.Register<GetIdFromServer>(HeaderConstants.ClientDetails);

await client.Start(); // start connecting to server

Console.ReadLine(); // use this for avoid closing the console window or exit.

client.Stop(); // used to Disconnect from server.
```