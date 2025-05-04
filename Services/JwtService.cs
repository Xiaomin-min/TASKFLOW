// Services/JwtService.cs
// ... (código idéntico al de la respuesta anterior, asegurándose de usar IConfiguration) ...
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskFlowApi.Models;


namespace TaskFlowApi.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerarToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key no configurada"));
            var duration = double.TryParse(jwtSettings["DurationInHours"], out var hours) ? hours : 1.0; // Valor por defecto 1 hora

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.NombreUsuario), // Usar ClaimTypes estándar
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),         // Incluir nombre para conveniencia
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Identificador único del token
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(duration),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}