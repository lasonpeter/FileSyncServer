# File Sync (FS) protocol overview
FS uses TCP/IP sockets to communicate on *client* to *server* basis.
All of the synchronization relies on a server having a suficient amount of memory to save all synced files.

!!! warning
    READ THIS:
    For the love of god, in current state don't use this with files that are of great security: ID card images(don't do it in general), banking records, those neat Lockheed Martin classified documents, or other files that may impose any kind of damage if exposed to the public


!!! information
    This protocol will change IMMENSELY to support a structure with layers like:
    authentication layer, data transport layer, encryption and such
- On the fly stream compression
- Optimize RAM, CPU & IO usage
- Encryption
- API for the service
- Rewrite everything in Rust or C++ (because why not)


---
## General structure

!!! information
    Dute to UNIX/POSIX something-IX not allowing to change `birth time` there will be no support for maintaining the *right* file creation time anywhere else than the host system that first created the file. 
    P.S. It's possible that this would work while using Windows/(anti FOSS stack) as the code for doing that exists but I couldn't care less to test it
Protocol takes advantage of using *packets* to send chunks of data between user and server.
As of writing this, packet's max size is `4096 bytes`


## Example client -> server file sync
!!! information
    This example assumes everything goes *as planned*, whole file sync procedure will be called a *session*
- File change is made on *client*
- *Client* waits `x` amount of seconds
- *Client* sends `FileSyncInit` packet with respective data
- *Server* receives `FileSyncInit` packet, asseses if conditions are meet and sends ``FileSyncInitResponse` packet
- *Client* starts uploading the file chunks in `FileSyncData` packets until it sends all of the file
- *Client* sends `FileSyncCheckHash` with `XXHash3` hash of local file
- *Server* receives `FileSyncCheckHash` and checks `XXHash3` hash of uploaded file and sends `FileSyncCheckHashResponse`
- *Client* receives `FileSyncCheckHashResponse` and sends `FileSyncFinish` to imply that *client* received the hash response and can close the *session*


## Example server -> client file sync
!!! information
    This example assumes everything goes *as planned*, whole file sync procedure will be called a *session*