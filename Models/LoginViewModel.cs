
using System.ComponentModel.DataAnnotations;

namespace SpotifyNet.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Ingresa tu contraseña.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool Recordarme { get; set; }
    }
}
