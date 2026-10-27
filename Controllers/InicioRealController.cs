using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Data;

namespace SpotifyNet.Controllers
{
    /// <summary>
    /// Biblioteca personal de Inicio. Usa la misma base de datos y las mismas
    /// reglas de acceso que el catálogo. No altera las playlists ni los MP3.
    /// </summary>
    [Authorize]
    public class InicioRealController : Controller
    {
        private readonly ApplicationDbContext _db;

        public InicioRealController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Datos()
        {
            var usuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(usuario)) return Unauthorized();

            // Solo música cuyo archivo original pertenece a la cuenta activa.
            // Las colaboraciones se siguen consultando desde Mi espacio.
            var total = await _db.MusicTracks.AsNoTracking()
                .CountAsync(t => t.Playlist.OwnerId == usuario);

            var pistas = await _db.MusicTracks.AsNoTracking()
                .Where(t => t.Playlist.OwnerId == usuario)
                .OrderByDescending(t => t.AddedAtUtc)
                .ThenBy(t => t.Title)
                .Take(120)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Artist,
                    t.Album,
                    t.AddedAtUtc,
                    PlaylistName = t.Playlist.Name,
                    PlaylistSlug = t.Playlist.Slug,
                    PlaylistIsCustom = t.Playlist.IsCustom,
                    PlaylistCover = t.Playlist.CoverFileName
                })
                .ToListAsync();

            var listas = await _db.MusicPlaylists.AsNoTracking()
                .Where(p => p.OwnerId == usuario && p.IsCustom)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Take(30)
                .Select(p => new
                {
                    p.Name,
                    p.Slug,
                    p.CoverFileName
                })
                .ToListAsync();

            // Favoritos vinculados a la cuenta activa, sin compartir datos de otras cuentas.
            var idsFavoritos = await _db.MusicPlaylistTracks.AsNoTracking()
                .Where(v => v.Playlist.OwnerId == usuario && v.Playlist.Slug == "tus-me-gusta")
                .Select(v => v.MusicTrackId)
                .ToListAsync();
            var favoritos = idsFavoritos.ToHashSet();

            var canciones = pistas.Select(p => new
            {
                id = p.Id,
                liked = favoritos.Contains(p.Id),
                title = p.Title,
                artist = string.IsNullOrWhiteSpace(p.Artist) ? "Archivo local" : p.Artist,
                album = string.IsNullOrWhiteSpace(p.Album) ? "Sin álbum" : p.Album,
                playlistName = p.PlaylistName,
                playlistUrl = p.PlaylistIsCustom
                    ? Url.Action("Playlist", "Biblioteca", new { slug = p.PlaylistSlug })
                    : Url.Action("Playlist", "Home", new { slug = p.PlaylistSlug }),
                url = Url.Action("Audio", "Biblioteca", new { id = p.Id }),
                cover = string.IsNullOrWhiteSpace(p.PlaylistCover)
                    ? null
                    : Url.Action("Portada", "Biblioteca", new { slug = p.PlaylistSlug })
            }).ToList();

            var playlists = listas.Select(p => new
            {
                name = p.Name,
                url = Url.Action("Playlist", "Biblioteca", new { slug = p.Slug }),
                cover = string.IsNullOrWhiteSpace(p.CoverFileName)
                    ? null
                    : Url.Action("Portada", "Biblioteca", new { slug = p.Slug })
            }).ToList();

            return Json(new { total, canciones, playlists });
        }
    }
}
