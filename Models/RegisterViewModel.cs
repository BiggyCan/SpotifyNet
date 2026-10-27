using System.ComponentModel.DataAnnotations;

namespace SpotifyNet.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ingresa tu correo electrónico.")]
        [EmailAddress(ErrorMessage = "Escribe un correo electrónico válido.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Crea una contraseña.")]
        [MinLength(10, ErrorMessage = "La contraseña necesita al menos 10 caracteres.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*\d).{10,}$",
            ErrorMessage = "Incluye una letra minúscula y un número.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Escribe el nombre que aparecerá en tu perfil.")]
        [StringLength(50, MinimumLength = 2,
            ErrorMessage = "El nombre debe tener entre 2 y 50 caracteres.")]
        public string DisplayName { get; set; } = "";

        [Required(ErrorMessage = "Indica el día de nacimiento.")]
        [Range(1, 31, ErrorMessage = "El día debe estar entre 1 y 31.")]
        public int? BirthDay { get; set; }

        [Required(ErrorMessage = "Selecciona el mes de nacimiento.")]
        [Range(1, 12, ErrorMessage = "Selecciona un mes válido.")]
        public int? BirthMonth { get; set; }

        [Required(ErrorMessage = "Indica el año de nacimiento.")]
        [Range(1900, 9999, ErrorMessage = "Indica un año válido.")]
        public int? BirthYear { get; set; }

        [Required(ErrorMessage = "Selecciona una opción de género.")]
        public string Gender { get; set; } = "";

        // Preferencias de demostración: no envían mensajes ni comparten datos.
        public bool ReceiveNews { get; set; }
        public bool ParticipateInTests { get; set; }

        public bool AcceptConditions { get; set; }
    }
}
