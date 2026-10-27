using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpotifyNet.Data;

#nullable disable
namespace SpotifyNet.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261026110000_PerfilesYComunidad")]
    public partial class PerfilesYComunidad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic", table: "MusicPlaylists", type: "INTEGER",
                nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(
                name: "ShareToken", table: "MusicPlaylists", type: "TEXT",
                maxLength: 64, nullable: true);
            migrationBuilder.CreateTable(
                name: "MusicPlaylistCollaborators",
                columns: table => new
                {
                    MusicPlaylistId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MusicPlaylistCollaborators", x => new { x.MusicPlaylistId, x.UserId });
                    table.ForeignKey("FK_MusicPlaylistCollaborators_MusicPlaylists_MusicPlaylistId",
                        x => x.MusicPlaylistId, "MusicPlaylists", "Id", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylistCollaborators_UserId",
                table: "MusicPlaylistCollaborators", column: "UserId");
            migrationBuilder.CreateIndex(
                name: "IX_MusicPlaylists_ShareToken", table: "MusicPlaylists",
                column: "ShareToken", unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MusicPlaylistCollaborators");
            migrationBuilder.DropIndex(name: "IX_MusicPlaylists_ShareToken", table: "MusicPlaylists");
            migrationBuilder.DropColumn(name: "IsPublic", table: "MusicPlaylists");
            migrationBuilder.DropColumn(name: "ShareToken", table: "MusicPlaylists");
        }
    }
}
