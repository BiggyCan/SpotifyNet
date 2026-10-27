using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyNet.Migrations
{
    /// <inheritdoc />
    public partial class PlaylistsCompartidas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MusicPlaylistTracks",
                columns: table => new
                {
                    MusicPlaylistId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MusicTrackId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylistTracks", x => new { x.MusicPlaylistId, x.MusicTrackId });
                    table.ForeignKey(
                        name: "FK_MusicPlaylistTracks_MusicPlaylists_MusicPlaylistId",
                        column: x => x.MusicPlaylistId,
                        principalTable: "MusicPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MusicPlaylistTracks_MusicTracks_MusicTrackId",
                        column: x => x.MusicTrackId,
                        principalTable: "MusicTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistTracks_MusicTrackId",
                table: "MusicPlaylistTracks",
                column: "MusicTrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MusicPlaylistTracks");
        }
    }
}
