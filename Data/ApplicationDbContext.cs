
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Models;

namespace SpotifyNet.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<MusicPlaylist> MusicPlaylists
            => Set<MusicPlaylist>();

        public DbSet<MusicTrack> MusicTracks
            => Set<MusicTrack>();

        public DbSet<MusicPlaylistTrack> MusicPlaylistTracks
            => Set<MusicPlaylistTrack>();

        public DbSet<MusicPlaylistCollaborator> MusicPlaylistCollaborators
            => Set<MusicPlaylistCollaborator>();

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<MusicPlaylist>()
                .HasIndex(p => new
                {
                    p.OwnerId,
                    p.Slug
                })
                .IsUnique();

            builder.Entity<MusicPlaylist>()
                .HasIndex(p => p.ShareToken)
                .IsUnique();

            builder.Entity<MusicPlaylistCollaborator>()
                .HasKey(c => new { c.MusicPlaylistId, c.UserId });

            builder.Entity<MusicPlaylistCollaborator>()
                .HasOne(c => c.Playlist)
                .WithMany(p => p.Collaborators)
                .HasForeignKey(c => c.MusicPlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MusicPlaylistCollaborator>()
                .HasIndex(c => c.UserId);

            builder.Entity<MusicTrack>()
                .HasOne(t => t.Playlist)
                .WithMany(p => p.Tracks)
                .HasForeignKey(t => t.MusicPlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MusicTrack>()
                .HasIndex(t => t.MusicPlaylistId);

            // Relación muchos a muchos adicional.
            builder.Entity<MusicPlaylistTrack>()
                .HasKey(pt => new
                {
                    pt.MusicPlaylistId,
                    pt.MusicTrackId
                });

            builder.Entity<MusicPlaylistTrack>()
                .HasOne(pt => pt.Playlist)
                .WithMany(p => p.SharedTracks)
                .HasForeignKey(pt => pt.MusicPlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MusicPlaylistTrack>()
                .HasOne(pt => pt.Track)
                .WithMany(t => t.SharedIn)
                .HasForeignKey(pt => pt.MusicTrackId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MusicPlaylistTrack>()
                .HasIndex(pt => pt.MusicTrackId);
        }
    }
}
