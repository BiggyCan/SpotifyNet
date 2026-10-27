using System.Collections.Generic;

namespace SpotifyNet.Models
{
    public class PlaylistTrackViewModel
    {
        public int Number { get; set; }
        public string Cover { get; set; } = "";
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string AddedDate { get; set; } = "";
        public string Duration { get; set; } = "";
    }

    public class PlaylistDetailViewModel
    {
        public string Slug { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Playlist pública";
        public string Owner { get; set; } = "";
        public string Cover { get; set; } = "";
        public string BannerColor { get; set; } = "#8c0357";
        public string Description { get; set; } = "";
        public string SavesText { get; set; } = "";
        public string TrackCountText { get; set; } = "";
        public string DurationText { get; set; } = "";
        public List<PlaylistTrackViewModel> Tracks { get; set; } = new();
        public List<PlaylistTrackViewModel> Recommendations { get; set; } = new();
    }
}