using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Data;

namespace SpotifyNet.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly UserManager<IdentityUser> _users;
        private readonly SignInManager<IdentityUser> _signIn;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public PerfilController(UserManager<IdentityUser> users,
            SignInManager<IdentityUser> signIn, ApplicationDbContext db,
            IWebHostEnvironment environment)
        {
            _users = users;
            _signIn = signIn;
            _db = db;
            _environment = environment;
        }

        private string PhotoFolder => Path.Combine(_environment.ContentRootPath, "App_Data", "avatars");

        [HttpGet]
        public async Task<IActionResult> Editar()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            var claims = await _users.GetClaimsAsync(user);
            ViewBag.Name = claims.FirstOrDefault(c => c.Type == "SpotifyNet.DisplayName")?.Value
                ?? user.Email?.Split('@')[0] ?? "Usuario";
            ViewBag.Bio = claims.FirstOrDefault(c => c.Type == "SpotifyNet.Bio")?.Value ?? "";
            ViewBag.Email = user.Email ?? "";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(string nombre, string biografia)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            nombre = (nombre ?? "").Trim();
            biografia = (biografia ?? "").Trim();
            if (nombre.Length is < 2 or > 50 || biografia.Length > 400)
            {
                TempData["ProfileError"] = "El nombre debe tener de 2 a 50 caracteres y la biografía máximo 400.";
                return RedirectToAction(nameof(Editar));
            }
            if (!await PutClaim(user, "SpotifyNet.DisplayName", nombre) ||
                !await PutClaim(user, "SpotifyNet.Bio", biografia))
            {
                TempData["ProfileError"] = "No se pudo actualizar el perfil.";
                return RedirectToAction(nameof(Editar));
            }
            await _signIn.RefreshSignInAsync(user);
            TempData["ProfileOk"] = "Perfil actualizado.";
            return RedirectToAction(nameof(Editar));
        }

        private async Task<bool> PutClaim(IdentityUser user, string type, string value)
        {
            var claims = await _users.GetClaimsAsync(user);
            var old = claims.FirstOrDefault(c => c.Type == type);
            var result = old == null
                ? await _users.AddClaimAsync(user, new Claim(type, value))
                : await _users.ReplaceClaimAsync(user, old, new Claim(type, value));
            return result.Succeeded;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(4 * 1024 * 1024)]
        public async Task<IActionResult> SubirFoto(IFormFile archivo)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Challenge();
            if (archivo == null || archivo.Length < 12 || archivo.Length > 3 * 1024 * 1024)
                return BadRequest("Selecciona una imagen de menos de 3 MB.");
            var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                return BadRequest("Solo JPG, PNG o WEBP.");
            byte[] header = new byte[12];
            await using (var fs = archivo.OpenReadStream())
            {
                if (await fs.ReadAsync(header.AsMemory(0, 12)) < 12)
                    return BadRequest("Imagen incompleta.");
            }
            bool valid = ext switch
            {
                ".jpg" or ".jpeg" => header[0] == 255 && header[1] == 216 && header[2] == 255,
                ".png" => header.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                ".webp" => System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                    && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP",
                _ => false
            };
            if (!valid) return BadRequest("La firma del archivo no es una imagen válida.");
            Directory.CreateDirectory(PhotoFolder);
            string fileName = Guid.NewGuid().ToString("N") + (ext == ".jpeg" ? ".jpg" : ext);
            string path = Path.Combine(PhotoFolder, fileName);
            await using (var output = new FileStream(path, FileMode.CreateNew))
                await archivo.CopyToAsync(output);
            var claims = await _users.GetClaimsAsync(user);
            string? old = claims.FirstOrDefault(c => c.Type == "SpotifyNet.Avatar")?.Value;
            if (!await PutClaim(user, "SpotifyNet.Avatar", fileName))
            {
                System.IO.File.Delete(path);
                return BadRequest("No se pudo guardar la foto.");
            }
            if (!string.IsNullOrEmpty(old) && Path.GetFileName(old) == old)
            {
                var oldPath = Path.Combine(PhotoFolder, old);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            await _signIn.RefreshSignInAsync(user);
            TempData["ProfileOk"] = "Foto de perfil actualizada.";
            return RedirectToAction(nameof(Editar));
        }

        [HttpGet]
        public async Task<IActionResult> Foto(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();
            var file = await _db.UserClaims.AsNoTracking()
                .Where(c => c.UserId == id && c.ClaimType == "SpotifyNet.Avatar")
                .Select(c => c.ClaimValue).FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(file) || Path.GetFileName(file) != file) return NotFound();
            var path = Path.Combine(PhotoFolder, file);
            if (!System.IO.File.Exists(path)) return NotFound();
            Response.Headers.CacheControl = "private, no-store";
            return PhysicalFile(path, Path.GetExtension(file).ToLowerInvariant() switch {
                ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg"
            });
        }
    }
}
