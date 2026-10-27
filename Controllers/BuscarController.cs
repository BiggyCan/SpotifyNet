using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Data;

namespace SpotifyNet.Controllers;

/// <summary>
/// Búsqueda unificada sobre música propia, favoritos y playlists de referencia.
/// No expone canciones o playlists privadas de otras cuentas.
/// </summary>
[Authorize]
public class BuscarController : Controller
{
    private readonly ApplicationDbContext _db;
    public BuscarController(ApplicationDbContext db) => _db = db;

    private sealed record PlaylistDemo(string Title, string Slug, string Image);
    private sealed record ResultadoPlaylist(string Title, string Subtitle, string Url, string Image);
    private sealed record ResultadoCancion(string Title, string Artist, string Album,
        string PlaylistName, string Url, string Image, string AudioUrl, Guid TrackId);
    private sealed record ResultadoGrupo(string Title, string Subtitle, string Url, string Image);

    private static readonly PlaylistDemo[] Catalogo =
    {
        new("KISS ME KISS ME KISS ME AGAIN", "kiss-me-kiss-me-kiss-me-again", "rapido-kiss.jpg"),
        new("Trap Cano", "trap-cano", "rapido-trap.jpg"),
        new("Hits de Internet", "hits-de-internet", "rapido-hits.jpg"),
        new("Grand Theft Auto Official Playlist", "grand-theft-auto-official-playlist", "rapido-gta.jpg"),
        new("Música para programar", "musica-para-programar", "rapido-programar.jpg"),
        new("Fiesta!!!", "fiesta", "rapido-fiesta.jpg"),
        new("This Is Astrix", "this-is-astrix", "rapido-astrix.jpg"),
        new("Tus me gusta", "tus-me-gusta", "rapido-liked.jpg"),
        new("CANO", "cano", "playlist-cano.jpg"),
        new("Trap Bastardo", "trap-bastardo", "playlist-trap-bastardo.jpg"),
        new("Para jugar Tryhard", "para-jugar-tryhard", "playlist-tryhard.jpg"),
        new("I.A. Trap", "ia-trap", "playlist-ia-trap.jpg"),
        new("GYM", "gym", "playlist-gym.jpg"),
        new("Blood Oceans (How Many?)", "blood-oceans-how-many", "playlist-blood.jpg"),
        new("Trippy Cano", "trippy-cano", "playlist-trippy.jpg")
    };

