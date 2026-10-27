using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpotifyNet.Controllers;

/// <summary>
/// Seis mezclas calculadas únicamente a partir de los archivos de la
/// cuenta conectada. No utiliza música externa ni comparte bibliotecas.
/// </summary>
[Authorize]
public class MixesController : Controller
{
    [HttpGet]
    public IActionResult Detalle(int numero = 1)
    {
        if (numero < 1 || numero > 6) return NotFound();
        ViewData["Title"] = $"Mix diario {numero}";
        return View(numero);
    }
}
