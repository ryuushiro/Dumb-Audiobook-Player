using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using AudiobookPlayer.Services;
using AudiobookPlayer.Models;
using LibVLCSharp.Shared;

namespace AudiobookPlayer
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _dbService;
        private readonly MetadataService _metadataService;
        private readonly AudioEngineService _audioEngine;
        
        private AppConfig _appConfig;

        private Book? _currentBook;
        private bool _isPlaying = false;
        private bool _isDraggingSlider = false;

        // Playlist Mode State
        private List<string> _playlist = new List<string>();
        private int _playlistIndex = 0;
        private bool _isPlaylistMode = false;
        
        // State for Chapter-relative Progress
        private ChapterInfo? _currentChapter;
        private List<ChapterInfo> _chapters = new List<ChapterInfo>();
        
        // Sleep Timer Features
        private DispatcherTimer _sleepTimer;
        private int _sleepMinutesRemaining;
        private bool _stopAtNextChapter = false;

        public MainWindow()
        {
            InitializeComponent();

            _dbService = new DatabaseService();
            _metadataService = new MetadataService();
            _audioEngine = new AudioEngineService();
            _appConfig = AppConfig.Load();
            
            _sleepTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _sleepTimer.Tick += SleepTimer_Tick;

            _audioEngine.TimeChanged += AudioEngine_TimeChanged;
            _audioEngine.EndReached += AudioEngine_EndReached;
            _audioEngine.ChapterChanged += AudioEngine_ChapterChanged;

            UpdateSkipButtonLabels();
        }

        private void UpdateSkipButtonLabels()
        {
            SkipBackBtn.Content = $"⏪ {_appConfig.SkipDuration}s";
            SkipForwardBtn.Content = $"{_appConfig.SkipDuration}s ⏩";
        }

        private void LoadBookBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Audiobook Files|*.mp3;*.m4b;*.m4a;*.wav;*.ogg|All Files|*.*",
                Title = "Select an Audiobook"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _isPlaylistMode = false;
                LoadAudiobook(openFileDialog.FileName);
            }
        }

        private void LibraryBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var libraryWindow = new LibraryWindow();
                libraryWindow.Owner = this;
                if (libraryWindow.ShowDialog() == true && libraryWindow.SelectedPlaylist.Count > 0)
                {
                    LoadPlaylist(libraryWindow.SelectedPlaylist, 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open library: {ex.Message}", "Error");
            }
        }

        private void LoadPlaylist(List<string> playlist, int startIndex)
        {
            _playlist = playlist;
            _playlistIndex = startIndex;
            _isPlaylistMode = playlist.Count > 1;
            LoadAudiobook(_playlist[_playlistIndex]);
        }

        private async void LoadAudiobook(string filePath)
        {
            try
            {
                if (_currentBook != null && !_isPlaylistMode) // Inherit position over playlist transition
                {
                    _currentBook.LastPlayedPosition = _audioEngine.GetTime();
                    _dbService.AddOrUpdateBook(_currentBook);
                }

                _currentBook = _metadataService.ExtractMetadata(filePath);
                
                TitleText.Text = _currentBook.Title;
                AuthorText.Text = _currentBook.Author;
                CoverImage.Source = _currentBook.CoverImage;

                long savedPosition = _dbService.GetSavedPosition(filePath);
                _currentBook.LastPlayedPosition = savedPosition;

                _audioEngine.LoadMedia(filePath);
                _audioEngine.Play();
                _isPlaying = true;
                UpdatePlayPauseButton();

                await System.Threading.Tasks.Task.Delay(300);

                // Fetch chapters after Media is parsed
                if (!_isPlaylistMode)
                {
                    _chapters = _audioEngine.GetChapters();
                    ChapterComboBox.ItemsSource = _chapters;
                    
                    if (_chapters.Count > 0)
                    {
                        _currentChapter = _chapters.FirstOrDefault(c => c.Index == _audioEngine.MediaPlayer.Chapter) ?? _chapters[0];
                        CurrentChapterText.Text = _currentChapter.Name;
                        UpdateChapterProgressBounds();
                    }
                    else
                    {
                        CurrentChapterText.Text = "No Chapters Available";
                        _currentChapter = null;
                        ProgressSlider.Maximum = Math.Max(1, _currentBook.TotalDuration);
                        TotalTimeText.Text = FormatTimeMs(_currentBook.TotalDuration);
                    }
                }
                else
                {
                    // MULTI-FILE PLAYLIST MODE
                    _chapters = new List<ChapterInfo>();
                    for (int i = 0; i < _playlist.Count; i++)
                    {
                        _chapters.Add(new ChapterInfo { Index = i, Name = System.IO.Path.GetFileNameWithoutExtension(_playlist[i]), StartTimeOffset = 0 });
                    }
                    ChapterComboBox.ItemsSource = _chapters;
                    
                    _currentChapter = _chapters[_playlistIndex];
                    CurrentChapterText.Text = _currentChapter.Name;
                    ChapterComboBox.SelectedIndex = _playlistIndex;
                    
                    ProgressSlider.Maximum = Math.Max(1, _currentBook.TotalDuration);
                    TotalTimeText.Text = FormatTimeMs(_currentBook.TotalDuration);
                }

                if (savedPosition > 0)
                {
                    _audioEngine.SetTime(savedPosition);
                }

                ApplyCurrentPlaybackSpeed();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load audiobook: {ex.Message}", "Error");
            }
        }

        private void UpdateChapterProgressBounds()
        {
            if (_currentChapter == null || _currentBook == null) return;
            long chapterDuration = GetChapterDuration(_currentChapter);
            ProgressSlider.Maximum = Math.Max(1, chapterDuration);
        }

        private long GetChapterDuration(ChapterInfo chapter)
        {
            if (_chapters.Count == 0 || _currentBook == null) return 0;
            
            int nextIndex = _chapters.IndexOf(chapter) + 1;
            if (nextIndex < _chapters.Count)
            {
                return _chapters[nextIndex].StartTimeOffset - chapter.StartTimeOffset;
            }
            return _currentBook.TotalDuration - chapter.StartTimeOffset;
        }

        private void PlayPauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBook == null) return;

            if (_isPlaying)
            {
                _audioEngine.Pause();
                _isPlaying = false;
            }
            else
            {
                _audioEngine.Play();
                _isPlaying = true;
            }
            UpdatePlayPauseButton();
        }

        private void UpdatePlayPauseButton()
        {
            PlayPauseBtn.Content = _isPlaying ? "⏸ Pause" : "▶ Play";
        }

        private void SkipBackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBook != null) _audioEngine.SkipBackward(_appConfig.SkipDuration * 1000);
        }

        private void SkipForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentBook != null) _audioEngine.SkipForward(_appConfig.SkipDuration * 1000);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Owner = this;
                if (settingsWindow.ShowDialog() == true)
                {
                    // Reload config after save
                    _appConfig = AppConfig.Load();
                    UpdateSkipButtonLabels();
                }
            }
            catch (Exception ex)
            {
                // This will pop up an error box instead of crashing the app!
                System.Windows.MessageBox.Show($"Crash Reason: {ex.Message}\n\n{ex.InnerException?.Message}", "App Crashed!");
            }
        }

        private void ApplyCurrentPlaybackSpeed()
        {
            if (_audioEngine == null || SpeedComboBox == null || SpeedComboBox.SelectedItem == null) return;
            
            var selectedItem = (ComboBoxItem)SpeedComboBox.SelectedItem;
            var content = selectedItem.Content?.ToString();
            
            float rate = 1.0f;
            if (content == "1.25x") rate = 1.25f;
            else if (content == "1.5x") rate = 1.5f;
            else if (content == "2.0x") rate = 2.0f;

            _audioEngine.SetPlaybackRate(rate);
        }

        private void SpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyCurrentPlaybackSpeed();
        }

        private void ChapterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChapterComboBox.SelectedItem is ChapterInfo selectedChapter)
            {
                if (_isPlaylistMode)
                {
                    if (_playlistIndex != selectedChapter.Index)
                    {
                        LoadPlaylist(_playlist, selectedChapter.Index);
                    }
                }
                else
                {
                    if (_audioEngine.MediaPlayer.Chapter != selectedChapter.Index)
                    {
                        _audioEngine.SetChapter(selectedChapter.Index);
                    }
                }
            }
        }

        private void PrevChapterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChapterComboBox.SelectedIndex > 0)
            {
                ChapterComboBox.SelectedIndex--;
            }
        }

        private void NextChapterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChapterComboBox.SelectedIndex < ChapterComboBox.Items.Count - 1)
            {
                ChapterComboBox.SelectedIndex++;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_audioEngine != null)
            {
                _audioEngine.SetVolume((int)e.NewValue);
            }

            if (VolumeIcon != null)
            {
                if (e.NewValue == 0)
                    VolumeIcon.Text = "🔇";
                else if (e.NewValue <= 33)
                    VolumeIcon.Text = "🔈";
                else if (e.NewValue <= 66)
                    VolumeIcon.Text = "🔉";
                else
                    VolumeIcon.Text = "🔊";
            }
        }

        private void SleepTimerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SleepTimerComboBox == null || SleepTimerComboBox.SelectedItem == null || _sleepTimer == null) return;
            
            var item = (ComboBoxItem)SleepTimerComboBox.SelectedItem;
            string selection = item.Content?.ToString() ?? "";

            _sleepTimer.Stop();
            _stopAtNextChapter = false;
            _sleepMinutesRemaining = 0;

            if (selection == "15 Minutes")
            {
                _sleepMinutesRemaining = 15;
                _sleepTimer.Start();
            }
            else if (selection == "30 Minutes")
            {
                _sleepMinutesRemaining = 30;
                _sleepTimer.Start();
            }
            else if (selection == "60 Minutes")
            {
                _sleepMinutesRemaining = 60;
                _sleepTimer.Start();
            }
            else if (selection == "End of Chapter")
            {
                _stopAtNextChapter = true;
            }
        }

        private void SleepTimer_Tick(object? sender, EventArgs e)
        {
            _sleepMinutesRemaining--;
            if (_sleepMinutesRemaining <= 0)
            {
                _sleepTimer.Stop();
                _audioEngine.Pause();
                _isPlaying = false;
                UpdatePlayPauseButton();
                SleepTimerComboBox.SelectedIndex = 0; // Reset to Off
            }
        }

        private void AudioEngine_ChapterChanged(object? sender, MediaPlayerChapterChangedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var active = _chapters.Find(c => c.Index == e.Chapter);
                if (active != null)
                {
                    _currentChapter = active; // MUST SAVE STATE
                    CurrentChapterText.Text = active.Name;
                    ChapterComboBox.SelectedItem = active;
                    UpdateChapterProgressBounds(); // Update max bounds for new chapter length
                }
                
                // Sleep Timer Trap: End of Chapter feature
                if (_stopAtNextChapter)
                {
                    _audioEngine.Pause();
                    _isPlaying = false;
                    UpdatePlayPauseButton();
                    _stopAtNextChapter = false;
                    SleepTimerComboBox.SelectedIndex = 0; // Reset to Off
                }
            });
        }

        private void AudioEngine_TimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_currentBook != null)
                {
                    if (_currentChapter != null && !_isPlaylistMode)
                    {
                        // Custom Chapter-Relative Progress Calculations
                        long chapterDuration = GetChapterDuration(_currentChapter);
                        long chapterRelativeTime = e.Time - _currentChapter.StartTimeOffset;
                        
                        // Prevent UI glitching if VLC events fire slightly out of bounds
                        if (chapterRelativeTime < 0) chapterRelativeTime = 0;
                        if (chapterRelativeTime > chapterDuration) chapterRelativeTime = chapterDuration;

                        if (!_isDraggingSlider)
                        {
                            ProgressSlider.Value = chapterRelativeTime;
                        }
                        
                        CurrentTimeText.Text = FormatTimeMs(chapterRelativeTime);
                        TotalTimeText.Text = "-" + FormatTimeMs(chapterDuration - chapterRelativeTime);
                    }
                    else
                    {
                        // Fallback logic for whole-book (No chapters found)
                        if (!_isDraggingSlider && e.Time <= ProgressSlider.Maximum)
                        {
                            ProgressSlider.Value = e.Time;
                        }
                        CurrentTimeText.Text = FormatTimeMs(e.Time);
                        long remaining = _currentBook.TotalDuration - e.Time;
                        TotalTimeText.Text = "-" + FormatTimeMs(remaining > 0 ? remaining : 0);
                    }
                    
                    _currentBook.LastPlayedPosition = e.Time;
                }
            });
        }

        private void AudioEngine_EndReached(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_currentBook != null && _appConfig.SavePosition)
                {
                    _currentBook.LastPlayedPosition = 0;
                    _dbService.AddOrUpdateBook(_currentBook);
                }

                if (_isPlaylistMode && _playlistIndex < _playlist.Count - 1)
                {
                    LoadPlaylist(_playlist, _playlistIndex + 1);
                }
                else
                {
                    _isPlaying = false;
                    UpdatePlayPauseButton();
                    ProgressSlider.Value = 0;
                    CurrentTimeText.Text = FormatTimeMs(0);
                }
            });
        }

        private void ProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void ProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = false;
            if (_currentBook != null)
            {
                long targetTime = (long)ProgressSlider.Value;
                
                // Seeking targets the global time, so add the chapter offset!
                if (_currentChapter != null && !_isPlaylistMode)
                {
                    targetTime += _currentChapter.StartTimeOffset;
                }
                
                _audioEngine.SetTime(targetTime);
            }
        }

        private string FormatTimeMs(long milliseconds)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(milliseconds);
            if (t.Hours > 0)
                return string.Format("{0}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
            else
                return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_currentBook != null && _appConfig.SavePosition)
            {
                _currentBook.LastPlayedPosition = _audioEngine.GetTime();
                _dbService.AddOrUpdateBook(_currentBook);
            }

            _audioEngine.Dispose();
            base.OnClosed(e);
        }
    }
}