    private static string ImagenDemo(string slug) =>
        "/images/referencias/" + (Catalogo.FirstOrDefault(p =>
            p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))?.Image ?? "rapido-liked.jpg");

    [HttpGet]
    public IActionResult Explorar(string? q)
    {
        var consulta = (q ?? "").Trim();
        if (consulta.Length > 80) consulta = consulta[..80];
        ViewData["Consulta"] = consulta;
        return View();
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Resultados([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var termino = (q ?? "").Trim();
        if (termino.Length > 80) termino = termino[..80];
        if (termino.Length < 2)
        {
            return Json(new
            {
                playlists = Array.Empty<ResultadoPlaylist>(),
                songs = Array.Empty<ResultadoCancion>(),
                artists = Array.Empty<ResultadoGrupo>(),
                albums = Array.Empty<ResultadoGrupo>()
            });
        }

        var usuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(usuario)) return Unauthorized();
        var filtro = termino.ToLowerInvariant();

        var playlists = Catalogo
            .Where(p => p.Title.Contains(termino, StringComparison.OrdinalIgnoreCase))
            .Select(p => new ResultadoPlaylist(p.Title, "Playlist de referencia",
                Url.Action("Playlist", "Home", new { slug = p.Slug }) ?? "/",
                "/images/referencias/" + p.Image))
            .ToList();

        var propias = await _db.MusicPlaylists.AsNoTracking()
            .Where(p => p.OwnerId == usuario &&
                (p.Name.ToLower().Contains(filtro) || p.Slug.ToLower().Contains(filtro)))
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(30)
            .Select(p => new { p.Name, p.Slug, p.IsCustom, p.CoverFileName })
            .ToListAsync(cancellationToken);

        foreach (var p in propias)
        {
            var destino = p.IsCustom
                ? Url.Action("Playlist", "Biblioteca", new { slug = p.Slug }) ?? "/"
                : Url.Action("Playlist", "Home", new { slug = p.Slug }) ?? "/";
            var portada = !string.IsNullOrWhiteSpace(p.CoverFileName)
                ? Url.Action("Portada", "Biblioteca", new { slug = p.Slug }) ?? ImagenDemo(p.Slug)
                : ImagenDemo(p.Slug);
            playlists.Add(new ResultadoPlaylist(p.Name,
                p.IsCustom ? "Tu playlist personal" : "Playlist de tu biblioteca", destino, portada));
        }

        // Se buscan también coincidencias por álbum, no solo por título o artista.
        var cancionesBase = await _db.MusicTracks.AsNoTracking()
            .Where(t => t.Playlist.OwnerId == usuario &&
                (t.Title.ToLower().Contains(filtro) ||
                 t.Artist.ToLower().Contains(filtro) ||
                 t.Album.ToLower().Contains(filtro)))
            .OrderBy(t => t.Title)
            .Take(160)
            .Select(t => new
            {
                t.Id, t.Title, t.Artist, t.Album,
                PlaylistName = t.Playlist.Name,
                PlaylistSlug = t.Playlist.Slug,
                t.Playlist.IsCustom,
                t.Playlist.CoverFileName
            })
            .ToListAsync(cancellationToken);

        var canciones = cancionesBase.Take(12).Select(t =>
        {
            var destino = t.IsCustom
                ? Url.Action("Playlist", "Biblioteca", new { slug = t.PlaylistSlug }) ?? "/"
                : Url.Action("Playlist", "Home", new { slug = t.PlaylistSlug }) ?? "/";
            var imagen = !string.IsNullOrWhiteSpace(t.CoverFileName)
                ? Url.Action("Portada", "Biblioteca", new { slug = t.PlaylistSlug }) ?? ImagenDemo(t.PlaylistSlug)
                : ImagenDemo(t.PlaylistSlug);
            return new ResultadoCancion(t.Title, t.Artist, t.Album,
                t.PlaylistName, destino, imagen,
                Url.Action("Audio", "Biblioteca", new { id = t.Id }) ?? "", t.Id);
        }).ToList();

        // Los grupos se generan a partir de canciones reales de esta cuenta.
        var artistas = cancionesBase
            .Where(t => !string.IsNullOrWhiteSpace(t.Artist) &&
                !t.Artist.Equals("Archivo local", StringComparison.OrdinalIgnoreCase) &&
                t.Artist.Contains(termino, StringComparison.OrdinalIgnoreCase))
            .GroupBy(t => t.Artist, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .Select(g => new ResultadoGrupo(g.Key, $"{g.Count()} canción(es) · Tu biblioteca",
                Url.Action("Index", "Catalogo", new { vista = "artistas", buscar = g.Key }) ?? "/Catalogo",
                ImagenDemo(g.First().PlaylistSlug)))
            .ToList();

        var albumes = cancionesBase
            .Where(t => !string.IsNullOrWhiteSpace(t.Album) &&
                !t.Album.Equals("Sin álbum", StringComparison.OrdinalIgnoreCase) &&
                t.Album.Contains(termino, StringComparison.OrdinalIgnoreCase))
            .GroupBy(t => t.Album, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .Select(g => new ResultadoGrupo(g.Key, $"{g.Count()} canción(es) · Álbum",
                Url.Action("Index", "Catalogo", new { vista = "albumes", buscar = g.Key }) ?? "/Catalogo",
                ImagenDemo(g.First().PlaylistSlug)))
            .ToList();

        return Json(new
        {
            playlists = playlists.GroupBy(p => p.Url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Last()).Take(12).ToList(),
            songs = canciones,
            artists = artistas,
            albums = albumes
        });
    }
}
