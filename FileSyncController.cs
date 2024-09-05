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

    /// <summary>
    /// Called upon receiving FSInit packet
    /// Initiates the file synchronization process 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    public void FileSyncInit(object? sender, PacketEventArgs eventArgs)
    {
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        FsInit fsInit = Serializer.Deserialize<FsInit>(memoryStream);
        Console.WriteLine($"Initiating sync: {fsInit.FileId}, {fsInit.FileSize}, {fsInit.FilePath} /{fsInit.FileName} , {fsInit.FuuId.ToString()}");
        fileLookup.Add(fsInit.FileId, new SFile(_socket, fsInit,_rocksDb));
        SFile? sFile;
        if (fileLookup.TryGetValue(fsInit.FileId, out sFile))
            sFile.FileSyncInit();
        //Console.WriteLine("INIT");
    }

    /// <summary>
    /// Called upon receiving FSData
    /// Sends the received data to appropriate SFile
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
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
        if (fileLookup.TryGetValue(fsData.FileId, out sFile)) {
            sFile.WriteData(fsData.FileData, fsData.Length);
        }
    }

    /// <summary>
    /// Called upon receiving FSUploadChechHash packet
    /// Tells specified SFile to check if hash is the same
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    public void FileSyncUploadCheckHash(object? sender, PacketEventArgs eventArgs)
    {
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        var fsData = Serializer.Deserialize<FSUploadCheckHash>(memoryStream);
        SFile? sFile;
        if (fileLookup.TryGetValue(fsData.FileId, out sFile)) 
            sFile.CheckHash(fsData);
    }

    /// <summary>
    /// Finalizes the file synchronization by disposing of all SFile related data
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
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

    
    /// <summary>
    /// Called upon receiving FSHashCheck
    /// Checks if the files associated with provided FUUIDs have the same hashes as the hashes provided with FUUIDs
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    public void FileSyncHashCheck(object? sender, PacketEventArgs eventArgs)
    {
        var memoryStream = new MemoryStream(eventArgs.Packet.Payload, 0, eventArgs.Packet.MessageLength);
        var fsHashCheck = Serializer.Deserialize<FSHashCheck>(memoryStream);
        List<Guid> changedFiles = new List<Guid>();
        foreach (var pair in fsHashCheck.HashCheckPairs)
        {
            var serverHash = BitConverter.ToUInt64(_rocksDb.Get(pair.FuuId.ToByteArray()));
            if (serverHash != pair.Hash)
            {
                changedFiles.Add(pair.FuuId);
            }
        }

        MemoryStream memoryStreamExport = new MemoryStream();
        Serializer.Serialize(memoryStreamExport,new FSHashCheckResponse()
        {
            Changed = changedFiles
        });
        _socket.SendAsync(new Packet(memoryStreamExport.ToArray(), PacketType.FileSyncHashCheckResponse, (int)memoryStreamExport.Length).ToBytes());
    }
}