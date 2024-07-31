using System.Net.Sockets;
using RocksDbSharp;
using Serilog;
using TransferLib;

namespace FileSyncServer.Startup;

public class ClientConnection 
{
    private Socket _socket;
    private RocksDb _rocksDb;
    public ClientConnection(Socket socket, RocksDb rocksDb)
    {
        _rocksDb = rocksDb;
        _socket = socket;
        Thread thread = new Thread(ConnectionThread);
        thread.Start(_socket);
    }

    private void ConnectionThread(object? o)
    {
        Socket client = (Socket)o!;
        Console.WriteLine("CONNECTED :D");
        
        var packetDistributor = new PacketDistributor(client);
//        packetDistributor.OnPing += (sender, eventArgs) => { Console.WriteLine("WE");}; 
        var fileSyncController = new FileSyncController(client,_rocksDb);
        packetDistributor.OnFileSyncInit += fileSyncController.FileSyncInit;
        packetDistributor.OnFileSyncData += fileSyncController.FileSyncData;
        packetDistributor.OnFileSyncCheckHash += fileSyncController.FileSyncCheckHash;
        packetDistributor.OnFileSyncFinish += fileSyncController.FileSyncFinish;
        try
        {
            packetDistributor.VersionHandshake();
            packetDistributor.AwaitPacket();
            Ping(client);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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