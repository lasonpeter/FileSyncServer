using System.Net;
using System.Net.Sockets;
using TransferLib;

namespace FileSyncServer;

internal class PacketDistributor
{
    public event EventHandler<PacketEventArgs>? OnPing;
    public event EventHandler<PacketEventArgs>? OnData;
    public event EventHandler<PacketEventArgs>? OnFileSyncInit;
    public event EventHandler<PacketEventArgs>? OnFileSyncData;
    public event EventHandler<PacketEventArgs>? OnFileSyncCheckHash;
    public event EventHandler<PacketEventArgs>? OnFileSyncFinish;


    public void AwaitPacket(Socket socket, IPAddress ipAddress,int port, TcpListener tcpListener)
    {
        new Thread(o =>
        {
            var buffer = new byte[4_099];
            var packet = new Packet();
            var x = 0;
            while (true)
            {
                Array.Clear(buffer);
                // Receive message.
                var size = 4099;
                var total = 0;
                var dataLeft = size;
                while (total < size)
                {
                    var recv = socket.Receive(buffer, total, dataLeft, SocketFlags.None);
                    if (recv == 0)
                    {
                        break;
                    }
                    total += recv;
                    dataLeft -= recv;
                    Console.WriteLine(total);
                }

                packet.DecodePacket(buffer);
                if (packet.PacketType is PacketType.Error)
                {
                    try
                    {
                        socket.Disconnect(true);
                        try
                        {
                            socket=tcpListener.AcceptSocket();
                            Console.WriteLine("ACCEPTED");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                Console.WriteLine("PacketType:"+packet.PacketType);
                switch (packet.PacketType)
                {
                    case PacketType.Ping:
                        OnPingPacket(new PacketEventArgs(packet));
                        break;
                    case PacketType.Data:
                        OnDataPacket(new PacketEventArgs(packet));
                        break;
                    case PacketType.FileSyncInit:
                        OnFileSyncInitPacket(new PacketEventArgs(packet));
                        break;
                    case PacketType.FileSyncData:
                    {
                        {
                            OnFileSyncDataPacket(new PacketEventArgs(packet));
                            x++;
                        }
                        //Console.WriteLine("DATA");
                    }
                        break;
                    case PacketType.FileSyncCheckHash:
                        OnFileSyncCheckHashPacket(new PacketEventArgs(packet));
                        break;
                    case PacketType.FileSyncFinish:
                    {
                        OnFileSyncFinishPacket(new PacketEventArgs(packet));
                    }
                        break;
                }
            }
        }).Start();
    }

    protected virtual void OnPingPacket(PacketEventArgs e)
    {
        var raiseEvent = OnPing;

        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }

    protected virtual void OnDataPacket(PacketEventArgs e)
    {
        var raiseEvent = OnData;

        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }

    protected virtual void OnFileSyncInitPacket(PacketEventArgs e)
    {
        var raiseEvent = OnFileSyncInit;

        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }

    protected virtual void OnFileSyncDataPacket(PacketEventArgs e)
    {
        var raiseEvent = OnFileSyncData;

        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }

    protected virtual void OnFileSyncCheckHashPacket(PacketEventArgs e)
    {
        var raiseEvent = OnFileSyncCheckHash;

        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }

    protected virtual void OnFileSyncFinishPacket(PacketEventArgs e)
    {
        var raiseEvent = OnFileSyncFinish;

        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }
}