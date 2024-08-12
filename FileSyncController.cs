using System.Net.Sockets;
using ProtoBuf;
using RocksDbSharp;
using Serilog;
using TransferLib;

namespace FileSyncServer;

public class FileSyncController
{
    private  Socket _socket;
    private  Dictionary<byte, SFile?> fileLookup = new();
    private RocksDb _rocksDb;
    public FileSyncController(Socket socket,RocksDb rocksDb)
    {
        _rocksDb = rocksDb;
        _socket = socket;
    }

    public void FileSyncInit(object? sender, PacketEventArgs eventArgs)
    {
        
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        var fsInit = Serializer.Deserialize<FsInit>(memoryStream);
        Console.WriteLine($"THIS:{fsInit.FuuId.Length}");
        foreach (var f in fsInit.FuuId)
        {
            Console.Write(f.ToString());
        }
        Console.WriteLine($"Initiating sync: {fsInit.FileId}, {fsInit.FileSize}, {fsInit.FilePath} /{fsInit.FileName} , {new Guid(fsInit.FuuId).ToString()}");
        fileLookup.Add(fsInit.FileId, new SFile(_socket, fsInit,_rocksDb));
        SFile? sFile;
        if (fileLookup.TryGetValue(fsInit.FileId, out sFile))
            sFile.FileSyncInit();
        //Console.WriteLine("INIT");
    }

    public void FileSyncData(object? sender, PacketEventArgs eventArgs)
    {
        /*foreach (var VARIABLE in eventArgs.Packet.Payload)
        {
            Console.Write(VARIABLE);
        }*/
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        FSSyncData fsData;
        try
        {
            fsData = Serializer.Deserialize<FSSyncData>(memoryStream);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            Console.WriteLine(e);
            Console.WriteLine(eventArgs.Packet.MessageLength);
            throw;
        }

        SFile? sFile;
        if (fileLookup.TryGetValue(fsData.FileId, out sFile)) sFile.WriteData(fsData.FileData, fsData.Length);
    }

    public void FileSyncCheckHash(object? sender, PacketEventArgs eventArgs)
    {
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        var fsData = Serializer.Deserialize<FSCheckHash>(memoryStream);
        SFile? sFile;
        if (fileLookup.TryGetValue(fsData.FileId, out sFile)) 
            sFile.CheckHash(fsData);
    }

    public void FileSyncFinish(object? sender, PacketEventArgs eventArgs)
    {
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        var fsFinish = Serializer.Deserialize<FSFinish>(memoryStream);
        fileLookup.TryGetValue(fsFinish.FileId, out var sFile);
        if (sFile != null)
        {
            sFile.FinishSync();
            Log.Information($"Synchronized {sFile?.GetFilePath()}");
        }

        fileLookup.Remove(fsFinish.FileId);
        
        //Console.WriteLine("Synchronized");
    }
}