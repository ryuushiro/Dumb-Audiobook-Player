# 🎧 Premium Windows Audiobook Player

A modern, high-performance audiobook player built with **C#**, **.NET 8**, and **WPF**. This application provides a premium listening experience with deep chapter support, persistent playback memory, and a sleek, Spotify-inspired dark interface.

<img width="866" height="573" alt="image" src="https://github.com/user-attachments/assets/4c0ff5e4-118f-4287-8887-189368350b5e" />


## ✨ Key Features

- **📖 Chapter-Relative Progress:** The main progress bar and timers track the *current chapter*, not just the entire book, providing a true audiobook experience.
- **🎨 Premium Dark UI:** A custom-styled, clean interface with rounded corners, drop shadows, and modern typography (Segoe UI).
- **⏩ SMART Seeking:** Adjustable playback speeds (1.0x to 2.0x) and quick 15s skip forward/backward.
- **📍 Persistent Playback:** Automatically remembers and resumes from your exact last played position for every book.
- **😴 Sleep Timer:** Set timers for 15, 30, or 60 minutes, or use the "End of Chapter" mode to finish your current section before pausing.
- **🖱️ Hover Volume:** A YouTube-inspired volume control that expands only when you hover over the speaker icon.
- **🖼️ Metadata Extraction:** Automatically extracts cover art, title, author, and chapter markers from `.m4b` and common audio formats.

## 🛠️ Tech Stack

- **Framework:** .NET 8.0 (Windows WPF)
- **Audio Engine:** [LibVLCSharp](https://github.com/videolan/libvlcsharp) (VLC Backend)
- **Database:** SQLite (Microsoft.Data.Sqlite) for library persistence.
- **Metadata:** [TagLibSharp](https://github.com/mono/taglib-sharp) for high-performance audio tagging.

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows OS (WPF Requirement)

### Installation

1. **Clone the repository:**
   ```bash
   git clone https://github.com/ryuushiro/Dumb-Audiobook-Player.git
   cd AudiobookPlayer
   ```

2. **Restore Dependencies:**
   The project uses NuGet for all dependencies. Running a build will automatically fetch them.
   ```bash
   dotnet restore
   ```

3. **Run the Application:**
   ```bash
   cd AudiobookPlayer
   dotnet run
   ```

## 🏗️ Building for Production

To create a standalone executable for distribution:

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false
```

The output will be found in `bin/Release/net8.0-windows/win-x64/publish/`.

## 📂 Project Structure

- **`/Models`**: Data structures (e.g., `Book.cs`).
- **`/Services`**: Core logic layers (`AudioEngineService`, `DatabaseService`, `MetadataService`).
- **`MainWindow.xaml`**: Premium UI definitions and custom ControlTemplates.
- **`MainWindow.xaml.cs`**: UI logic and event wiring.

---
Built with ❤️ by dumbasses.
