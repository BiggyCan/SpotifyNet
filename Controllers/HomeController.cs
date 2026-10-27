
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpotifyNet.Models;

namespace SpotifyNet.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return View("InicioUsuario");
            }

            return View();
        }

        [Authorize]
        public IActionResult InicioUsuario()
        {
            return View();
        }

        [Authorize]
        public IActionResult Playlist(
            string slug = "kiss-me-kiss-me-kiss-me-again")
        {
            // Bloque G: favoritos reales guardados por usuario, no canciones ficticias.
            if (string.Equals(slug, "tus-me-gusta", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Favoritos");
            }

            var playlist = GetPlaylists().FirstOrDefault(p =>
                string.Equals(
                    p.Slug,
                    slug,
                    StringComparison.OrdinalIgnoreCase));

            if (playlist == null)
                return NotFound();

            return View(playlist);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                    ?? HttpContext.TraceIdentifier
            });
        }

        private static string Cover(string filename)
        {
            return "/images/referencias/" + filename;
        }

        private static PlaylistTrackViewModel Song(
            int number,
            string cover,
            string title,
            string artist,
            string album,
            string addedDate,
            string duration)
        {
            return new PlaylistTrackViewModel
            {
                Number = number,
                Cover = cover,
                Title = title,
                Artist = artist,
                Album = album,
                AddedDate = addedDate,
                Duration = duration
            };
        }

        private static PlaylistDetailViewModel DemoPlaylist(
            string slug,
            string name,
            string owner,
            string coverFile,
            string color,
            params string[] sampleTitles)
        {
            string cover = Cover(coverFile);

            if (sampleTitles.Length == 0)
            {
                sampleTitles = new[]
                {
                    "Pista de demostración 1",
                    "Pista de demostración 2",
                    "Pista de demostración 3",
                    "Pista de demostración 4"
                };
            }

            var tracks = sampleTitles.Select((title, index) =>
                Song(
                    index + 1,
                    cover,
                    title,
                    "SpotifyNet · Demostración",
                    name,
                    "—",
                    "—"
                )
            ).ToList();

            return new PlaylistDetailViewModel
            {
                Slug = slug,
                Name = name,
                Type = "Playlist de demostración",
                Owner = owner,
                Cover = cover,
                BannerColor = color,
                Description = "Lista de ejemplo de SpotifyNet.",
                SavesText = "",
                TrackCountText =
                    tracks.Count + " canciones de muestra",
                DurationText = "",
                Tracks = tracks,
                Recommendations = new List<PlaylistTrackViewModel>()
            };
        }

        private static PlaylistDetailViewModel KissPlaylist()
        {
            string cover = Cover("rapido-kiss.jpg");

            var titles = new[]
            {
                new[] {"Kiss Me Again - Radio Edit", "Roy Bee", "Kiss Me Again", "2:44"},
                new[] {"Mtc", "S3RL", "MTC", "4:20"},
                new[] {"Echo", "Crusher-P", "Echo", "3:50"},
                new[] {"Oh Oh Oh Sexy Vampire - JUSTINB Remix", "Disko Warp, Fright Ranger, Justinb", "I Love Disko Warp Singles Collection, Vol. 2", "3:29"},
                new[] {"Caramelldansen", "Caramella Girls", "Supergott (Speedy-Mixes)", "2:55"},
                new[] {"Everytime We Touch - Nightcore & KYANU Edit", "Cascada, KYANU", "Nightcore", "2:28"},
                new[] {"Discord", "The Living Tombstone, Eurobeat Brony", "Discord", "3:13"},
                new[] {"Ring Dinge Ding", "DJ Splash", "Ring Dinge Ding", "3:01"},
                new[] {"cybergirl", "2006WR", "cybergirl", "1:19"},
                new[] {"The Sailor Song", "Toy-Box", "FanTastic", "3:05"},
                new[] {"More & More", "dreamsoda", "More & More", "1:24"},
                new[] {"I Don't Know - Radio Mix", "Erika", "Erika (Essential Tunes)", "3:19"},
                new[] {"6 Little Eggs - Nightcore", "Enlighten", "6 Little Eggs", "3:22"},
                new[] {"noodles", "xaptiox", "noodles", "3:16"}
            };

            var tracks = new List<PlaylistTrackViewModel>();

            for (int i = 0; i < titles.Length; i++)
            {
                string songCover = i switch
                {
                    0 => Cover("rapido-kiss.jpg"),
                    4 => Cover("playlist-tryhard.jpg"),
                    6 => Cover("rapido-fiesta.jpg"),
                    _ => Cover("rapido-hits.jpg")
                };

                tracks.Add(Song(
                    i + 1,
                    songCover,
                    titles[i][0],
                    titles[i][1],
                    titles[i][2],
                    "29 nov 2022",
                    titles[i][3]
                ));
            }

            var recommendations = new List<PlaylistTrackViewModel>
            {
                Song(1, cover, "Kiss Me Again - Club Mix",
                    "Roy Bee", "Kiss Me Again", "", "5:27"),

                Song(2, Cover("rapido-hits.jpg"),
                    "Bass Slut - Original Mix",
                    "S3RL", "Bass Slut", "", "5:29"),

                Song(3, Cover("rapido-fiesta.jpg"),
                    "How Do You Do - Nightcore",
                    "Jaegershu", "Nightcore", "", "3:04"),

                Song(4, Cover("rapido-astrix.jpg"),
                    "Rockefeller Street",
                    "Getter Jaani", "Rockefeller Street", "", "2:35"),

                Song(5, Cover("rapido-kiss.jpg"),
                    "Catchit", "S3RL", "Catchit", "", "3:31")
            };

            return new PlaylistDetailViewModel
            {
                Slug = "kiss-me-kiss-me-kiss-me-again",
                Name = "KISS ME KISS ME KISS ME AGAIN",
                Type = "Playlist pública · Referencia",
                Owner = "eliza",
                Cover = cover,
                BannerColor = "#97064e",
                Description = "",
                SavesText = "5.085 veces guardada en la referencia",
                TrackCountText = "14 canciones de muestra",
                DurationText = "",
                Tracks = tracks,
                Recommendations = recommendations
            };
        }

        private static List<PlaylistDetailViewModel> GetPlaylists()
        {
            return new List<PlaylistDetailViewModel>
            {
                KissPlaylist(),

                // Tarjetas de la parte superior
                DemoPlaylist(
                    "trap-cano",
                    "Trap Cano",
                    "SpotifyNet",
                    "rapido-trap.jpg",
                    "#873e42"),

                DemoPlaylist(
                    "hits-de-internet",
                    "Hits de Internet",
                    "SpotifyNet",
                    "rapido-hits.jpg",
                    "#b62691"),

                DemoPlaylist(
                    "grand-theft-auto-official-playlist",
                    "Grand Theft Auto Official Playlist",
                    "SpotifyNet",
                    "rapido-gta.jpg",
                    "#583d76"),

                DemoPlaylist(
                    "musica-para-programar",
                    "Música para programar",
                    "SpotifyNet",
                    "rapido-programar.jpg",
                    "#193969"),

                DemoPlaylist(
                    "fiesta",
                    "Fiesta!!!",
                    "SpotifyNet",
                    "rapido-fiesta.jpg",
                    "#195b95"),

                DemoPlaylist(
                    "this-is-astrix",
                    "This Is Astrix",
                    "SpotifyNet",
                    "rapido-astrix.jpg",
                    "#c65391"),

                DemoPlaylist(
                    "tus-me-gusta",
                    "Tus me gusta",
                    "SpotifyNet",
                    "rapido-liked.jpg",
                    "#6543a8"),

                // Playlists de la colección central
                DemoPlaylist(
                    "cano",
                    "CANO",
                    "Cristhian Cano",
                    "playlist-cano.jpg",
                    "#354a8c",
                    "Dame un minuto",
                    "Noches largas",
                    "Ciudad oscura",
                    "Mirada fría",
                    "Modo fantasma"),

                DemoPlaylist(
                    "trap-bastardo",
                    "Trap Bastardo",
                    "Cristhian Cano",
                    "playlist-trap-bastardo.jpg",
                    "#512c2e",
                    "Barrio frío",
                    "Fuego lento",
                    "Nada gratis",
                    "Código negro"),

                DemoPlaylist(
                    "para-jugar-tryhard",
                    "Para jugar Tryhard 😎",
                    "Cristhian Cano",
                    "playlist-tryhard.jpg",
                    "#76542c",
                    "Focus Mode",
                    "No Scope",
                    "Rush B",
                    "Clutch"),

                DemoPlaylist(
                    "ia-trap",
                    "I.A. Trap",
                    "Cristhian Cano",
                    "playlist-ia-trap.jpg",
                    "#5f662f",
                    "Algoritmo",
                    "Datos",
                    "Neón Digital",
                    "Prompt oscuro"),

                DemoPlaylist(
                    "gym",
                    "GYM",
                    "Cristhian Cano",
                    "playlist-gym.jpg",
                    "#790d1b",
                    "Warm Up",
                    "Peso muerto",
                    "Sin descanso",
                    "Repetición final"),

                DemoPlaylist(
                    "blood-oceans-how-many",
                    "Blood Oceans (How Many?)",
                    "Cristhian Cano",
                    "playlist-blood.jpg",
                    "#640f1b",
                    "How Many?",
                    "Dark Tide",
                    "Red Horizon",
                    "Deep Water"),

                DemoPlaylist(
                    "trippy-cano",
                    "Trippy Cano",
                    "Cristhian Cano",
                    "playlist-trippy.jpg",
                    "#563c7d",
                    "Sueño raro",
                    "Cristales",
                    "Luces lilas",
                    "Portal")
            };
        }
    }
}
