using System;
using System.Collections.Generic;
namespace SpotifyNet.Models
{
    public class CatalogoSongViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string Url { get; set; } = "";
        public string PlaylistName { get; set; } = "";
        public string PlaylistUrl { get; set; } = "";
        public string CoverUrl { get; set; } = "";
    }
    public class CatalogoGroupViewModel
    {
        public string Name { get; set; } = "";
        public List<CatalogoSongViewModel> Songs { get; set; } = new();
    }
    public class CatalogoViewModel
    {
        public string View { get; set; } = "artistas";
        public string Search { get; set; } = "";
        public int TotalSongs { get; set; }
        public List<CatalogoGroupViewModel> Groups { get; set; } = new();
    }
}
