using System;
using System.IO;
using Microsoft.Data.Sqlite;
using AudiobookPlayer.Models;

namespace AudiobookPlayer.Services
{
    /// <summary>
    /// Handles database operations for storing and retrieving audiobook data,
    /// primarily focused on persisting playback positions.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _dbPath;

        public DatabaseService()
        {
            // Create an SQLite database named library.db in the application's local app data folder.
            var appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataFolderPath, "AudiobookPlayer");
            
            // Ensure the directory exists
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            _dbPath = Path.Combine(appFolder, "library.db");

            InitializeDatabase();
        }

        /// <summary>
        /// Initializes the database and creates the Books table if it doesn't exist.
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Create the Books table with FilePath as a UNIQUE constraint for UPSERT operation
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Books (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FilePath TEXT UNIQUE NOT NULL,
                    Title TEXT,
                    Author TEXT,
                    LastPlayedPosition INTEGER DEFAULT 0,
                    TotalDuration INTEGER DEFAULT 0
                );
            ";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Adds a new book to the database or updates the existing one based on its FilePath.
        /// </summary>
        /// <param name="book">The book object to insert or update.</param>
        public void AddOrUpdateBook(Book book)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            // Insert new book or update if the FilePath already exists.
            // Using UPSERT syntax (ON CONFLICT).
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Books (FilePath, Title, Author, LastPlayedPosition, TotalDuration)
                VALUES ($FilePath, $Title, $Author, $LastPlayedPosition, $TotalDuration)
                ON CONFLICT(FilePath) DO UPDATE SET
                    Title = excluded.Title,
                    Author = excluded.Author,
                    LastPlayedPosition = excluded.LastPlayedPosition,
                    TotalDuration = excluded.TotalDuration;
            ";

            command.Parameters.AddWithValue("$FilePath", book.FilePath);
            command.Parameters.AddWithValue("$Title", book.Title ?? string.Empty);
            command.Parameters.AddWithValue("$Author", book.Author ?? string.Empty);
            command.Parameters.AddWithValue("$LastPlayedPosition", book.LastPlayedPosition);
            command.Parameters.AddWithValue("$TotalDuration", book.TotalDuration);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Retrieves the last saved playback position (in milliseconds) for a given file.
        /// </summary>
        /// <param name="filePath">The file path of the audiobook.</param>
        /// <returns>The position in milliseconds, or 0 if not found.</returns>
        public long GetSavedPosition(string filePath)
        {
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT LastPlayedPosition FROM Books WHERE FilePath = $FilePath;";
            command.Parameters.AddWithValue("$FilePath", filePath);

            var result = command.ExecuteScalar();
            if (result != null && long.TryParse(result.ToString(), out long position))
            {
                return position;
            }

            return 0; // Return 0 as the default starting position if not found
        }
    }
}
