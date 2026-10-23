# L2 Updater

Open-source game client updater and launcher for Windows. One executable serves several game clients; the list of clients, mirrors and links comes from a signed manifest published by the game site.

- Checks the game folder against a signed file list (SHA-256 per file) and downloads only changed files.
- Several mirrors per client, resume, atomic replacement, user settings are never overwritten.
- Manifest and file lists are signed (ECDSA P-256 / SHA-256). The public key is built into the updater, the private key never leaves the publisher's machine: a compromised website cannot push files to players.
- No administrator rights, no access outside the game folder and its own folder, no telemetry, no code injection into the game.
- Self-update, news and server status, RU/EN interface.

Format of the manifest and file lists: [docs/format.md](docs/format.md).

Build (Linux or Windows, .NET 8 SDK):

```
dotnet build
dotnet test
dotnet publish src/L2Updater.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o publish
```

License: MIT.
