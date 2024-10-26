FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 11000
COPY /bin/Release/net8.0/linux-x64 /app
ENTRYPOINT ["dotnet", "FileSyncServer.dll"]