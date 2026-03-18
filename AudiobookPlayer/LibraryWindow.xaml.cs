using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using AudiobookPlayer.Models;

namespace AudiobookPlayer
{
    /// <summary>
    /// Represents a single audiobook (a folder containing audio files).
    /// </summary>
    public class AudiobookFileInfo
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
    }

    public partial class LibraryWindow : Window
    {
        private static readonly string[] SupportedExtensions = { ".m4b", ".mp3", ".m4a", ".ogg", ".wav" };

        /// <summary>
        /// The full path of the audiobook file the user selected. Read by MainWindow after ShowDialog.
        /// </summary>
        public List<string> SelectedPlaylist { get; private set; } = new List<string>();

        public LibraryWindow()
        {
            InitializeComponent();
            Loaded += LibraryWindow_Loaded;
        }

        private void LibraryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var config = AppConfig.Load();

            if (string.IsNullOrWhiteSpace(config.LibraryPath) || !Directory.Exists(config.LibraryPath))
            {
                MessageBox.Show(
                    "No library folder is set, or the folder doesn't exist.\n\nPlease go to Settings (⚙️) and select your audiobook folder first.",
                    "Library Not Configured",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Close();
                return;
            }

            LibraryPathLabel.Text = config.LibraryPath;

            // Each subfolder = one audiobook
            var bookFolders = Directory.GetDirectories(config.LibraryPath)
                .Select(folderPath =>
                {
                    var audioFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .ToList();

                    return new AudiobookFileInfo
                    {
                        Title = Path.GetFileName(folderPath),
                        Subtitle = $"{audioFiles.Count} audio file{(audioFiles.Count != 1 ? "s" : "")}",
                        FullPath = folderPath
                    };
                })
                .Where(b => b.Subtitle != "0 audio files") // Only show folders that actually contain audio
                .OrderBy(b => b.Title)
                .ToList();

            BooksListBox.ItemsSource = bookFolders;
            BookCountLabel.Text = $"{bookFolders.Count} book{(bookFolders.Count != 1 ? "s" : "")} found";
        }

        private void BooksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BooksListBox.SelectedItem is AudiobookFileInfo selected)
            {
                // Find the best file to load from this book folder:
                // Prefer a single .m4b file, otherwise pick the first audio file alphabetically
                var audioFiles = Directory.GetFiles(selected.FullPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .OrderBy(f => f)
                    .ToList();

                var m4bFile = audioFiles.FirstOrDefault(f => Path.GetExtension(f).ToLowerInvariant() == ".m4b");
                
                if (m4bFile != null)
                {
                    SelectedPlaylist = new List<string> { m4bFile };
                }
                else
                {
                    SelectedPlaylist = audioFiles;
                }

                if (SelectedPlaylist.Count > 0)
                {
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
