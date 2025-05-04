// Controllers/TareasController.cs
namespace TaskFlowApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TaskFlowApi.Data;
using TaskFlowApi.Enums;
using TaskFlowApi.Models;
using TaskFlowApi.Services;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación para todas las acciones
public class TareasController : ControllerBase
{
    private readonly TaskFlowDbContext _context;
    private readonly MotivacionService _motivacionService;
    private readonly ILogger<TareasController> _logger;

    public TareasController(TaskFlowDbContext context, MotivacionService motivacionService, ILogger<TareasController> logger)
    {
        _context = context;
        _motivacionService = motivacionService;
        _logger = logger;
    }

    // Helper para obtener el NombreUsuario del token JWT
    private string ObtenerUsuarioActualId() => User.FindFirstValue(ClaimTypes.NameIdentifier)
                                            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

    // GET: api/tareas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ElementoTarea>>> ObtenerTareasUsuario()
    {
        var usuarioId = ObtenerUsuarioActualId();
        var tareas = await _context.Tareas
                            .Where(t => t.UsuarioId == usuarioId)
                            .OrderByDescending(t => t.FechaCreacion)
                            .AsNoTracking() // Mejora rendimiento para solo lectura
                            .ToListAsync();
        return Ok(tareas);
    }

    // GET: api/tareas/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ElementoTarea), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ElementoTarea>> ObtenerTareaPorId(long id)
    {
        var usuarioId = ObtenerUsuarioActualId();
        var tarea = await _context.Tareas
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId);

        if (tarea == null)
        {
            return NotFound(new { Mensaje = $"Tarea con ID {id} no encontrada para este usuario." });
        }
        return Ok(tarea);
    }

    // POST: api/tareas
    // Usaremos un DTO (Data Transfer Object) para la creación para no exponer todo el modelo
    public record CrearTareaDto(
        [Required(ErrorMessage = "El título es obligatorio.")] string Titulo,
        string? Descripcion,
        DateTime? FechaVencimiento
    );

    [HttpPost]
    [ProducesResponseType(typeof(ElementoTarea), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ElementoTarea>> CrearTarea([FromBody] CrearTareaDto tareaDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState); // Aunque el record valida

        var usuarioId = ObtenerUsuarioActualId();
        var nuevaTarea = new ElementoTarea
        {
            Titulo = tareaDto.Titulo,
            Descripcion = tareaDto.Descripcion,
            FechaVencimiento = tareaDto.FechaVencimiento,
            UsuarioId = usuarioId,
            Estado = EstadoTarea.Pendiente, // Estado inicial
            FechaCreacion = DateTime.UtcNow
            // EF Core asignará el Id al guardar
        };

        _context.Tareas.Add(nuevaTarea);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tarea creada ID: {TareaId} por Usuario: {UsuarioId}", nuevaTarea.Id, usuarioId);

        // Devuelve 201 Created con la ubicación y el objeto completo creado
        return CreatedAtAction(nameof(ObtenerTareaPorId), new { id = nuevaTarea.Id }, nuevaTarea);
    }

    // Usaremos un DTO para la actualización
    public record ActualizarTareaDto(
        [Required(ErrorMessage = "El título es obligatorio.")] string Titulo,
        string? Descripcion,
        EstadoTarea Estado, // Permitir actualizar estado
        DateTime? FechaVencimiento
    );

    // PUT: api/tareas/{id} - Actualización completa
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status200OK)] // Si se completa
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarTarea(long id, [FromBody] ActualizarTareaDto tareaDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var usuarioId = ObtenerUsuarioActualId();
        var tareaExistente = await _context.Tareas
                                    .FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId);

        if (tareaExistente == null)
        {
            return NotFound(new { Mensaje = $"Tarea con ID {id} no encontrada para actualizar." });
        }

        bool seCompleto = !tareaExistente.Estado.Equals(EstadoTarea.Completada) && tareaDto.Estado.Equals(EstadoTarea.Completada);

        // Actualizar propiedades (EF Core rastrea los cambios)
        tareaExistente.Titulo = tareaDto.Titulo;
        tareaExistente.Descripcion = tareaDto.Descripcion;
        tareaExistente.Estado = tareaDto.Estado;
        tareaExistente.FechaVencimiento = tareaDto.FechaVencimiento;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Tarea actualizada ID: {TareaId} por Usuario: {UsuarioId}", id, usuarioId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Conflicto de concurrencia al actualizar tarea ID: {TareaId}", id);
            // Re-verificar si existe
            bool existe = await _context.Tareas.AnyAsync(e => e.Id == id && e.UsuarioId == usuarioId);
            if (!existe) return NotFound(); else throw; // Relanzar si aún existe pero hubo otro error
        }

        if (seCompleto)
        {
            var mensaje = _motivacionService.ObtenerMensajeAleatorio();
            // Devolvemos 200 OK con la tarea actualizada y el mensaje
            return Ok(new { Tarea = tareaExistente, MensajeMotivacional = mensaje });
        }
        else
        {
            // Si no se completó, devolvemos 204 No Content (éxito sin cuerpo)
            return NoContent();
        }
    }

    // DELETE: api/tareas/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarTarea(long id)
    {
        var usuarioId = ObtenerUsuarioActualId();
        var tareaExistente = await _context.Tareas
                                    .FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId);

        if (tareaExistente == null)
        {
            return NotFound(new { Mensaje = $"Tarea con ID {id} no encontrada para eliminar." });
        }

        _context.Tareas.Remove(tareaExistente);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tarea eliminada ID: {TareaId} por Usuario: {UsuarioId}", id, usuarioId);

        return NoContent();
    }
}