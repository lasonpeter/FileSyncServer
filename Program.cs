using System.Net;
using System.Net.Sockets;
using System.Text;
using TransferLib;

namespace FileSyncServer;

class Program
{
    static async Task Main(string[] args)
    {
        IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync("localhost");
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        var hostName = Dns.GetHostName();
        IPHostEntry localhost = await Dns.GetHostEntryAsync(hostName);
        // This is the IP address of the local machine
        IPAddress localIpAddress = localhost.AddressList[0];
        IPEndPoint ipEndPoint = new(ipAddress, 11_000);
        using Socket client = new(
            ipEndPoint.AddressFamily, 
            SocketType.Stream, 
            ProtocolType.Tcp);
        int x = 0;
        await client.ConnectAsync(ipEndPoint);
        byte[] pingPacket = new Packet(1).ToBytes();
        var socketPing = new Thread( (o =>
        {
            while(true){
                Console.WriteLine("PING");
                client.SendAsync(pingPacket);
                Thread.Sleep(5000);
            }
        }));
        socketPing.Start();
        Thread sendThread = new Thread(o =>
        {
            byte[] we = "Hello"u8.ToArray();
            Packet packet = new Packet();
            while (true)
            {
                // slow down
                packet.Compose(we, 0);
                //_ = await client.SendAsync(packet.ToBytes(), SocketFlags.None);
                Console.WriteLine(
                    $"Socket client sent message: \"{Encoding.UTF8.GetString(packet.Payload, 0, packet.MessageLength)}\"");
                Thread.Sleep(500);
                
            }
        });
        sendThread.Start();
        socketPing.Join();

    }
}
