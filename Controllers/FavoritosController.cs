using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpotifyNet.Controllers
{
    /// <summary>
    /// Bloque G. La página muestra los favoritos de la sesión actual.
    /// La consulta y las operaciones permanecen en BibliotecaController,
    /// que ya valida el dueño de cada archivo y el antifalsificación.
    /// </summary>
    [Authorize]
    public class FavoritosController : Controller
    {
        [HttpGet]
        public IActionResult Index() => View();
    }
}
