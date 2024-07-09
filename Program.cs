using System.Data.SQLite;
using System.Net;
using System.Net.Sockets;
using FileSyncServer.Config;
using FileSyncServer.Startup;
using Newtonsoft.Json;
using Serilog;
using Microsoft.Data.Sqlite;

namespace FileSyncServer;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            try
            {

                await using (var connection = new SqliteConnection($"Data Source={"db.sqlite"}"))
                {
                    connection.Open();
                    await using var command = new SqliteCommand("", connection);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            

            //INITIALIZING LOGGING
            Console.WriteLine("STARTING UP");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogFiles", "Log.txt"),
                    rollingInterval: RollingInterval.Month,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
                .CreateLogger();
            Console.WriteLine("LOADING CONFIG");
            //LOADING CONFIG
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
            Console.WriteLine("CONFIG LOADED");
            //Establishing listener
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
                    Console.WriteLine("Trying parsing");
                    Log.Information("Trying parsing");
                    ipAddress = IPAddress.Parse(settings.HostName);
                    Console.WriteLine("Success !");
                    Log.Information("Success !");
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    Log.Error("WRONG HOST IP");
                    throw;
                }
            }
            Console.WriteLine("CONNECTION EXPOSED");
            IPEndPoint ipEndPoint = new(ipAddress, settings.Port);
            var tcpListener = new TcpListener(ipEndPoint);
            tcpListener.Start();
            //Waiting for a new client
            while (true)
            {
                Socket client =  tcpListener.AcceptSocket();
                //INITIATE NEW CLIENT CONNECTION
                ClientConnection newClientConnection = new ClientConnection(client);
                Console.WriteLine("CLIENT CONNECTED");
            }

            return 0;
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
            return 1;
        }

        return 0;
    }
}