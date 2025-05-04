// Models/Usuario.cs
using System.ComponentModel.DataAnnotations;

namespace TaskFlowApi.Models
{
    public class Usuario
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string NombreUsuario { get; set; } = null!;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        // Propiedad de navegación (EF Core la usa para relaciones)
        public virtual ICollection<ElementoTarea> Tareas { get; set; } = new List<ElementoTarea>();
    }
}
