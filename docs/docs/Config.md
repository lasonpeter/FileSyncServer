# FileSync configuration

## Client configuration

Example configuration file `config.json`
```json
{
  "port": 11000,
  "host_name": "localhost", 
  "synchronization_paths": [
    {
      "path": "/home/xenu/Documents/",
      "recursive": true,
      "synchronized": true  
    }
  ],
  "working_directory": ""
}
```

- `port` - port to which the client will connect with server
- `host_name` - string value of hostname to which a client connects to, supports both ip addresses and domains
- `synchronization_paths` - list of synchronized path objects
- `working_directory`- ***Not used as of now***
-
#### SynchronizedPath scheme:

- `path` - path of a synchronized file or folder
- `recursive` - a bool value indicating whether to synchronize the folders within the synchronized folder
- `synchronized` - a bool value indicating whether the file or folder is being synchronized by the *FileSync*

---
## Server configuration

Example configuration file `config.json`
```json
{
  "port": 11000,
  "host_name": "127.0.0.1",
  "working_directory": "/home/xenu/FileSync/"
}
```

- `port` - port on which the server will listen for incoming connections
- `hostname` - hostname on which the server will be broadcasting, supports both ip and domains
- `working_directory` - directory in which the *FileSync* will store the synced files