using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpotifyNet.Data;

#nullable disable
namespace SpotifyNet.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261027120000_CatalogoMusical")]
    public partial class CatalogoMusical : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Album", table: "MusicTracks", type: "TEXT",
                maxLength: 180, nullable: false, defaultValue: "Sin álbum");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Album", table: "MusicTracks");
        }
    }
}
