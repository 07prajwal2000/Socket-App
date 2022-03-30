using Socket.Server;

Console.WriteLine("Server Started");

var server = new Server();
await server.Start();

Console.ReadLine();

server.Stop();

Console.WriteLine("Server Closed");