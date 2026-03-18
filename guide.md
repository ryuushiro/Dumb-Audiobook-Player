# Project Specification: Windows Audiobook Player (C# / WPF)

## 1. Project Overview
Build a dedicated Windows audiobook player using C# and .NET 8. The application must prioritize persistent playback memory, precise micro-seeking, playback speed control without pitch distortion, and audiobook-specific metadata extraction.

## 2. Tech Stack Requirements
* **Language/Framework:** C# / .NET 8 (WPF Application)
* **Audio Engine:** `LibVLCSharp` and `LibVLCSharp.WPF` (Crucial for handling .m4b, .mp3, exact micro-seeking, and pitch-preserved speed changes).
* **Database:** `Microsoft.Data.Sqlite` (Local database for storing playback positions).
* **Metadata:** `TagLibSharp` (For parsing title, author, and cover art from audio files).

## 3. Recommended Architecture
Please organize the code using a clean separation of concerns within the WPF project:
/AudiobookPlayer
│── App.xaml & App.xaml.cs          # Application entry point
│── MainWindow.xaml & MainWindow.xaml.cs # Main UI and code-behind
│── /Services
│   ├── AudioEngineService.cs       # LibVLCSharp wrapper (play, pause, seek, speed)
│   ├── DatabaseService.cs          # SQLite logic for timestamps
│   └── MetadataService.cs          # TagLibSharp logic for tags/cover art
└── /Models
    └── Book.cs                     # Data model for the audiobook

## 4. Step-by-Step Implementation Guide

### Phase 1: Project Setup and Database (`DatabaseService.cs`)
1.  Define the `Book.cs` model with properties: Id, FilePath, Title, Author, LastPlayedPosition (long ms), TotalDuration (long ms).
2.  Create an SQLite database named `library.db` in the application's local app data folder.
3.  Implement `DatabaseService.cs` with methods to:
    * Initialize the database and create a `Books` table if it doesn't exist.
    * `AddOrUpdateBook(Book book)`
    * `GetSavedPosition(string filePath)` -> returns long (milliseconds).

### Phase 2: Metadata Extraction (`MetadataService.cs`)
1.  Implement a method `ExtractMetadata(string filePath)` using `TagLibSharp`.
2.  Extract the Title, Author, and the Cover Art (as a `byte[]` or `BitmapImage` for WPF).
3.  Return a populated `Book` object.

### Phase 3: The Audio Engine (`AudioEngineService.cs`)
1.  Initialize `LibVLC` and `MediaPlayer`.
2.  Implement methods:
    * `LoadMedia(string filePath)`
    * `Play()`, `Pause()`
    * `SetTime(long milliseconds)`
    * `GetTime()` -> returns current position in ms.
    * `SetPlaybackRate(float rate)` -> e.g., 1.0f, 1.25f, 1.5f.
    * `SkipForward(long ms)` and `SkipBackward(long ms)` for 15-second jumps.
3.  Expose events for `TimeChanged` and `EndReached` so the UI can update.

### Phase 4: The WPF Interface (`MainWindow.xaml`)
1.  Build the UI using XAML.
2.  **Layout Elements Required:**
    * An `Image` control for Cover Art.
    * `TextBlock` controls for Title and Author.
    * A `Slider` for the playback progress bar.
    * `TextBlock` controls for Current Time / Total Time.
    * `Button` controls: Play/Pause, Skip Back 15s, Skip Forward 15s, Load Book.
    * A `ComboBox` for Playback Speed.

### Phase 5: Wiring and Integration (`MainWindow.xaml.cs`)
1.  Instantiate the three services in the MainWindow.
2.  **Critical Logic Flow:**
    * "Load Book" opens an `OpenFileDialog`.
    * Call `MetadataService` to get book details and update the UI.
    * Call `DatabaseService` to check for a saved `LastPlayedPosition`.
    * Call `AudioEngineService` to load the media and `SetTime()` to the saved position.
    * Subscribe to the audio engine's `TimeChanged` event to update the UI Slider and occasionally write the current position back to the `DatabaseService`.
3.  Override `OnClosing` in MainWindow to ensure the final timestamp is saved before exiting.