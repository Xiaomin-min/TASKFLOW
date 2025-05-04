// Models/Auth/LoginModelo.cs
using System.ComponentModel.DataAnnotations;
// ... (código idéntico al de la respuesta anterior) ...
namespace TaskFlowApi.Models.Auth
{
    public class LoginModelo
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string NombreUsuario { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = null!;
    }
}
