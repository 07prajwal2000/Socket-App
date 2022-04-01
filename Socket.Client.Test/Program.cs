using Socket.Client.Test.Demo;

var testClient = new TestClient();

await testClient.Start();

// uint uin = 2000;
//
// var bytes =new byte[10];
// var source = BitConverter.GetBytes(uin);
// Array.Copy(source, bytes, source.Length);
// var num = BitConverter.ToInt32(bytes);
// Console.WriteLine();