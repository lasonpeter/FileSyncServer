using System.Net.Sockets;
using ProtoBuf;
using Serilog;
using TransferLib;

namespace FileSyncServer;

public class SFile
{
    private FileStream _fileStream;
    private readonly FSInit _fsInit;

    private readonly Socket _socket;

    public SFile(Socket socket, FSInit fsInit)
    {
        _socket = socket;
        _fsInit = fsInit;
    }

    public void FileSyncInit()
    {
        #if DEBUG
            Directory.CreateDirectory("/home/xenu/FileSyncStorage" + _fsInit.FilePath);
            _fileStream = new FileStream("/home/xenu/FileSyncStorage" + _fsInit.FilePath + "/" + _fsInit.FileName,
                new FileStreamOptions
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.Write,
                    PreallocationSize = _fsInit.FileSize
                });
        #else
         Directory.CreateDirectory("/home/server/FileSyncStorage" + _fsInit.FilePath);
            _fileStream = new FileStream("/home/server/htopFileSyncStorage" + _fsInit.FilePath + "/" + _fsInit.FileName,
                new FileStreamOptions
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.Write,
                    PreallocationSize = _fsInit.FileSize
                });
        #endif
        
        var fsInitResponse = new FSInitResponse
        {
            FileId = _fsInit.FileId,
            IsAccepted = true
        };
        var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, fsInitResponse);
        _socket.Send(new Packet(memoryStream.ToArray(), PacketType.FileSyncInitResponse, (int)memoryStream.Length)
            .ToBytes());
    }

    public void WriteData(byte[] data, int length)
    {
        try
        {
            //Console.WriteLine($"Written: {length}");
            //Console.WriteLine(data[0] + data[1]);
            _fileStream.Write(data, 0, length);
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            Console.WriteLine(e);
            throw;
        }
    }

    public void CheckHash(byte fileId)
    {
        //Console.WriteLine("CHECK");
        _fileStream.Flush();
        _fileStream.Close();
        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, new FSCheckHashResponse
        {
            FileId = fileId,
            IsCorrect = true
        });
        _socket.Send(new Packet(memoryStream.ToArray(), PacketType.FileSyncCheckHashResponse, (int)memoryStream.Length)
            .ToBytes());
    }
}