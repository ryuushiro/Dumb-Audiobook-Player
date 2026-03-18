using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using AudiobookPlayer.Models;

namespace AudiobookPlayer.Services
{
    /// <summary>
    /// Handles extraction of metadata from audiobook files (e.g., MP3, M4B)
    /// using TagLibSharp.
    /// </summary>
    public class MetadataService
    {
        /// <summary>
        /// Extracts metadata (Title, Author, Total Duration, Cover Art) from the specified audio file.
        /// </summary>
        /// <param name="filePath">The absolute path to the audiobook file.</param>
        /// <returns>A populated Book object containing the extracted properties.</returns>
        public Book ExtractMetadata(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException($"The audio file at '{filePath}' was not found.");
            }

            // Using TagLib to read file metadata
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;

            var book = new Book
            {
                FilePath = filePath,
                // Fallback to filename if Title is missing
                Title = !string.IsNullOrWhiteSpace(tag.Title) ? tag.Title : Path.GetFileNameWithoutExtension(filePath),
                // Album artist is usually priority for book authors, followed by performer
                Author = tag.FirstAlbumArtist ?? tag.FirstPerformer ?? "Unknown Author",
                TotalDuration = (long)file.Properties.Duration.TotalMilliseconds
            };

            // Extract Cover Art if available
            if (tag.Pictures.Length > 0)
            {
                var picture = tag.Pictures.FirstOrDefault();
                if (picture != null)
                {
                    book.CoverImage = LoadImage(picture.Data.Data);
                }
            }

            return book;
        }

        /// <summary>
        /// Converts a byte array representing an image into a WPF BitmapImage.
        /// </summary>
        /// <param name="imageData">The image byte array.</param>
        /// <returns>A BitmapImage suitable for WPF UI binding, or null if no data is provided.</returns>
        private BitmapImage? LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;

            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(imageData))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load image directly into memory so we can dispose the stream
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }

            bitmap.Freeze(); // Makes the object thread-safe and read-only, optimizing UI rendering
            return bitmap;
        }
    }
}
