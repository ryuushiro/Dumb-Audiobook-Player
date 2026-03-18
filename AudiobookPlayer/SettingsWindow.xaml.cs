using System.Windows;
using System.Windows.Controls;
using AudiobookPlayer.Models;

namespace AudiobookPlayer
{
    public partial class SettingsWindow : Window
    {
        private AppConfig _config;

        public SettingsWindow()
        {
            InitializeComponent();
            _config = AppConfig.Load();
            LoadSettingsToUI();
        }

        /// <summary>
        /// Populates the UI controls with the current config values.
        /// </summary>
        private void LoadSettingsToUI()
        {
            // Library Path
            if (!string.IsNullOrEmpty(_config.LibraryPath))
            {
                LibraryPathText.Text = _config.LibraryPath;
            }

            // Skip Duration - find the matching ComboBoxItem by Tag
            foreach (ComboBoxItem item in SkipDurationComboBox.Items)
            {
                if (item.Tag?.ToString() == _config.SkipDuration.ToString())
                {
                    SkipDurationComboBox.SelectedItem = item;
                    break;
                }
            }

            // Save Position
            SavePositionCheckBox.IsChecked = _config.SavePosition;

            // Theme
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString() == _config.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            // .NET 8 WPF supports OpenFolderDialog
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Library Folder",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                LibraryPathText.Text = dialog.FolderName;
                LibraryPathText.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Library Path
            _config.LibraryPath = LibraryPathText.Text == "No folder selected" ? "" : LibraryPathText.Text;

            // Skip Duration
            if (SkipDurationComboBox.SelectedItem is ComboBoxItem selectedSkip)
            {
                if (int.TryParse(selectedSkip.Tag?.ToString(), out int skipVal))
                {
                    _config.SkipDuration = skipVal;
                }
            }

            // Save Position
            _config.SavePosition = SavePositionCheckBox.IsChecked ?? true;

            // Theme
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedTheme)
            {
                _config.Theme = selectedTheme.Tag?.ToString() ?? "DarkGreen";
            }

            _config.Save();
            
            // Instantly apply theme!
            App.ApplyTheme(_config.Theme);
            
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
