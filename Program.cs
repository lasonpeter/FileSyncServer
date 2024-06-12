using System.Net;
using System.Net.Sockets;
using FileSyncServer.Config;
using Newtonsoft.Json;
using Serilog;
using TransferLib;

namespace FileSyncServer;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine("STARTING UP");
        JsonSerializer jsonSerializer = new JsonSerializer();
        Settings settings;
        try
        {
            settings = jsonSerializer.Deserialize<Settings>(new JsonTextReader(File.OpenText("config.json")));
            if (settings is null)
            {
                Log.Error("Couldn't load config");
                return -1;
            }
        }
        catch (Exception e)
        {
            Log.Error("Couldn't load config");
            Console.WriteLine(e);
            throw;
        }
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogFiles", "Log.txt"),
                rollingInterval: RollingInterval.Month,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
            .CreateLogger();

        IPHostEntry ipHostInfo;
        IPAddress ipAddress;
        try
        {
            ipHostInfo = await Dns.GetHostEntryAsync(settings.HostName);
            ipAddress = ipHostInfo.AddressList[0]; 

        }
        catch (Exception e)
        {
            try
            {
                ipAddress =IPAddress.Parse(settings.HostName);
                Console.WriteLine("Trying parsing");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log.Error("WRONG HOST IP");
                throw;
            }
        }
        // This is the IP address of the local machine
        IPEndPoint ipEndPoint = new(ipAddress, settings.Port);
        var tcpListener = new TcpListener(ipEndPoint);
        tcpListener.Start();
        Socket client = tcpListener.AcceptSocket();
        //INITIATE NEW CLIENT CONNECTION
        Console.WriteLine("CONNECTED :D");
        var x = 0;
        var pingPacket = new Packet(PacketType.Ping).ToBytes();
        var socketPing = new Thread(o =>
        {
            while (true)
            {
                //Console.WriteLine("SENT PING");  
                try
                {
                    client.SendAsync(pingPacket);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                Thread.Sleep(5000);
            }
        });
        socketPing.Start();
        var packetDistributor = new PacketDistributor();
//        packetDistributor.OnPing += (sender, eventArgs) => { Console.WriteLine("WE");}; 
        var fileSyncController = new FileSyncController(client);
        packetDistributor.OnFileSyncInit += fileSyncController.FileSyncInit;
        packetDistributor.OnFileSyncData += fileSyncController.FileSyncData;
        packetDistributor.OnFileSyncCheckHash += fileSyncController.FileSyncCheckHash;
        packetDistributor.OnFileSyncFinish += fileSyncController.FileSyncFinish;
        packetDistributor.AwaitPacket(client,ipAddress,settings.Port,tcpListener);
        socketPing.Join();
        return 0;
    }
}