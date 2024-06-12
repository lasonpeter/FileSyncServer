using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace FileSyncServer.Config;

public class Settings
{
    public Settings(int port, string hostName)
    {
        Port = port;
        HostName = hostName;
    }

    public Settings()
    {
    }

    [JsonProperty("port")]
    public int Port { get; set; }
    [JsonProperty("host_name")] 
    public string HostName { get; set; } = "localhost";
}