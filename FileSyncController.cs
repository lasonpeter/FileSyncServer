using System.Net.Sockets;
using ProtoBuf;
using Serilog;
using TransferLib;

namespace FileSyncServer;

public class FileSyncController
{
    private  Socket _socket;
    private  Dictionary<byte, SFile?> fileLookup = new();

    public FileSyncController(Socket socket)
    {
        _socket = socket;
    }

    public void FileSyncInit(object? sender, PacketEventArgs eventArgs)
    {
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        var fsInit = Serializer.Deserialize<FSInit>(memoryStream);
        Console.WriteLine($"Initiating sync: {fsInit.FileId}, {fsInit.FileSize}, {fsInit.FilePath} /{fsInit.FileName}");
        fileLookup.Add(fsInit.FileId, new SFile(_socket, fsInit));
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
        SFile sFile;
        fileLookup.TryGetValue(fsFinish.FileId, out sFile);
        Log.Information($"Synchronized {sFile?.GetFilePath()}");
        fileLookup.Remove(fsFinish.FileId);
        //Console.WriteLine("Synchronized");
    }
}