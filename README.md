# FileSync (Server)
#### Simple file sync implementation in .NET 

# Documentation
Documentation is available [here](https://lasonpeter.github.io/FileSyncServer/)
# How to use

## Installation

---

### Docker image
    docker pull lasonpeter/filesyncserver
This should pull the image from Docker Hub repository
### Platform specific binaries
Platform specific binaries are available [here](https://github.com/lasonpeter/FileSyncServer/releases)

# TODO

- [ ] Stream compression
- [ ] Client-server authentication
- [ ] Bidirectional file synchronization (currently its only client -> server)
- [ ] File conflict resolution
- [ ] Partial file synchronization


## Licensing
This project uses the [MIT](https://opensource.org/license/MIT) license

---

## WARNING
The Project is W.I.P, and as of now it's *usable** but not recommended,
as a result there will be no precise documentation until the first stable release
which is projected to happen around November 2024

---
Project uses its own protocol library which currently is in its infancy: [TransferLib](https://github.com/lasonpeter/TransferLib)
