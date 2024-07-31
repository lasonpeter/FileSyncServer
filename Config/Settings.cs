using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace FileSyncServer.Config;

public class Settings
{
    private static Settings? _instance;

    public static Settings Instance
    {
        get
        {
            if (_instance is null)
            {
                throw new Exception("Settings not loaded");
            }
            return _instance;
        }
    }
    
    
    public Settings(int port, string hostName)
    {
        Port = port;
        HostName = hostName;
        _instance = this;
    }
    
    [JsonProperty("port")] public int Port { get; set; } = 11000;
    [JsonProperty("host_name")] public string HostName { get; set; } = "localhost";
    [JsonProperty("working_directory")] public string WorkingDirectory;
    public readonly ushort Version = 1;

}