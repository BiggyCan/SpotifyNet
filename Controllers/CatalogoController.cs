using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Data;
using SpotifyNet.Models;

namespace SpotifyNet.Controllers
{
    [Authorize]
    public class CatalogoController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CatalogoController(ApplicationDbContext db) => _db = db;
        private string Usuario => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        [HttpGet]
        public async Task<IActionResult> Index(string vista = "artistas", string? buscar = null)
        {
            vista = vista == "albumes" ? "albumes" : "artistas";
            buscar = (buscar ?? "").Trim();
            if (buscar.Length > 90) buscar = buscar[..90];
            // Solo los archivos de los que el usuario es propietario.
            var tracks = await _db.MusicTracks.AsNoTracking()
                .Include(t => t.Playlist)
                .Where(t => t.Playlist.OwnerId == Usuario)
                .OrderBy(t => t.Artist).ThenBy(t => t.Title).ToListAsync();
            var songs = tracks.Select(t => new CatalogoSongViewModel {
                Id = t.Id, Title = t.Title,
                Artist = string.IsNullOrWhiteSpace(t.Artist) ? "Artista desconocido" : t.Artist,
                Album = string.IsNullOrWhiteSpace(t.Album) ? "Sin álbum" : t.Album,
                Url = Url.Action("Audio", "Biblioteca", new { id = t.Id }) ?? "",
                PlaylistName = t.Playlist.Name,
                PlaylistUrl = t.Playlist.IsCustom
                    ? (Url.Action("Playlist", "Biblioteca", new { slug = t.Playlist.Slug }) ?? "")
                    : (Url.Action("Playlist", "Home", new { slug = t.Playlist.Slug }) ?? ""),
                CoverUrl = !string.IsNullOrWhiteSpace(t.Playlist.CoverFileName)
                    ? (Url.Action("Portada", "Biblioteca", new { slug = t.Playlist.Slug }) ?? "")
                    : "/images/referencias/playlist-cano.jpg"
            }).ToList();
            if (buscar.Length > 0)
                songs = songs.Where(t =>
                    t.Title.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                    t.Artist.Contains(buscar, StringComparison.OrdinalIgnoreCase) ||
                    t.Album.Contains(buscar, StringComparison.OrdinalIgnoreCase)).ToList();
            var model = new CatalogoViewModel { View = vista, Search = buscar,
                TotalSongs = songs.Count,
                Groups = songs.GroupBy(s => vista == "albumes" ? s.Album : s.Artist,
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase)
                    .Select(g => new CatalogoGroupViewModel { Name = g.Key,
                        Songs = g.OrderBy(s => s.Title, StringComparer.CurrentCultureIgnoreCase).ToList() })
                    .ToList()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Datos(Guid id)
        {
            var track = await _db.MusicTracks.AsNoTracking()
                .Where(t => t.Id == id && t.Playlist.OwnerId == Usuario)
                .Select(t => new { t.Id, t.Title, t.Artist, t.Album })
                .FirstOrDefaultAsync();
            if (track == null) return NotFound(new { error = "Esta canción no pertenece a tu biblioteca." });
            return Json(track);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(Guid id, string titulo, string artista, string album)
        {
            var cancion = await _db.MusicTracks.Include(t => t.Playlist)
                .FirstOrDefaultAsync(t => t.Id == id && t.Playlist.OwnerId == Usuario);
            if (cancion == null) return NotFound(new { error = "No tienes permiso para editar esta canción." });
            titulo = (titulo ?? "").Trim();
            artista = (artista ?? "").Trim();
            album = (album ?? "").Trim();
            if (titulo.Length < 1 || titulo.Length > 250 || artista.Length < 1 || artista.Length > 150 || album.Length > 180)
                return BadRequest(new { error = "Título: 1-250. Artista: 1-150. Álbum: máximo 180 caracteres." });
            cancion.Title = titulo;
            cancion.Artist = artista;
            cancion.Album = album.Length > 0 ? album : "Sin álbum";
            await _db.SaveChangesAsync();
            return Json(new { ok = true, title = cancion.Title, artist = cancion.Artist, album = cancion.Album });
        }
    }
}
