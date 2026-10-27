
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpotifyNet.Models
{
    public class MusicPlaylist
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string OwnerId { get; set; } = "";

        [Required]
        [MaxLength(120)]
        public string Slug { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        public bool IsCustom { get; set; }

        // Privadas por defecto; publicarlas requiere permiso explícito.
        public bool IsPublic { get; set; }

        [MaxLength(64)]
        public string? ShareToken { get; set; }

        public ICollection<MusicPlaylistCollaborator> Collaborators { get; set; }
            = new List<MusicPlaylistCollaborator>();

        [MaxLength(600)]
        public string Description { get; set; } = "";

        [MaxLength(120)]
        public string? CoverFileName { get; set; }

        public DateTime CreatedAtUtc { get; set; }
            = DateTime.UtcNow;

        // Canciones originales de esta playlist.
        public ICollection<MusicTrack> Tracks { get; set; }
            = new List<MusicTrack>();

        // Canciones añadidas desde otras playlists.
        public ICollection<MusicPlaylistTrack> SharedTracks
        {
            get; set;
        } = new List<MusicPlaylistTrack>();
    }

    public class MusicTrack
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Playlist que conserva el archivo original.
        public Guid MusicPlaylistId { get; set; }

        public MusicPlaylist Playlist { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string Title { get; set; } = "";

        [MaxLength(150)]
        public string Artist { get; set; } = "Archivo local";

        // Álbum del archivo; editable por quien subió la canción.
        [MaxLength(180)]
        public string Album { get; set; } = "Sin álbum";

        [Required]
        public string StoredFileName { get; set; } = "";

        [Required]
        public string ContentType { get; set; } = "";

        public long SizeBytes { get; set; }

        // Posición manual; null conserva el orden anterior por fecha.
        public int? Position { get; set; }

        public DateTime AddedAtUtc { get; set; }
            = DateTime.UtcNow;

        // Otras playlists que contienen esta canción.
        public ICollection<MusicPlaylistTrack> SharedIn
        {
            get; set;
        } = new List<MusicPlaylistTrack>();
    }

    // Relación adicional sin duplicar el MP3.
    public class MusicPlaylistTrack
    {
        public Guid MusicPlaylistId { get; set; }

        public MusicPlaylist Playlist { get; set; } = null!;

        public Guid MusicTrackId { get; set; }

        public MusicTrack Track { get; set; } = null!;

        // Posición manual; null conserva el orden anterior por fecha.
        public int? Position { get; set; }

        public DateTime AddedAtUtc { get; set; }
            = DateTime.UtcNow;
    }
}

namespace SpotifyNet.Models
{
    public class MusicPlaylistCollaborator
    {
        public Guid MusicPlaylistId { get; set; }
        public MusicPlaylist Playlist { get; set; } = null!;
        [Required]
        public string UserId { get; set; } = "";
        public DateTime AddedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
