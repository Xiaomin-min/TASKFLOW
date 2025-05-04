// Data/TaskFlowDbContext.cs
using Microsoft.EntityFrameworkCore;
using TaskFlowApi.Models;

namespace TaskFlowApi.Data
{
    public class TaskFlowDbContext : DbContext
    {
        public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<ElementoTarea> Tareas { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Asegurar que el NombreUsuario sea único (índice único)
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.NombreUsuario)
                .IsUnique();

            // Asegurar que el Email sea único
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configurar la relación explícitamente (aunque EF Core la infiere)
            modelBuilder.Entity<ElementoTarea>()
                .HasOne(t => t.Propietario) // Una tarea tiene un propietario
                .WithMany(u => u.Tareas) // Un usuario tiene muchas tareas
                .HasForeignKey(t => t.UsuarioId) // La clave foránea es UsuarioId
                .OnDelete(DeleteBehavior.Cascade); // Opcional: Si se borra un usuario, borrar sus tareas
                                                   // O usar DeleteBehavior.Restrict para evitar borrar usuarios con tareas
        }
    }
}