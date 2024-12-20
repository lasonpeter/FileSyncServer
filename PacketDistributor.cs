using System.Net;
using System.Net.Sockets;
using FileSyncServer.Config;
using ProtoBuf;
using Serilog;
using TransferLib;

namespace FileSyncServer;

internal class PacketDistributor
{
    public event EventHandler<PacketEventArgs>? OnPing;
    public event EventHandler<PacketEventArgs>? OnData;
    /// <summary>
    /// Occurs when the FSInit packet is received
    /// </summary>
    public event EventHandler<PacketEventArgs>? OnFileSyncInit;
    /// <summary>
    /// Occurs when the FSData packet is received
    /// </summary>
    public event EventHandler<PacketEventArgs>? OnFileSyncData;
    /// <summary>
    /// Occurs when the FSUploadCheckHash packet is received 
    /// </summary>
    public event EventHandler<PacketEventArgs>? OnFileSyncUploadCheckHash;
    /// <summary>
    /// Occurs when the FSFinish packet is received 
    /// </summary>
    public event EventHandler<PacketEventArgs>? OnFileSyncFinish;
    /// <summary>
    /// Occurs when the FSHashCheck packet is received
    /// </summary>
    public event EventHandler<PacketEventArgs>? OnFileSyncHashCheck;

    private Socket _socket;

    public PacketDistributor(Socket socket)
    {
        _socket = socket;
    }

    /// <summary>
    /// Indefinitely waits for a packet from the client 
    /// </summary>
    public void AwaitPacket()
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
                    try
                    {
                        int recv = _socket.Receive(buffer, total, dataLeft, SocketFlags.None);
                        total += recv;
                        dataLeft -= recv;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Client abruptly disconnected ");
                        Log.Warning("Client abruptly disconnected ");
                        break;
                    }
                }

                packet.DecodePacket(buffer);
                if (packet.PacketType is PacketType.Error)
                {
                    Console.WriteLine("CONNECTION LOST !!!!!!!!!!!!!"); 
                    break;
                }

                //Console.WriteLine("PacketType:"+packet.PacketType);
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
                    case PacketType.FileSyncUploadCheckHash:
                        OnFileSyncCheckHashPacket(new PacketEventArgs(packet));
                        break;
                    case PacketType.FileSyncFinish:
                    {
                        OnFileSyncFinishPacket(new PacketEventArgs(packet));
                    }
                        break;
                    case PacketType.FileSyncHashCheck:
                    {
                        OnFileSyncHashCheckPacket(new PacketEventArgs(packet));
                    }
                        break;
                }
            }
        }).Start();
    }

    /// <summary>
    /// Performs the version handshake
    /// </summary>
    /// <exception cref="Exception">Throws exception if the version is incompatible</exception>
    public void VersionHandshake()
    {
        var packet = new Packet();
        var buffer = new byte[4_099];
        var size = 4099;
        var total = 0;
        var dataLeft = size;
        while (total < size)
        {
            try
            {
                int recv = _socket.Receive(buffer, total, dataLeft, SocketFlags.None);
                total += recv;
                dataLeft -= recv;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Client abruptly disconnected ");
                Log.Warning("Client abruptly disconnected ");
                break;
            }
        } 
        Console.WriteLine("RECEIVED HANDSHAKE");
        try
        {
            packet.DecodePacket(buffer);
            if (packet.PacketType is PacketType.VersionHandshake)
            {
                MemoryStream memoryStream = new MemoryStream(packet.Payload, 0, packet.MessageLength);
                VersionHandshake versionHandshake =
                    Serializer.Deserialize<VersionHandshake>(memoryStream);
                Console.WriteLine($"Client {versionHandshake.Version} | {Settings.Instance.Version}");
                if (versionHandshake.Version == Settings.Instance.Version)
                {
                    VersionHandshakeResponse versionHandshakeResponse = new VersionHandshakeResponse()
                    {
                        ApplicationVersionCompatibilityLevel = ApplicationVersionCompatibilityLevel.FullyCompatible,
                        Version = Settings.Instance.Version
                    };
                    using MemoryStream stream = new MemoryStream();
                    Serializer.Serialize(stream, versionHandshakeResponse);
                    _socket.Send(new Packet(stream.ToArray(), PacketType.VersionHandshakeResponse).ToBytes());
                }
                else
                {
                    VersionHandshakeResponse versionHandshakeResponse = new VersionHandshakeResponse()
                    {
                        ApplicationVersionCompatibilityLevel = ApplicationVersionCompatibilityLevel.Incompatible,
                        Version = Settings.Instance.Version
                    };
                    using MemoryStream stream = new MemoryStream();
                    Serializer.Serialize(stream, versionHandshake);
                    _socket.Send(new Packet(stream.ToArray(), PacketType.VersionHandshakeResponse).ToBytes());
                    throw (new Exception("Incompatible version"));
                }

                /*
                case ApplicationVersionCompatibilityLevel.FullyCompatible:
                    {
                        Console.WriteLine("Fully compatible");
                        Log.Information("Fully compatible !");
                    }
                    case ApplicationVersionCompatibilityLevel.PartiallyCompatible:
                    {
                        Console.WriteLine(
                            $"Partially compatible | client:{Settings.Instance.Version} server:{packet.Version}");
                        Log.Warning(
                            $"Partially compatible | client:{Settings.Instance.Version} server:{packet.Version}");
                    }
                    case ApplicationVersionCompatibilityLevel.Incompatible:
                    {
                        Console.WriteLine(
                            $"Incompatible version | client:{Settings.Instance.Version} server:{packet.Version}");
                        Log.Warning(
                            $"Incompatible version | client:{Settings.Instance.Version} server:{packet.Version}");
                        throw (new Exception(
                            $"Incompatible version | client:{Settings.Instance.Version} server:{packet.Version}"));
                    }
                }*/
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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
        var raiseEvent = OnFileSyncUploadCheckHash;

        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }

    protected virtual void OnFileSyncFinishPacket(PacketEventArgs e)
    {
        var raiseEvent = OnFileSyncFinish;
        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }
    protected virtual void OnFileSyncHashCheckPacket(PacketEventArgs e)
    {
        var raiseEvent = OnFileSyncHashCheck;
        if (raiseEvent != null) raiseEvent.Invoke(this, e);
    }
}