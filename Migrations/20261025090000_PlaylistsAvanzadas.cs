using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpotifyNet.Data;

#nullable disable

namespace SpotifyNet.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261025090000_PlaylistsAvanzadas")]
    public partial class PlaylistsAvanzadas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Description", table: "MusicPlaylists",
                type: "TEXT", maxLength: 600, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CoverFileName", table: "MusicPlaylists",
                type: "TEXT", maxLength: 120, nullable: true);
            migrationBuilder.AddColumn<int>(name: "Position", table: "MusicTracks",
                type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "Position", table: "MusicPlaylistTracks",
                type: "INTEGER", nullable: true);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Description", table: "MusicPlaylists");
            migrationBuilder.DropColumn(name: "CoverFileName", table: "MusicPlaylists");
            migrationBuilder.DropColumn(name: "Position", table: "MusicTracks");
            migrationBuilder.DropColumn(name: "Position", table: "MusicPlaylistTracks");
        }
    }
}
