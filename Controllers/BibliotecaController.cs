
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Data;
using SpotifyNet.Models;

namespace SpotifyNet.Controllers
{
    [Authorize]
    public class BibliotecaController : Controller
    {
        private const long MaxAudioBytes = 30L * 1024 * 1024;
        private const string SlugFavoritos = "tus-me-gusta";

        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;

        private static readonly HashSet<string> Extensiones =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".wav", ".ogg", ".m4a"
            };

        // Playlists de referencia que existen en HomeController.
        private static readonly Dictionary<string, string> Referencias =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["kiss-me-kiss-me-kiss-me-again"] =
                    "KISS ME KISS ME KISS ME AGAIN",
                ["trap-cano"] = "Trap Cano",
                ["hits-de-internet"] = "Hits de Internet",
                ["grand-theft-auto-official-playlist"] =
                    "Grand Theft Auto Official Playlist",
                ["musica-para-programar"] = "Música para programar",
                ["fiesta"] = "Fiesta!!!",
                ["this-is-astrix"] = "This Is Astrix",
                ["tus-me-gusta"] = "Tus me gusta",
                ["cano"] = "CANO",
                ["trap-bastardo"] = "Trap Bastardo",
                ["para-jugar-tryhard"] = "Para jugar Tryhard 😎",
                ["ia-trap"] = "I.A. Trap",
                ["gym"] = "GYM",
                ["blood-oceans-how-many"] =
                    "Blood Oceans (How Many?)",
                ["trippy-cano"] = "Trippy Cano"
            };

        public BibliotecaController(
            ApplicationDbContext db,
            IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        private string UsuarioId()
        {
            return User.FindFirstValue(
                ClaimTypes.NameIdentifier) ?? "";
        }

        private string CarpetaAudios()
        {
            return Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "audio");
        }

        private string RutaAudio(string archivo)
        {
            return Path.Combine(CarpetaAudios(), archivo);
        }

        private static bool SlugValido(string? slug)
        {
            return !string.IsNullOrWhiteSpace(slug) &&
                Regex.IsMatch(
                    slug,
                    @"^[a-z0-9][a-z0-9-]{0,119}$");
        }

        private static string CrearSlug(string nombre)
        {
            var caracteres = nombre
                .Normalize(NormalizationForm.FormD)
                .Where(c =>
                    CharUnicodeInfo.GetUnicodeCategory(c)
                    != UnicodeCategory.NonSpacingMark)
                .Select(c =>
                    c <= 127 && char.IsLetterOrDigit(c)
                        ? char.ToLowerInvariant(c)
                        : '-');

            string slug = Regex.Replace(
                new string(caracteres.ToArray()),
                "-+",
                "-").Trim('-');

            if (slug.Length == 0)
                slug = "playlist";

            if (slug.Length > 55)
                slug = slug[..55].TrimEnd('-');

            return "mi-" + slug + "-" +
                Guid.NewGuid().ToString("N")[..8];
        }

        private static string TipoContenido(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                _ => "application/octet-stream"
            };
        }

        private static async Task<bool> FirmaAudioValida(
            IFormFile archivo,
            string extension)
        {
            byte[] cabecera = new byte[12];

            using var stream = archivo.OpenReadStream();

            int leidos = await stream.ReadAsync(
                cabecera.AsMemory(0, cabecera.Length));

            if (leidos < 8)
                return false;

            return extension.ToLowerInvariant() switch
            {
                ".mp3" =>
                    (cabecera[0] == 'I' &&
                     cabecera[1] == 'D' &&
                     cabecera[2] == '3') ||
                    (cabecera[0] == 0xFF &&
                     (cabecera[1] & 0xE0) == 0xE0),

                ".wav" =>
                    leidos >= 12 &&
                    Encoding.ASCII.GetString(
                        cabecera, 0, 4) == "RIFF" &&
                    Encoding.ASCII.GetString(
                        cabecera, 8, 4) == "WAVE",

                ".ogg" =>
                    Encoding.ASCII.GetString(
                        cabecera, 0, 4) == "OggS",

                ".m4a" =>
                    Encoding.ASCII.GetString(
                        cabecera, 4, 4) == "ftyp",

                _ => false
            };
        }

        private async Task<MusicTrack?> CancionPropia(Guid id)
        {
            string usuario = UsuarioId();

            return await _db.MusicTracks
                .Include(t => t.Playlist)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.Playlist.OwnerId == usuario);
        }

        // ================================================
        // PLAYLISTS PERSONALES
        // ================================================

        [HttpGet]
        public async Task<IActionResult> Playlist(string slug)
        {
            if (!SlugValido(slug))
                return NotFound();

            string usuario = UsuarioId();

            var lista = await _db.MusicPlaylists
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug &&
                    p.IsCustom);

            if (lista == null)
                return NotFound();

            var modelo = new PlaylistDetailViewModel
            {
                Slug = lista.Slug,
                Name = lista.Name,
                Type = "Playlist privada",
                Owner = User.Identity?.Name?.Split('@')[0]
                    ?? "Usuario",
                Cover = string.IsNullOrEmpty(lista.CoverFileName)
                    ? "/images/referencias/rapido-liked.jpg"
                    : Url.Action(nameof(Portada), "Biblioteca", new { slug = lista.Slug })
                      ?? "/images/referencias/rapido-liked.jpg",
                BannerColor = "#49358c",
                Description = string.IsNullOrWhiteSpace(lista.Description)
                    ? "Tu playlist personal."
                    : lista.Description,
                SavesText = "",
                TrackCountText = "",
                DurationText = "",
                Tracks = new List<PlaylistTrackViewModel>(),
                Recommendations =
                    new List<PlaylistTrackViewModel>()
            };

            return View(
                "~/Views/Home/Playlist.cshtml",
                modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearLista(
            [FromForm] string nombre)
        {
            nombre = (nombre ?? "").Trim();

            if (nombre.Length < 2 || nombre.Length > 100)
            {
                return BadRequest(new
                {
                    error = "El nombre debe tener entre 2 y 100 caracteres."
                });
            }

            var lista = new MusicPlaylist
            {
                OwnerId = UsuarioId(),
                Name = nombre,
                Slug = CrearSlug(nombre),
                IsCustom = true
            };

            _db.MusicPlaylists.Add(lista);
            await _db.SaveChangesAsync();

            return Json(new
            {
                ok = true,
                slug = lista.Slug,
                url = Url.Action(
                    nameof(Playlist),
                    "Biblioteca",
                    new { slug = lista.Slug })
            });
        }

        [HttpGet]
        public async Task<IActionResult> MisListas()
        {
            string usuario = UsuarioId();

            var listas = await _db.MusicPlaylists
                .AsNoTracking()
                .Where(p =>
                    p.OwnerId == usuario &&
                    p.IsCustom)
                .OrderByDescending(p => p.CreatedAtUtc)
                .Select(p => new
                {
                    p.Name,
                    p.Slug,
                    p.CoverFileName
                })
                .ToListAsync();

            return Json(listas.Select(p => new
            {
                name = p.Name,
                slug = p.Slug,
                coverUrl = string.IsNullOrEmpty(p.CoverFileName)
                    ? null : Url.Action(nameof(Portada), "Biblioteca", new { slug = p.Slug }),
                url = Url.Action(
                    nameof(Playlist),
                    "Biblioteca",
                    new { slug = p.Slug })
            }));
        }

        // ================================================
        // LISTAR CANCIONES: ORIGINALES + COMPARTIDAS
        // ================================================

        [HttpGet]
        public async Task<IActionResult> Canciones(string slug)
        {
            if (!SlugValido(slug))
            {
                return BadRequest(new
                {
                    error = "Playlist inválida."
                });
            }

            string usuario = UsuarioId();

            var lista = await _db.MusicPlaylists
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug);

            if (lista == null)
            {
                return Json(new
                {
                    exists = false,
                    isCustom = false,
                    tracks = Array.Empty<object>()
                });
            }

            var originales = await _db.MusicTracks
                .AsNoTracking()
                .Where(t => t.MusicPlaylistId == lista.Id)
                .ToListAsync();

            var compartidas = await _db.MusicPlaylistTracks
                .AsNoTracking()
                .Include(v => v.Track)
                .Where(v => v.MusicPlaylistId == lista.Id)
                .ToListAsync();

            var todas = originales
                .Select(t => (
                    Track: t,
                    Fecha: t.AddedAtUtc,
                    Posicion: t.Position))
                .Concat(compartidas.Select(v => (
                    Track: v.Track,
                    Fecha: v.AddedAtUtc,
                    Posicion: v.Position)))
                .GroupBy(x => x.Track.Id)
                .Select(g => g.First())
                .OrderBy(x => x.Posicion ?? int.MaxValue)
                .ThenBy(x => x.Fecha)
                .ThenBy(x => x.Track.Id)
                .ToList();

            var favoritos = await _db.MusicPlaylists
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == SlugFavoritos);

            var idsFavoritos = new HashSet<Guid>();

            if (favoritos != null)
            {
                var principales = await _db.MusicTracks
                    .AsNoTracking()
                    .Where(t =>
                        t.MusicPlaylistId == favoritos.Id)
                    .Select(t => t.Id)
                    .ToListAsync();

                var vinculadas = await _db.MusicPlaylistTracks
                    .AsNoTracking()
                    .Where(v =>
                        v.MusicPlaylistId == favoritos.Id)
                    .Select(v => v.MusicTrackId)
                    .ToListAsync();

                idsFavoritos.UnionWith(principales);
                idsFavoritos.UnionWith(vinculadas);
            }

            return Json(new
            {
                exists = true,
                isCustom = lista.IsCustom,
                canEdit = lista.Slug != SlugFavoritos,
                name = lista.Name,
                description = lista.Description,
                coverUrl = string.IsNullOrEmpty(lista.CoverFileName)
                    ? null
                    : Url.Action(nameof(Portada), "Biblioteca", new { slug }),
                tracks = todas.Select(item => new
                {
                    id = item.Track.Id,
                    title = item.Track.Title,
                    artist = item.Track.Artist,
                    album = item.Track.Album,
                    url = Url.Action(
                        nameof(Audio),
                        "Biblioteca",
                        new { id = item.Track.Id }),
                    addedDate =
                        item.Fecha.ToString("dd/MM/yyyy"),
                    sizeBytes = item.Track.SizeBytes,
                    isLiked =
                        idsFavoritos.Contains(item.Track.Id)
                })
            });
        }

        // ================================================
        // BLOQUE B: METADATOS, PORTADAS Y ORDEN
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarDetalles(
            [FromForm] string slug, [FromForm] string nombre,
            [FromForm] string descripcion)
        {
            if (!SlugValido(slug) || slug == SlugFavoritos) return BadRequest(new { error = "Lista inválida." });
            nombre = (nombre ?? "").Trim();
            descripcion = (descripcion ?? "").Trim();
            if (nombre.Length < 2 || nombre.Length > 100 || descripcion.Length > 600)
                return BadRequest(new { error = "Nombre: 2 a 100 caracteres. Descripción: máximo 600." });

            var lista = await _db.MusicPlaylists.FirstOrDefaultAsync(p =>
                p.OwnerId == UsuarioId() && p.Slug == slug);
            if (lista == null) return NotFound(new { error = "No puedes editar esta playlist." });

            // Solo el dueño de este registro modifica sus metadatos.
            lista.Name = nombre;
            lista.Description = descripcion;
            await _db.SaveChangesAsync();
            return Json(new { ok = true, name = lista.Name, description = lista.Description });
        }

        private string CarpetaPortadas() => Path.Combine(
            _environment.ContentRootPath, "App_Data", "covers");

        [HttpGet]
        public async Task<IActionResult> Portada(string slug)
        {
            if (!SlugValido(slug)) return NotFound();
            var lista = await _db.MusicPlaylists.AsNoTracking()
                .FirstOrDefaultAsync(p => p.OwnerId == UsuarioId() && p.Slug == slug);
            if (lista == null || string.IsNullOrEmpty(lista.CoverFileName)) return NotFound();
            string nombre = Path.GetFileName(lista.CoverFileName);
            if (nombre != lista.CoverFileName) return NotFound();
            string ruta = Path.Combine(CarpetaPortadas(), nombre);
            if (!System.IO.File.Exists(ruta)) return NotFound();
            string tipo = Path.GetExtension(nombre).ToLowerInvariant() switch {
                ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg"
            };
            Response.Headers.CacheControl = "private, no-store";
            return PhysicalFile(ruta, tipo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> SubirPortada(
            [FromForm] string slug, [FromForm] IFormFile archivo)
        {
            if (!SlugValido(slug) || slug == SlugFavoritos) return BadRequest(new { error = "Lista inválida." });
            var lista = await _db.MusicPlaylists.FirstOrDefaultAsync(p =>
                p.OwnerId == UsuarioId() && p.Slug == slug);
            if (lista == null) return NotFound(new { error = "Esta playlist no se puede editar." });
            if (archivo == null || archivo.Length == 0 || archivo.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "Selecciona una imagen de hasta 5 MB." });

            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                return BadRequest(new { error = "La portada debe ser JPG, PNG o WEBP." });
            byte[] firma = new byte[12];
            await using (var entrada = archivo.OpenReadStream())
                if (await entrada.ReadAsync(firma.AsMemory(0, 12)) < 12)
                    return BadRequest(new { error = "Imagen inválida." });
            bool valida = ext switch {
                ".jpg" or ".jpeg" => firma[0] == 0xFF && firma[1] == 0xD8 && firma[2] == 0xFF,
                ".png" => firma.Take(8).SequenceEqual(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}),
                ".webp" => Encoding.ASCII.GetString(firma, 0, 4) == "RIFF"
                           && Encoding.ASCII.GetString(firma, 8, 4) == "WEBP",
                _ => false
            };
            if (!valida) return BadRequest(new { error = "El archivo no tiene una firma de imagen válida." });
            Directory.CreateDirectory(CarpetaPortadas());
            var nuevo = Guid.NewGuid().ToString("N") + (ext == ".jpeg" ? ".jpg" : ext);
            var ruta = Path.Combine(CarpetaPortadas(), nuevo);
            var anterior = lista.CoverFileName;
            try {
                await using (var destino = new FileStream(ruta, FileMode.CreateNew))
                    await archivo.CopyToAsync(destino);
                lista.CoverFileName = nuevo;
                await _db.SaveChangesAsync();
            } catch {
                if (System.IO.File.Exists(ruta)) System.IO.File.Delete(ruta);
                throw;
            }
            if (!string.IsNullOrEmpty(anterior)) {
                var vieja = Path.Combine(CarpetaPortadas(), Path.GetFileName(anterior));
                if (System.IO.File.Exists(vieja)) System.IO.File.Delete(vieja);
            }
            return Json(new { ok = true,
                coverUrl = Url.Action(nameof(Portada), "Biblioteca", new { slug }) });
        }

        public class ReorderPlaylistRequest {
            public string Slug { get; set; } = "";
            public List<Guid> Ids { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarOrden([FromBody] ReorderPlaylistRequest solicitud)
        {
            if (solicitud == null || !SlugValido(solicitud.Slug) || solicitud.Ids == null ||
                solicitud.Ids.Count != solicitud.Ids.Distinct().Count())
                return BadRequest(new { error = "El orden indicado no es válido." });
            var lista = await _db.MusicPlaylists.FirstOrDefaultAsync(p =>
                p.OwnerId == UsuarioId() && p.Slug == solicitud.Slug);
            if (lista == null || lista.Slug == SlugFavoritos) return NotFound();

            var originales = await _db.MusicTracks
                .Where(t => t.MusicPlaylistId == lista.Id).ToListAsync();
            var compartidas = await _db.MusicPlaylistTracks
                .Where(v => v.MusicPlaylistId == lista.Id).ToListAsync();
            var idsGuardados = originales.Select(t => t.Id)
                .Concat(compartidas.Select(v => v.MusicTrackId)).ToHashSet();
            if (idsGuardados.Count != solicitud.Ids.Count ||
                solicitud.Ids.Any(id => !idsGuardados.Contains(id)))
                return BadRequest(new { error = "Actualiza la playlist antes de guardar el orden." });

            for (int i = 0; i < solicitud.Ids.Count; i++) {
                var id = solicitud.Ids[i];
                var original = originales.FirstOrDefault(t => t.Id == id);
                if (original != null) original.Position = i;
                else {
                    var vinculo = compartidas.First(v => v.MusicTrackId == id);
                    vinculo.Position = i;
                }
            }
            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        // ================================================
        // SUBIR AUDIO NUEVO
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(40 * 1024 * 1024)]
        public async Task<IActionResult> Subir(
            [FromForm] string slug,
            [FromForm] string nombre,
            [FromForm] IFormFile archivo)
        {
            if (!SlugValido(slug))
            {
                return BadRequest(new
                {
                    error = "La playlist no es válida."
                });
            }

            // Los favoritos se alimentan desde canciones
            // existentes para evitar archivos huérfanos.
            if (slug == SlugFavoritos)
            {
                return BadRequest(new
                {
                    error =
                        "Para guardar favoritos, usa el corazón de una canción."
                });
            }

            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest(new
                {
                    error = "Selecciona un archivo de audio."
                });
            }

            if (archivo.Length > MaxAudioBytes)
            {
                return BadRequest(new
                {
                    error = "El límite por archivo es de 30 MB."
                });
            }

            string original = Path.GetFileName(archivo.FileName);
            string extension = Path.GetExtension(original)
                .ToLowerInvariant();

            if (!Extensiones.Contains(extension) ||
                !await FirmaAudioValida(archivo, extension))
            {
                return BadRequest(new
                {
                    error = "Usa un archivo MP3, WAV, OGG o M4A válido."
                });
            }

            string titulo =
                Path.GetFileNameWithoutExtension(original).Trim();

            if (titulo.Length == 0)
                titulo = "Canción sin título";

            if (titulo.Length > 250)
                titulo = titulo[..250];

            // Separa automáticamente «Artista - Título» si el nombre lo permite.
            string artistaDetectado = "Artista desconocido";
            int separador = titulo.IndexOf(" - ", StringComparison.Ordinal);
            if (separador > 0 && separador < 150 && separador + 3 < titulo.Length)
            {
                artistaDetectado = titulo[..separador].Trim();
                titulo = titulo[(separador + 3)..].Trim();
            }

            string usuario = UsuarioId();

            var lista = await _db.MusicPlaylists
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug);

            if (lista == null)
            {
                if (!Referencias.TryGetValue(
                    slug,
                    out string? nombreReferencia))
                {
                    return NotFound(new
                    {
                        error = "La playlist no existe."
                    });
                }

                lista = new MusicPlaylist
                {
                    OwnerId = usuario,
                    Slug = slug,
                    Name = nombreReferencia,
                    IsCustom = false
                };

                _db.MusicPlaylists.Add(lista);
            }

            Directory.CreateDirectory(CarpetaAudios());

            string nombreInterno =
                Guid.NewGuid().ToString("N") + extension;

            string ruta = RutaAudio(nombreInterno);

            try
            {
                await using (var destino = new FileStream(
                    ruta,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await archivo.CopyToAsync(destino);
                }

                var cancion = new MusicTrack
                {
                    Playlist = lista,
                    Title = titulo,
                    Artist = artistaDetectado,
                    Album = "Sin álbum",
                    StoredFileName = nombreInterno,
                    ContentType = TipoContenido(extension),
                    SizeBytes = archivo.Length
                };

                _db.MusicTracks.Add(cancion);
                await _db.SaveChangesAsync();

                return Json(new
                {
                    ok = true,
                    id = cancion.Id,
                    title = cancion.Title
                });
            }
            catch
            {
                if (System.IO.File.Exists(ruta))
                    System.IO.File.Delete(ruta);

                throw;
            }
        }

        // ================================================
        // AÑADIR UNA CANCIÓN EXISTENTE A OTRA PLAYLIST
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarALista(
            [FromForm] Guid id,
            [FromForm] string slug)
        {
            if (!SlugValido(slug))
            {
                return BadRequest(new
                {
                    error = "Playlist inválida."
                });
            }

            var cancion = await CancionPropia(id);

            if (cancion == null)
                return NotFound(new
                {
                    error = "No encontramos esa canción."
                });

            string usuario = UsuarioId();

            var destino = await _db.MusicPlaylists
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug);

            if (destino == null)
            {
                if (!Referencias.TryGetValue(
                    slug,
                    out string? nombreReferencia))
                {
                    return NotFound(new
                    {
                        error = "La playlist de destino no existe."
                    });
                }

                destino = new MusicPlaylist
                {
                    OwnerId = usuario,
                    Slug = slug,
                    Name = nombreReferencia,
                    IsCustom = false
                };

                _db.MusicPlaylists.Add(destino);
            }

            // La canción ya pertenece directamente a esta lista.
            if (cancion.MusicPlaylistId == destino.Id)
            {
                return Json(new
                {
                    ok = true,
                    already = true,
                    message = "La canción ya está en esta playlist."
                });
            }

            bool existe = await _db.MusicPlaylistTracks
                .AnyAsync(v =>
                    v.MusicPlaylistId == destino.Id &&
                    v.MusicTrackId == cancion.Id);

            if (existe)
            {
                return Json(new
                {
                    ok = true,
                    already = true,
                    message = "La canción ya está en esta playlist."
                });
            }

            _db.MusicPlaylistTracks.Add(
                new MusicPlaylistTrack
                {
                    Playlist = destino,
                    Track = cancion
                });

            await _db.SaveChangesAsync();

            return Json(new
            {
                ok = true,
                already = false,
                message = "Canción añadida a " + destino.Name
            });
        }

        // ================================================
        // QUITAR UNA CANCIÓN DE UNA PLAYLIST
        // SIN BORRAR COPIAS VINCULADAS
        // ================================================

        private async Task<IActionResult> QuitarVinculo(
            MusicTrack cancion,
            MusicPlaylist lista)
        {
            string? archivoParaEliminar = null;

            if (cancion.MusicPlaylistId == lista.Id)
            {
                // Si otras listas la utilizan, transferimos
                // la relación principal a una de ellas.
                var otroVinculo = await _db.MusicPlaylistTracks
                    .FirstOrDefaultAsync(v =>
                        v.MusicTrackId == cancion.Id &&
                        v.MusicPlaylistId != lista.Id);

                if (otroVinculo != null)
                {
                    cancion.MusicPlaylistId =
                        otroVinculo.MusicPlaylistId;

                    _db.MusicPlaylistTracks.Remove(otroVinculo);
                }
                else
                {
                    // No hay ninguna otra lista que la utilice.
                    archivoParaEliminar = cancion.StoredFileName;
                    _db.MusicTracks.Remove(cancion);
                }
            }
            else
            {
                var vinculo = await _db.MusicPlaylistTracks
                    .FirstOrDefaultAsync(v =>
                        v.MusicPlaylistId == lista.Id &&
                        v.MusicTrackId == cancion.Id);

                if (vinculo == null)
                {
                    return NotFound(new
                    {
                        error = "La canción no pertenece a esta playlist."
                    });
                }

                _db.MusicPlaylistTracks.Remove(vinculo);
            }

            await _db.SaveChangesAsync();

            if (archivoParaEliminar != null)
            {
                string ruta = RutaAudio(archivoParaEliminar);

                if (System.IO.File.Exists(ruta))
                    System.IO.File.Delete(ruta);
            }

            return Json(new { ok = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarDeLista(
            [FromForm] Guid id,
            [FromForm] string slug)
        {
            var cancion = await CancionPropia(id);

            if (cancion == null)
                return NotFound();

            string usuario = UsuarioId();

            var lista = await _db.MusicPlaylists
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug);

            if (lista == null)
                return NotFound();

            return await QuitarVinculo(cancion, lista);
        }

        // Mantiene el endpoint que ya utilizaba SpotifyNet.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCancion(
            [FromForm] Guid id,
            [FromForm] string? slug)
        {
            var cancion = await CancionPropia(id);

            if (cancion == null)
                return NotFound();

            slug ??= cancion.Playlist.Slug;

            string usuario = UsuarioId();

            var lista = await _db.MusicPlaylists
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug);

            if (lista == null)
                return NotFound();

            return await QuitarVinculo(cancion, lista);
        }

        // ================================================
        // REPRODUCCIÓN PROTEGIDA
        // ================================================

        [HttpGet]
        public async Task<IActionResult> Audio(Guid id)
        {
            // El propietario siempre tiene acceso. Un colaborador o un usuario
            // que ve una playlist pública solo puede escuchar los temas incluidos
            // en esa playlist. El archivo nunca se expone desde wwwroot.
            var cancion = await _db.MusicTracks.AsNoTracking()
                .Include(t => t.Playlist)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (cancion == null) return NotFound();

            var usuario = UsuarioId();
            bool puedeLeer = cancion.Playlist.OwnerId == usuario ||
                cancion.Playlist.IsPublic ||
                await _db.MusicPlaylistCollaborators.AnyAsync(c =>
                    c.MusicPlaylistId == cancion.MusicPlaylistId && c.UserId == usuario);
            if (!puedeLeer)
            {
                puedeLeer = await _db.MusicPlaylistTracks.AnyAsync(v =>
                    v.MusicTrackId == id &&
                    (v.Playlist.OwnerId == usuario || v.Playlist.IsPublic ||
                     v.Playlist.Collaborators.Any(c => c.UserId == usuario)));
            }
            if (!puedeLeer) return NotFound();

            string ruta = RutaAudio(cancion.StoredFileName);

            if (!System.IO.File.Exists(ruta))
                return NotFound();

            Response.Headers.CacheControl = "private, no-store";

            return PhysicalFile(
                ruta,
                cancion.ContentType,
                enableRangeProcessing: true);
        }

        // ================================================
        // RENOMBRAR PLAYLIST PERSONAL
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenombrarLista(
            [FromForm] string slug,
            [FromForm] string nombre)
        {
            nombre = (nombre ?? "").Trim();

            if (nombre.Length < 2 || nombre.Length > 100)
            {
                return BadRequest(new
                {
                    error = "El nombre debe tener entre 2 y 100 caracteres."
                });
            }

            string usuario = UsuarioId();

            var lista = await _db.MusicPlaylists
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug &&
                    p.IsCustom);

            if (lista == null)
                return NotFound();

            lista.Name = nombre;
            await _db.SaveChangesAsync();

            return Json(new { ok = true });
        }

        // ================================================
        // ELIMINAR PLAYLIST PERSONAL SIN PERDER
        // CANCIONES COMPARTIDAS CON OTRAS LISTAS
        // ================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarLista(
            [FromForm] string slug)
        {
            string usuario = UsuarioId();

            var lista = await _db.MusicPlaylists
                .FirstOrDefaultAsync(p =>
                    p.OwnerId == usuario &&
                    p.Slug == slug &&
                    p.IsCustom);

            if (lista == null)
                return NotFound();

            var cancionesOriginales = await _db.MusicTracks
                .Include(t => t.SharedIn)
                .Where(t => t.MusicPlaylistId == lista.Id)
                .ToListAsync();

            var archivosParaEliminar = new List<string>();

            foreach (var cancion in cancionesOriginales)
            {
                var alternativa = cancion.SharedIn
                    .FirstOrDefault(v =>
                        v.MusicPlaylistId != lista.Id);

                if (alternativa != null)
                {
                    // El MP3 continúa existiendo en la
                    // otra playlist, sin duplicarse.
                    cancion.MusicPlaylistId =
                        alternativa.MusicPlaylistId;

                    _db.MusicPlaylistTracks.Remove(alternativa);
                }
                else
                {
                    archivosParaEliminar.Add(
                        cancion.StoredFileName);

                    _db.MusicTracks.Remove(cancion);
                }
            }

            var portadaAlEliminar = lista.CoverFileName;
            _db.MusicPlaylists.Remove(lista);

            await _db.SaveChangesAsync();

            foreach (string archivo in archivosParaEliminar)
            {
                string ruta = RutaAudio(archivo);

                if (System.IO.File.Exists(ruta))
                    System.IO.File.Delete(ruta);
            }

            if (!string.IsNullOrEmpty(portadaAlEliminar)) {
                var rutaPortada = Path.Combine(CarpetaPortadas(), Path.GetFileName(portadaAlEliminar));
                if (System.IO.File.Exists(rutaPortada)) System.IO.File.Delete(rutaPortada);
            }

            return Json(new
            {
                ok = true,
                url = Url.Action("Index", "Home")
            });
        }
    }
}
