// Models/ElementoTarea.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskFlowApi.Enums;

namespace TaskFlowApi.Models
{
    public class ElementoTarea
    {
        [Key] // Indica que es la clave primaria
        public long Id { get; set; } // EF Core lo configurará como autoincremental por defecto

        [Required(ErrorMessage = "El título es obligatorio.")]
        [StringLength(150)]
        public string Titulo { get; set; } = null!;

        public string? Descripcion { get; set; }

        [Required]
        public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaVencimiento { get; set; }

        // Clave Foránea
        [Required]
        public string UsuarioId { get; set; } = null!;

        // Propiedad de Navegación hacia el Usuario propietario
        [ForeignKey("UsuarioId")]
        public virtual Usuario Propietario { get; set; } = null!;
    }
}
