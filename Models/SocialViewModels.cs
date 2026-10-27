using System;
using System.Collections.Generic;

namespace SpotifyNet.Models
{
    public class SocialPlaylistSummary
    {
        public string Name { get; set; } = "";
        public string Token { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Owner { get; set; } = "";
        public bool IsPublic { get; set; }
        public bool IsOwner { get; set; }
    }

    public class SocialDashboardViewModel
    {
        public string DisplayName { get; set; } = "";
        public string UserId { get; set; } = "";
        public List<SocialPlaylistSummary> Owned { get; set; } = new();
        public List<SocialPlaylistSummary> Collaborations { get; set; } = new();
    }

    public class SocialSongViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
    }

    public class SocialSharedViewModel
    {
        public string Token { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string OwnerName { get; set; } = "";
        public string OwnerId { get; set; } = "";
        public bool IsPublic { get; set; }
        public bool IsOwner { get; set; }
        public bool CanContribute { get; set; }
        public List<SocialSongViewModel> Songs { get; set; } = new();
        public List<SocialSongViewModel> AvailableSongs { get; set; } = new();
    }

    public class SocialPublicProfileViewModel
    {
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Bio { get; set; } = "";
        public List<SocialPlaylistSummary> PublicPlaylists { get; set; } = new();
    }
}
