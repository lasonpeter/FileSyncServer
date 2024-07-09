using System.Net.Sockets;
using Serilog;
using TransferLib;

namespace FileSyncServer.Startup;

public class ClientConnection 
{
    private Socket _socket;

    public ClientConnection(Socket socket)
    {
        _socket = socket;
        Thread thread = new Thread(ConnectionThread);
        thread.Start(_socket);
    }

    private void ConnectionThread(object? o)
    {
        Socket client = (Socket)o!;
        Console.WriteLine("CONNECTED :D");
        
        var packetDistributor = new PacketDistributor();
//        packetDistributor.OnPing += (sender, eventArgs) => { Console.WriteLine("WE");}; 
        var fileSyncController = new FileSyncController(client);
        packetDistributor.OnFileSyncInit += fileSyncController.FileSyncInit;
        packetDistributor.OnFileSyncData += fileSyncController.FileSyncData;
        packetDistributor.OnFileSyncCheckHash += fileSyncController.FileSyncCheckHash;
        packetDistributor.OnFileSyncFinish += fileSyncController.FileSyncFinish;
        packetDistributor.AwaitPacket(client);
        Ping(client);
        Console.WriteLine("DEAD");
    }

    private void Ping(Socket client)
    {
        var pingPacket = new Packet(PacketType.Ping).ToBytes();
        var socketPing = new Thread(o =>
        {
            while (true)
            {
                //Console.WriteLine("SENT PING");  
                try
                {
                    lock(_socket){
                        client.Send(pingPacket);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client abruptly disconnected ");
                    Log.Warning("Client abruptly disconnected ");
                    break;
                }
                Thread.Sleep(5000);
            }
        });
        socketPing.Start();
        socketPing.Join();
        _socket.Dispose();
        Log.Information("Socket disposed");
    }
}