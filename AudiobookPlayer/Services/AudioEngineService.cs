using System;
using System.Collections.Generic;
using LibVLCSharp.Shared;

namespace AudiobookPlayer.Services
{
    public class ChapterInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public long StartTimeOffset { get; set; }

        public override string ToString() => Name;
    }

    /// <summary>
    /// A wrapper service around LibVLCSharp to handle all audio playback functionalities.
    /// This service provides an abstraction layer for the WPF application.
    /// </summary>
    public class AudioEngineService : IDisposable
    {
        private readonly LibVLC _libVLC;
        
        // Exposed so the UI component (VideoView) can attach to it, though strictly for audio, 
        // MediaPlayer handles the heavy lifting without the GUI.
        public MediaPlayer MediaPlayer { get; private set; }

        public event EventHandler<MediaPlayerTimeChangedEventArgs>? TimeChanged;
        public event EventHandler<EventArgs>? EndReached;
        public event EventHandler<MediaPlayerChapterChangedEventArgs>? ChapterChanged;

        public AudioEngineService()
        {
            // Initialize the LibVLC core backend. 
            // Note: This requires the VideoLAN.LibVLC.Windows package to work properly on Windows
            // if VLC is not installed system-wide.
            Core.Initialize();

            _libVLC = new LibVLC();
            MediaPlayer = new MediaPlayer(_libVLC);

            // Forward the events from LibVLC MediaPlayer to our own events
            MediaPlayer.TimeChanged += (sender, e) => TimeChanged?.Invoke(this, e);
            MediaPlayer.EndReached += (sender, e) => EndReached?.Invoke(this, EventArgs.Empty);
            MediaPlayer.ChapterChanged += (sender, e) => ChapterChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Loads the specified audio file into the player.
        /// </summary>
        public void LoadMedia(string filePath)
        {
            // Create media object using the local file path
            var media = new Media(_libVLC, new Uri(filePath));
            
            // Load it into the media player
            MediaPlayer.Media = media;
        }

        public void Play()
        {
            MediaPlayer.Play();
        }

        public void Pause()
        {
            MediaPlayer.Pause();
        }

        /// <summary>
        /// Sets the playback position.
        /// </summary>
        /// <param name="milliseconds">Target position in milliseconds.</param>
        public void SetTime(long milliseconds)
        {
            MediaPlayer.Time = milliseconds;
        }

        /// <summary>
        /// Gets the current playback position.
        /// </summary>
        /// <returns>Current position in milliseconds.</returns>
        public long GetTime()
        {
            return MediaPlayer.Time;
        }

        /// <summary>
        /// Adjusts the playback speed.
        /// </summary>
        /// <param name="rate">E.g., 1.0f for normal, 1.25f/1.5f for faster.</param>
        public void SetPlaybackRate(float rate)
        {
            MediaPlayer.SetRate(rate);
        }

        /// <summary>
        /// Skips playback forward by a set amount.
        /// </summary>
        /// <param name="ms">Amount of milliseconds to skip. Defaults to 15000 (15s).</param>
        public void SkipForward(long ms = 15000)
        {
            var newTime = MediaPlayer.Time + ms;
            var length = MediaPlayer.Length;
            
            // Prevent skipping past the end
            if (length > 0 && newTime > length)
            {
                newTime = length;
            }
            
            MediaPlayer.Time = newTime;
        }

        /// <summary>
        /// Skips playback backward by a set amount.
        /// </summary>
        /// <param name="ms">Amount of milliseconds to skip. Defaults to 15000 (15s).</param>
        public void SkipBackward(long ms = 15000)
        {
            var newTime = MediaPlayer.Time - ms;
            
            // Prevent skipping before the beginning
            if (newTime < 0)
            {
                newTime = 0;
            }
            
            MediaPlayer.Time = newTime;
        }

        /// <summary>
        /// Retrieves the list of chapters from the currently loaded media.
        /// </summary>
        public List<ChapterInfo> GetChapters()
        {
            var chapters = new List<ChapterInfo>();
            var descriptions = MediaPlayer.FullChapterDescriptions(0);
            
            if (descriptions != null)
            {
                for (int i = 0; i < descriptions.Length; i++)
                {
                    // 1. Pull the name out into a local variable so the compiler can track it safely
                    string? rawName = descriptions[i].Name;
                    
                    chapters.Add(new ChapterInfo
                    {
                        Index = i,
                        // 2. Now the compiler knows exactly what 'rawName' is, and the warning disappears
                        Name = string.IsNullOrWhiteSpace(rawName) ? $"Chapter {i + 1}" : rawName,
                        StartTimeOffset = descriptions[i].TimeOffset
                    });
                }
            }
            return chapters;
        }

        /// <summary>
        /// Sets the playback volume.
        /// </summary>
        /// <param name="volume">Volume from 0 to 100.</param>
        public void SetVolume(int volume)
        {
            if (MediaPlayer != null)
            {
                MediaPlayer.Volume = volume;
            }
        }

        /// <summary>
        /// Jumps to a specific chapter index.
        /// </summary>
        public void SetChapter(int index)
        {
            if (index >= 0)
            {
                MediaPlayer.Chapter = index;
            }
        }

        public void Dispose()
        {
            // Unsubscribe tightly coupled events to prevent memory leaks
            if (MediaPlayer != null)
            {
                MediaPlayer.TimeChanged -= (sender, e) => TimeChanged?.Invoke(this, e);
                MediaPlayer.EndReached -= (sender, e) => EndReached?.Invoke(this, EventArgs.Empty);
                MediaPlayer.ChapterChanged -= (sender, e) => ChapterChanged?.Invoke(this, e);
                MediaPlayer.Dispose();
            }
            
            _libVLC?.Dispose();
        }
    }
}
