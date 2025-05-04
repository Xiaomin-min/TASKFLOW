// Models/Auth/RegistroModelo.cs
using System.ComponentModel.DataAnnotations;
// ... (código idéntico al de la respuesta anterior) ...
namespace TaskFlowApi.Models.Auth
{
    public class RegistroModelo
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
        public string NombreUsuario { get; set; } = null!;

        [Required(ErrorMessage = "El email es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string Password { get; set; } = null!;
    }
}
