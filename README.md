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

## Code signing policy

Free code signing for the released `L2Updater.exe` is being arranged through [SignPath.io](https://signpath.io), with a certificate from the [SignPath Foundation](https://signpath.org). Until it is approved, releases are not signed and Windows SmartScreen may warn on the first start (More info, Run anyway).

- Only builds made by the GitHub Actions workflow in this repository (`.github/workflows/build.yml`) from the `main` branch are signed. Nothing built on a developer's computer is signed.
- Every build runs `dotnet test` first; a failing test stops the release.
- The signed file contains only the code of this repository and the NuGet/.NET runtime components it references, no closed-source parts.
- Team roles: the committer, the reviewer and the approver of releases is the repository owner, [bankaishow](https://github.com/bankaishow).

## Privacy policy

The updater does not collect, store or send personal data and has no telemetry. It only makes these network requests, all over HTTPS, to addresses taken from the signed manifest (or `updater.ini`):

- the manifest and file lists with their signatures;
- game files and the new updater version, from the mirrors listed in the manifest;
- the news and server status feeds shown in the window (plain data, optional).

Like any web request, these reveal the player's IP address to the site and the mirrors, and carry the header `User-Agent: L2Updater/<version>`. The folders chosen by the player and the last selected client are kept only in the local file `updater.ini`.

License: MIT.
