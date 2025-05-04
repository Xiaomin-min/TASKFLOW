// Services/AuthService.cs
using Microsoft.EntityFrameworkCore;
using TaskFlowApi.Data;
using TaskFlowApi.Models;
using TaskFlowApi.Models.Auth;
using BCryptNet = BCrypt.Net.BCrypt;

namespace TaskFlowApi.Services
{
    public class AuthService
    {
        private readonly TaskFlowDbContext _context;
        private readonly ILogger<AuthService> _logger; // Añadir Logging

        public AuthService(TaskFlowDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Exito, string? Error)> RegistrarUsuarioAsync(RegistroModelo modelo)
        {
            // Verificar si el usuario o email ya existen
            bool usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.NombreUsuario == modelo.NombreUsuario || u.Email == modelo.Email);

            if (usuarioExiste)
            {
                return (false, "El nombre de usuario o email ya está en uso.");
            }

            string passwordHash = BCryptNet.HashPassword(modelo.Password);

            var nuevoUsuario = new Usuario
            {
                NombreUsuario = modelo.NombreUsuario,
                Email = modelo.Email,
                PasswordHash = passwordHash
            };

            _context.Usuarios.Add(nuevoUsuario);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Usuario registrado exitosamente: {Username}", modelo.NombreUsuario);
                return (true, null);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al registrar usuario {Username}", modelo.NombreUsuario);
                // Analizar InnerException si es necesario para detalles
                return (false, "Error al guardar en la base de datos.");
            }
        }

        public async Task<Usuario?> VerificarCredencialesAsync(LoginModelo modelo)
        {
            var usuario = await _context.Usuarios
                                .FirstOrDefaultAsync(u => u.NombreUsuario == modelo.NombreUsuario);

            if (usuario != null && BCryptNet.Verify(modelo.Password, usuario.PasswordHash))
            {
                return usuario;
            }
            return null;
        }
    }
}