using System.Net.Sockets;
using System.Text;
using FileSyncServer.Config;
using ProtoBuf;
using RocksDbSharp;
using Serilog;
using TransferLib;
using XXHash3NET;

namespace FileSyncServer;

public class SFile
{
    private FileStream _fileStream;
    private readonly FsInit _fsInit;
    private RocksDb _rocksDb;
    private readonly Socket _socket;

    public string GetFilePath()
    {
        return _fsInit.FilePath + "/" + _fsInit.FileName;
    }
    public SFile(Socket socket, FsInit fsInit, RocksDb rocksDb)
    {
        _rocksDb = rocksDb;
        _socket = socket;
        _fsInit = fsInit;
    }

    public void FileSyncInit()
    { 
        //TODO: Settings.Instance.WorkingDirectory + _fsInit.FilePath may create error if FilePath is going to be relative in the future !

        Console.WriteLine();
        Console.WriteLine($"WORKING ON FUUID: {new Guid(_fsInit.FuuId).ToString()}");
        Directory.CreateDirectory(Settings.Instance.WorkingDirectory + _fsInit.FilePath);
        _fileStream = new FileStream($"{Settings.Instance.WorkingDirectory}{_fsInit.FilePath}/{_fsInit.FileName}",
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.ReadWrite,
                PreallocationSize = _fsInit.FileSize
            });
        try
        {
            byte[] arr = new byte[_fsInit.FuuId.Length + 1];
            arr[0] = 0;
            if (!_rocksDb.HasKey(_fsInit.FuuId))
            {
                _fsInit.FuuId.CopyTo(arr, 1);
                Console.WriteLine($"CREATING NEW FILE WITH FUUID:{new Guid(_fsInit.FuuId).ToString()}");
                _rocksDb.Put(_fsInit.FuuId, new Byte []{0});
            }
            _rocksDb.Put(arr, Encoding.UTF8.GetBytes($"{_fsInit.FilePath}/{_fsInit.FileName}"));
            var serverHash =_rocksDb.Get(_fsInit.FuuId);
            Console.WriteLine($"Hash of file with FUUID: {new Guid(_fsInit.FuuId).ToString()} is: {BitConverter.ToUInt64(serverHash)}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        Console.WriteLine($"{Settings.Instance.WorkingDirectory}{_fsInit.FilePath}/{_fsInit.FileName}");
        _rocksDb.Put(new Guid().ToByteArray(), Encoding.UTF8.GetBytes(_fsInit.FilePath+"/"+_fsInit.FileName));
        var fsInitResponse = new FSInitResponse
        {
            FileId = _fsInit.FileId,
            IsAccepted = true
        };
        var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, fsInitResponse);
        Console.WriteLine("SENT");
        lock(_socket){
            _socket.Send(new Packet(memoryStream.ToArray(), PacketType.FileSyncInitResponse, (int)memoryStream.Length)
                .ToBytes());
        }
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

    public void CheckHash(FSCheckHash _fsFile)
    {
        Console.WriteLine("CHECK");
        _fileStream.Flush();
        ulong hash64;
        using var memoryStream = new MemoryStream();
        {
            hash64 = XXHash3.Hash64(_fileStream);
            Console.WriteLine(hash64);
            _fileStream.Close();
        }
        if (_fsFile.Hash == hash64)
        {
            Serializer.Serialize(memoryStream, new FSCheckHashResponse
            {
                FileId = _fsFile.FileId,
                IsCorrect = true
            });
        }
        else
        {
            Serializer.Serialize(memoryStream, new FSCheckHashResponse
            {
                FileId = _fsFile.FileId,
                IsCorrect = false
            });
        }
        lock(_socket){
            _socket.Send(new Packet(memoryStream.ToArray(), PacketType.FileSyncCheckHashResponse,
                    (int)memoryStream.Length)
                .ToBytes());
        }
    }

    public void FinishSync()
    {
        FileInfo fileInfo = new FileInfo($"{Settings.Instance.WorkingDirectory}{_fsInit.FilePath}/{_fsInit.FileName}");
        fileInfo.LastAccessTime = _fsInit.LastAccessTime;
        fileInfo.LastWriteTime = _fsInit.LastWriteTime;
        fileInfo.CreationTime = _fsInit.CreationTime;
    }
}