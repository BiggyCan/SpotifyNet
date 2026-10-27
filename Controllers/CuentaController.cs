using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpotifyNet.Models;

namespace SpotifyNet.Controllers
{
    public class CuentaController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        private static readonly HashSet<string> GenerosPermitidos = new(
            StringComparer.Ordinal)
        {
            "Hombre", "Mujer", "No binario", "Otro", "Prefiero no aclararlo"
        };

        public CuentaController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.MostrarContrasena = true;
                return View(model);
            }

            var resultado = await _signInManager.PasswordSignInAsync(
                model.Email.Trim(),
                model.Password,
                model.Recordarme,
                lockoutOnFailure: true);

            if (resultado.Succeeded)
                return RedirectToAction("Index", "Home");

            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError("",
                    "Cuenta bloqueada temporalmente por varios intentos.");
            }
            else
            {
                ModelState.AddModelError("",
                    "Correo o contraseña incorrectos.");
            }

            ViewBag.MostrarContrasena = true;
            return View(model);
        }

        [HttpGet]
        public IActionResult Registrarse()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrarse(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            model.Email = (model.Email ?? "").Trim();
            model.DisplayName = (model.DisplayName ?? "").Trim();

            if (!GenerosPermitidos.Contains(model.Gender ?? ""))
            {
                ModelState.AddModelError(nameof(model.Gender),
                    "Selecciona una opción de género válida.");
            }

            if (!model.AcceptConditions)
            {
                ModelState.AddModelError(nameof(model.AcceptConditions),
                    "Confirma las condiciones de la cuenta de demostración.");
            }

            DateTime fechaNacimiento = default;
            if (model.BirthDay.HasValue && model.BirthMonth.HasValue &&
                model.BirthYear.HasValue)
            {
                try
                {
                    fechaNacimiento = new DateTime(
                        model.BirthYear.Value,
                        model.BirthMonth.Value,
                        model.BirthDay.Value);

                    if (fechaNacimiento > DateTime.Today.AddYears(-13))
                    {
                        ModelState.AddModelError(nameof(model.BirthYear),
                            "Para usar esta demostración debes tener al menos 13 años.");
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    ModelState.AddModelError(nameof(model.BirthDay),
                        "La fecha de nacimiento no es válida.");
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            var usuario = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var resultado = await _userManager.CreateAsync(usuario, model.Password);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    if (error.Code is "DuplicateUserName" or "DuplicateEmail")
                    {
                        ModelState.AddModelError(nameof(model.Email),
                            "Este correo ya está registrado.");
                    }
                    else
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }

                return View(model);
            }

            // Guardamos el perfil en AspNetUserClaims, que ya forma parte
            // de Identity y NO requiere una nueva migración de SQLite.
            var claims = new List<Claim>
            {
                new("SpotifyNet.DisplayName", model.DisplayName),
                new("SpotifyNet.BirthDate", fechaNacimiento.ToString(
                    "yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new("SpotifyNet.Gender", model.Gender ?? ""),
                new("SpotifyNet.ReceiveNews", model.ReceiveNews.ToString()),
                new("SpotifyNet.ParticipateInTests", model.ParticipateInTests.ToString()),
                new("SpotifyNet.ConditionsAcceptedAtUtc",
                    DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture))
            };

            var guardado = await _userManager.AddClaimsAsync(usuario, claims);

            if (!guardado.Succeeded)
            {
                await _userManager.DeleteAsync(usuario);
                ModelState.AddModelError("",
                    "No se pudo guardar el perfil. Inténtalo nuevamente.");
                return View(model);
            }

            await _signInManager.SignInAsync(usuario, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesion()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
