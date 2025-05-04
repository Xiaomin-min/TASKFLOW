// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using TaskFlowApi.Models.Auth;
using TaskFlowApi.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;

    public AuthController(AuthService authService, JwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    [HttpPost("registrar")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Para errores de validación
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)] // Para usuario existente
    public async Task<IActionResult> Registrar([FromBody] RegistroModelo modelo)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (exito, error) = await _authService.RegistrarUsuarioAsync(modelo);

        if (exito)
        {
            return StatusCode(StatusCodes.Status201Created, new { Mensaje = "Usuario registrado exitosamente." });
        }
        else
        {
            // Asumiendo que el error es por conflicto si no fue éxito
            return Conflict(new { Mensaje = error ?? "Error desconocido al registrar." });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Devuelve { token: "..." }
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginModelo modelo)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _authService.VerificarCredencialesAsync(modelo);

        if (usuario != null)
        {
            var token = _jwtService.GenerarToken(usuario);
            return Ok(new { Token = token });
        }
        else
        {
            return Unauthorized(new { Mensaje = "Credenciales inválidas." });
        }
    }
}