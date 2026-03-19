# DUMB AUDIOBOOK PLAYER
A dedicated audiobook media player for Windows. This app might look dumb, disgustingly simple, and amateurish, but it works!™<br><br>
<img width="966" height="673" alt="image" src="https://github.com/user-attachments/assets/79185db3-326d-4eec-aa79-03c6a1a835fc" /><br>
<img width="586" height="493" alt="image" src="https://github.com/user-attachments/assets/4e660e80-b0b3-41c1-891d-5c0514c702fc" />


## Why this exists?
Because I can. But also, because most Windows media players treat audiobooks like long songs. I knew there are bunches of dedicated audiobook media player out there, but I'm just too lazy to search for one.
## Features
- Chapter-Aware Progress: The progress bar shows you where you are in the current chapter, not just a 40-hour file.
- Persistent Memory: Close the app anytime. It saves your position to a local SQLite database and resumes instantly when you return.
- Smart Playback: Adjust speed from 1.0x to 2.0x with pitch preservation.
- Clean Interface: Dark mode by default (Spotify/Netflix inspired) or a light Apple-style theme.
- Sleep Timer: Turn off automatically after 15, 30, or 60 minutes, or at the end of the current chapter.
- Metadata Support: Pulls covers, authors, and chapter markers from .m4b, .mp3, and other common formats.<br>

TL;DR: Basically the bare minimum features for this kind of app.
## Tech
- Engine: LibVLCSharp (VLC)
- Metadata: TagLibSharp
- Database: SQLite
- UI: WPF / XAML

# SETUP
## Requirements
- .NET 8.0 SDK
- Windows
## Build and Run
1. Clone the repo:
```bash
git clone https://github.com/ryuushiro/Dumb-Audiobook-Player.git
```
2. Run it:
```bash
dotnet run --project AudiobookPlayer
```
## Create an EXE
To build a single, standalone executable:<br>
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false
```
Find the output in `bin/Release/net8.0-windows/win-x64/publish/`.
