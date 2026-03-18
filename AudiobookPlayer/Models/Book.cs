using System.Windows.Media.Imaging;

namespace AudiobookPlayer.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public long LastPlayedPosition { get; set; } // stored in milliseconds
        public long TotalDuration { get; set; }      // stored in milliseconds
        
        public BitmapImage? CoverImage { get; set; }
    }
}
