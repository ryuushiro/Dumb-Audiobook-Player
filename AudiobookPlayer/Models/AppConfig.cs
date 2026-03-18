using System;
using System.IO;
using System.Text.Json;

namespace AudiobookPlayer.Models
{
    /// <summary>
    /// Holds application-level settings, persisted as JSON.
    /// </summary>
    public class AppConfig
    {
        public string LibraryPath { get; set; } = string.Empty;
        public int SkipDuration { get; set; } = 15;
        public bool SavePosition { get; set; } = true;

        private static readonly string ConfigDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudiobookPlayer");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.json");

        /// <summary>
        /// Loads the config from disk, or returns defaults if the file doesn't exist.
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch { /* Return defaults on any error */ }
            return new AppConfig();
        }

        /// <summary>
        /// Saves the current config to disk as JSON.
        /// </summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
