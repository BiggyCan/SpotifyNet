using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Data;
using SpotifyNet.Models;

namespace SpotifyNet.Controllers
{
    // Todas las operaciones sociales requieren autenticación.
    [Authorize]
    public class SocialController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _users;
        private readonly IWebHostEnvironment _environment;

        public SocialController(ApplicationDbContext db, UserManager<IdentityUser> users,
            IWebHostEnvironment environment)
        {
            _db = db;
            _users = users;
            _environment = environment;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

        private static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

        private async Task<string> DisplayName(string id)
        {
            var stored = await _db.UserClaims.AsNoTracking().Where(c =>
                    c.UserId == id && c.ClaimType == "SpotifyNet.DisplayName")
                .Select(c => c.ClaimValue).FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(stored)) return stored;
            var account = await _users.FindByIdAsync(id);
            return account?.UserName?.Split('@')[0] ?? "Usuario";
        }

        private async Task<MusicPlaylist?> FindOwned(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug) || slug.Length > 120) return null;
            return await _db.MusicPlaylists.FirstOrDefaultAsync(p =>
                p.OwnerId == UserId && p.Slug == slug && p.Slug != "tus-me-gusta");
        }

        private async Task<bool> CanRead(MusicPlaylist playlist)
        {
            if (playlist.OwnerId == UserId || playlist.IsPublic) return true;
            return await _db.MusicPlaylistCollaborators.AnyAsync(c =>
                c.MusicPlaylistId == playlist.Id && c.UserId == UserId);
        }

        [HttpGet]
        public async Task<IActionResult> Estado(string slug)
        {
            var playlist = await FindOwned(slug);
            if (playlist == null) return Json(new { found = false });
            var token = playlist.ShareToken;
            var members = await _db.MusicPlaylistCollaborators.AsNoTracking()
                .Where(c => c.MusicPlaylistId == playlist.Id).ToListAsync();
            var memberData = new System.Collections.Generic.List<object>();
            foreach (var member in members)
            {
                var account = await _users.FindByIdAsync(member.UserId);
                if (account != null)
                    memberData.Add(new { id = member.UserId, email = account.Email ?? "" });
            }
            return Json(new {
                found = true,
                isPublic = playlist.IsPublic,
                url = !string.IsNullOrEmpty(token)
                    ? Url.Action(nameof(Compartida), "Social", new { token }, Request.Scheme)
                    : (string?)null,
                members = memberData
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Visibilidad(string slug, bool publica)
        {
            var playlist = await FindOwned(slug);
            if (playlist == null) return NotFound(new { error = "Solo el propietario puede cambiar la privacidad." });
            playlist.ShareToken ??= NewToken();
            playlist.IsPublic = publica;
            await _db.SaveChangesAsync();
            return Json(new { ok = true, isPublic = playlist.IsPublic,
                url = Url.Action(nameof(Compartida), "Social", new { token = playlist.ShareToken }, Request.Scheme) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invitar(string slug, string email)
        {
            var playlist = await FindOwned(slug);
            if (playlist == null) return NotFound(new { error = "No eres propietario de esta playlist." });
            email = (email ?? "").Trim();
            if (email.Length > 256 || email.Length < 3)
                return BadRequest(new { error = "Escribe un correo válido." });
            var invitee = await _users.FindByEmailAsync(email);
            if (invitee == null) return NotFound(new { error = "La persona debe tener cuenta en SpotifyNet." });
            if (invitee.Id == UserId) return BadRequest(new { error = "Ya eres propietario de esta lista." });
            if (await _db.MusicPlaylistCollaborators.AnyAsync(c =>
                c.MusicPlaylistId == playlist.Id && c.UserId == invitee.Id))
                return BadRequest(new { error = "Este usuario ya es colaborador." });
            playlist.ShareToken ??= NewToken();
            _db.MusicPlaylistCollaborators.Add(new MusicPlaylistCollaborator {
                MusicPlaylistId = playlist.Id, UserId = invitee.Id });
            await _db.SaveChangesAsync();
            return Json(new { ok = true, email = invitee.Email,
                url = Url.Action(nameof(Compartida), "Social", new { token = playlist.ShareToken }, Request.Scheme) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarColaborador(string slug, string userId)
        {
            var playlist = await FindOwned(slug);
            if (playlist == null) return NotFound(new { error = "Solo el propietario administra colaboradores." });
            var member = await _db.MusicPlaylistCollaborators.FirstOrDefaultAsync(c =>
                c.MusicPlaylistId == playlist.Id && c.UserId == userId);
            if (member == null) return NotFound(new { error = "Colaborador no encontrado." });
            _db.MusicPlaylistCollaborators.Remove(member);
            await _db.SaveChangesAsync();
            return Json(new { ok = true });
        }

        [HttpGet]
        public async Task<IActionResult> MiEspacio()
        {
            var id = UserId;
            var lists = await _db.MusicPlaylists.AsNoTracking()
                .Where(p => p.OwnerId == id && p.Slug != "tus-me-gusta")
                .OrderBy(p => p.Name).ToListAsync();
            var ownerName = await DisplayName(id);
            var model = new SocialDashboardViewModel { UserId = id, DisplayName = ownerName };
            model.Owned = lists.Select(p => new SocialPlaylistSummary {
                Name = p.Name, Slug = p.Slug, Token = p.ShareToken ?? "",
                IsPublic = p.IsPublic, IsOwner = true, Owner = ownerName
            }).ToList();
            var membership = await _db.MusicPlaylistCollaborators.AsNoTracking()
                .Include(c => c.Playlist).Where(c => c.UserId == id).ToListAsync();
            foreach (var member in membership)
            {
                var list = member.Playlist;
                if (string.IsNullOrWhiteSpace(list.ShareToken)) continue;
                model.Collaborations.Add(new SocialPlaylistSummary {
                    Name = list.Name, Token = list.ShareToken, IsPublic = list.IsPublic,
                    Owner = await DisplayName(list.OwnerId)
                });
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Perfil(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || await _users.FindByIdAsync(id) == null)
                return NotFound();
            var profile = new SocialPublicProfileViewModel {
                UserId = id, DisplayName = await DisplayName(id),
                Bio = await _db.UserClaims.AsNoTracking()
                    .Where(c => c.UserId == id && c.ClaimType == "SpotifyNet.Bio")
                    .Select(c => c.ClaimValue).FirstOrDefaultAsync() ?? ""
            };
            profile.PublicPlaylists = await _db.MusicPlaylists.AsNoTracking()
                .Where(p => p.OwnerId == id && p.IsPublic && p.ShareToken != null)
                .OrderBy(p => p.Name)
                .Select(p => new SocialPlaylistSummary { Name = p.Name,
                    Token = p.ShareToken!, IsPublic = true })
                .ToListAsync();
            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Compartida(string token)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length != 48) return NotFound();
            var playlist = await _db.MusicPlaylists.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShareToken == token);
            if (playlist == null || !await CanRead(playlist)) return NotFound();
            var links = await _db.MusicPlaylistTracks.AsNoTracking()
                .Include(c => c.Track).Where(c => c.MusicPlaylistId == playlist.Id).ToListAsync();
            var originals = await _db.MusicTracks.AsNoTracking()
                .Where(t => t.MusicPlaylistId == playlist.Id).ToListAsync();
            var songs = originals.Select(t => (Track: t, Date: t.AddedAtUtc, Position: t.Position))
                .Concat(links.Select(l => (Track: l.Track, Date: l.AddedAtUtc, Position: l.Position)))
                .GroupBy(v => v.Track.Id).Select(g => g.First())
                .OrderBy(v => v.Position ?? int.MaxValue).ThenBy(v => v.Date)
                .Select(v => new SocialSongViewModel {
                    Id = v.Track.Id, Title = v.Track.Title, Artist = v.Track.Artist
                }).ToList();
            bool isOwner = playlist.OwnerId == UserId;
            bool isCollaborator = !isOwner && await _db.MusicPlaylistCollaborators.AnyAsync(c =>
                c.MusicPlaylistId == playlist.Id && c.UserId == UserId);
            var model = new SocialSharedViewModel {
                Token = token, Name = playlist.Name, Description = playlist.Description,
                OwnerName = await DisplayName(playlist.OwnerId), OwnerId = playlist.OwnerId,
                IsPublic = playlist.IsPublic, IsOwner = isOwner,
                CanContribute = isOwner || isCollaborator, Songs = songs
            };
            if (model.CanContribute)
            {
                var myTrackIds = await _db.MusicTracks.AsNoTracking()
                    .Where(t => t.Playlist.OwnerId == UserId)
                    .OrderBy(t => t.Title)
                    .Select(t => new SocialSongViewModel { Id = t.Id, Title = t.Title, Artist = t.Artist })
                    .ToListAsync();
                var added = songs.Select(x => x.Id).ToHashSet();
                model.AvailableSongs = myTrackIds.Where(t => !added.Contains(t.Id)).ToList();
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarCancion(string token, Guid cancionId)
        {
            var playlist = await _db.MusicPlaylists.FirstOrDefaultAsync(p => p.ShareToken == token);
            if (playlist == null) return NotFound();
            bool owner = playlist.OwnerId == UserId;
            bool collaborator = await _db.MusicPlaylistCollaborators.AnyAsync(c =>
                c.MusicPlaylistId == playlist.Id && c.UserId == UserId);
            if (!owner && !collaborator) return Forbid();
            var song = await _db.MusicTracks.AsNoTracking().Include(t => t.Playlist)
                .FirstOrDefaultAsync(t => t.Id == cancionId && t.Playlist.OwnerId == UserId);
            if (song == null) return NotFound();
            bool existing = song.MusicPlaylistId == playlist.Id ||
                await _db.MusicPlaylistTracks.AnyAsync(l =>
                    l.MusicPlaylistId == playlist.Id && l.MusicTrackId == song.Id);
            if (existing) return BadRequest(new { error = "Esta canción ya está en la playlist." });
            _db.MusicPlaylistTracks.Add(new MusicPlaylistTrack {
                MusicPlaylistId = playlist.Id, MusicTrackId = song.Id });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Compartida), new { token });
        }

        [HttpGet]
        public async Task<IActionResult> Audio(string token, Guid id)
        {
            var list = await _db.MusicPlaylists.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShareToken == token);
            if (list == null || !await CanRead(list)) return NotFound();
            bool included = await _db.MusicTracks.AnyAsync(t => t.Id == id && t.MusicPlaylistId == list.Id)
                || await _db.MusicPlaylistTracks.AnyAsync(t => t.MusicPlaylistId == list.Id && t.MusicTrackId == id);
            if (!included) return NotFound();
            var song = await _db.MusicTracks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (song == null || Path.GetFileName(song.StoredFileName) != song.StoredFileName) return NotFound();
            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "audio", song.StoredFileName);
            if (!System.IO.File.Exists(path)) return NotFound();
            Response.Headers.CacheControl = "private, no-store";
            return PhysicalFile(path, song.ContentType, enableRangeProcessing: true);
        }

        [HttpGet]
        public async Task<IActionResult> Portada(string token)
        {
            var list = await _db.MusicPlaylists.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ShareToken == token);
            if (list == null || !await CanRead(list) || string.IsNullOrWhiteSpace(list.CoverFileName))
                return NotFound();
            if (Path.GetFileName(list.CoverFileName) != list.CoverFileName) return NotFound();
            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "covers", list.CoverFileName);
            if (!System.IO.File.Exists(path)) return NotFound();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            Response.Headers.CacheControl = "private, no-store";
            return PhysicalFile(path, ext switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" });
        }
    }
}
