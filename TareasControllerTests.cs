using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFlowApi.Controllers;
using TaskFlowApi.Data;
using TaskFlowApi.Models;
using TaskFlowApi.Services;
using TaskFlowApi.Enums;
using Microsoft.Extensions.Logging; // Necesario para ILogger

namespace TaskFlowApi.Tests
{
    public class TareasControllerTests
    {
        private readonly Mock<MotivacionService> _mockMotivacionService;
        private readonly Mock<ILogger<TareasController>> _mockLogger;

        public TareasControllerTests()
        {
            // Usamos mocks para servicios que no son el DbContext
            _mockMotivacionService = new Mock<MotivacionService>();
            _mockLogger = new Mock<ILogger<TareasController>>(); // Mock del Logger

            // Configuramos el mock para que siempre devuelva un mensaje
            _mockMotivacionService.Setup(s => s.ObtenerMensajeAleatorio()).Returns("¡Mensaje de prueba!");
        }

        // --- Helper para crear DbContext en Memoria ---
        private TaskFlowDbContext CrearDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
                .UseInMemoryDatabase(databaseName: dbName) // ¡Importante! Usar nombres únicos por test o grupo
                .Options;
            return new TaskFlowDbContext(options);
        }

        // --- Helper para Simular Usuario Autenticado ---
        private void SimularUsuarioAutenticado(ControllerBase controller, string nombreUsuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, nombreUsuario), // Este es el claim que usamos para obtener el ID
                new Claim(ClaimTypes.Name, nombreUsuario)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        // --- TESTS ---

        [Fact]
        public async Task ObtenerTareasUsuario_CuandoExistenTareas_DebeRetornarOkConListaDeTareas()
        {
            // Arrange
            var dbName = nameof(ObtenerTareasUsuario_CuandoExistenTareas_DebeRetornarOkConListaDeTareas);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "testuser1";

            // Añadir datos de prueba a la BD en memoria
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "test1@test.com", PasswordHash = "hash" });
            dbContext.Tareas.AddRange(
                new ElementoTarea { Id = 1, Titulo = "Tarea 1", UsuarioId = usuarioId },
                new ElementoTarea { Id = 2, Titulo = "Tarea 2", UsuarioId = usuarioId },
                new ElementoTarea { Id = 3, Titulo = "Tarea Otro User", UsuarioId = "otroUser" } // Tarea de otro usuario
            );
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);

            // Act
            var actionResult = await controller.ObtenerTareasUsuario();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result); // actionResult.Result porque devuelve ActionResult<T>
            var tareas = Assert.IsAssignableFrom<IEnumerable<ElementoTarea>>(okResult.Value);
            Assert.Equal(2, tareas.Count()); // Solo debe devolver las tareas de testuser1
            Assert.All(tareas, t => Assert.Equal(usuarioId, t.UsuarioId)); // Verificar que todas son del usuario correcto
        }

        [Fact]
        public async Task ObtenerTareasUsuario_CuandoNoExistenTareas_DebeRetornarOkConListaVacia()
        {
            // Arrange
            var dbName = nameof(ObtenerTareasUsuario_CuandoNoExistenTareas_DebeRetornarOkConListaVacia);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "testuser2";
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "test2@test.com", PasswordHash = "hash" });
            await dbContext.SaveChangesAsync(); // Guardar usuario

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);

            // Act
            var actionResult = await controller.ObtenerTareasUsuario();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var tareas = Assert.IsAssignableFrom<IEnumerable<ElementoTarea>>(okResult.Value);
            Assert.Empty(tareas);
        }


        [Fact]
        public async Task ObtenerTareaPorId_CuandoTareaExisteYPerteneceAlUsuario_DebeRetornarOkConTarea()
        {
            // Arrange
            var dbName = nameof(ObtenerTareaPorId_CuandoTareaExisteYPerteneceAlUsuario_DebeRetornarOkConTarea);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "userConTarea";
            var idTarea = 5L;
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "user@test.com", PasswordHash = "hash" });
            dbContext.Tareas.Add(new ElementoTarea { Id = idTarea, Titulo = "Tarea Específica", UsuarioId = usuarioId });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);

            // Act
            var actionResult = await controller.ObtenerTareaPorId(idTarea);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var tarea = Assert.IsType<ElementoTarea>(okResult.Value);
            Assert.Equal(idTarea, tarea.Id);
            Assert.Equal(usuarioId, tarea.UsuarioId);
        }

        [Fact]
        public async Task ObtenerTareaPorId_CuandoTareaNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            var dbName = nameof(ObtenerTareaPorId_CuandoTareaNoExiste_DebeRetornarNotFound);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "userSinTarea";
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "user@test.com", PasswordHash = "hash" });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);

            // Act
            var actionResult = await controller.ObtenerTareaPorId(999L); // ID que no existe

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            Assert.NotNull(notFoundResult.Value); // Debería tener un cuerpo con el mensaje
        }

        [Fact]
        public async Task ObtenerTareaPorId_CuandoTareaExistePeroNoPerteneceAlUsuario_DebeRetornarNotFound()
        {
            // Arrange
            var dbName = nameof(ObtenerTareaPorId_CuandoTareaExistePeroNoPerteneceAlUsuario_DebeRetornarNotFound);
            var dbContext = CrearDbContext(dbName);
            var usuarioIdActual = "usuarioActual";
            var otroUsuarioId = "otroUsuario";
            var idTareaAjena = 6L;
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioIdActual, Email = "actual@t.com", PasswordHash = "h" });
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = otroUsuarioId, Email = "otro@t.com", PasswordHash = "h" });
            dbContext.Tareas.Add(new ElementoTarea { Id = idTareaAjena, Titulo = "Tarea Ajena", UsuarioId = otroUsuarioId });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioIdActual);

            // Act
            var actionResult = await controller.ObtenerTareaPorId(idTareaAjena);

            // Assert
            Assert.IsType<NotFoundObjectResult>(actionResult.Result); // NotFound para ocultar existencia
        }

        [Fact]
        public async Task CrearTarea_ConDatosValidos_DebeRetornarCreatedAtActionConTarea()
        {
            // Arrange
            var dbName = nameof(CrearTarea_ConDatosValidos_DebeRetornarCreatedAtActionConTarea);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "creadorUser";
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "creador@t.com", PasswordHash = "h" });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);
            var nuevaTareaDto = new TareasController.CrearTareaDto("Nueva Tarea Test", "Descripción test", null);

            // Act
            var actionResult = await controller.CrearTarea(nuevaTareaDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var tareaCreada = Assert.IsType<ElementoTarea>(createdAtActionResult.Value);

            Assert.Equal(nuevaTareaDto.Titulo, tareaCreada.Titulo);
            Assert.Equal(usuarioId, tareaCreada.UsuarioId);
            Assert.Equal(EstadoTarea.Pendiente, tareaCreada.Estado);
            Assert.True(tareaCreada.Id > 0); // El ID debe haber sido asignado por EF Core In-Memory

            // Verificar que se guardó en la "base de datos"
            var tareaEnDb = await dbContext.Tareas.FindAsync(tareaCreada.Id);
            Assert.NotNull(tareaEnDb);
            Assert.Equal(nuevaTareaDto.Titulo, tareaEnDb.Titulo);
        }

        [Fact]
        public async Task ActualizarTarea_MarcarComoCompletada_DebeRetornarOkConTareaYMensaje()
        {
            // Arrange
            var dbName = nameof(ActualizarTarea_MarcarComoCompletada_DebeRetornarOkConTareaYMensaje);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "updaterUser";
            var tareaId = 10L;
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "updater@t.com", PasswordHash = "h" });
            dbContext.Tareas.Add(new ElementoTarea { Id = tareaId, Titulo = "Tarea a Completar", UsuarioId = usuarioId, Estado = EstadoTarea.EnProgreso });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);
            var tareaActualizadaDto = new TareasController.ActualizarTareaDto(
                "Tarea a Completar", "Desc actualizada", EstadoTarea.Completada, null); // Cambiando estado

            // Act
            var actionResult = await controller.ActualizarTarea(tareaId, tareaActualizadaDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult); // Devuelve IActionResult directamente
            dynamic valorRetornado = okResult.Value!; // Usar dynamic para acceder a propiedades anónimas fácil
            ElementoTarea tareaActualizada = valorRetornado.GetType().GetProperty("Tarea")!.GetValue(valorRetornado, null);
            string mensaje = valorRetornado.GetType().GetProperty("MensajeMotivacional")!.GetValue(valorRetornado, null);


            Assert.Equal(EstadoTarea.Completada, tareaActualizada.Estado);
            Assert.Equal("¡Mensaje de prueba!", mensaje); // Verifica que el mensaje mockeado está

            // Verificar cambio en DB
            var tareaEnDb = await dbContext.Tareas.FindAsync(tareaId);
            Assert.Equal(EstadoTarea.Completada, tareaEnDb!.Estado);
        }

        [Fact]
        public async Task ActualizarTarea_SinMarcarComoCompletada_DebeRetornarNoContent()
        {
            // Arrange
            var dbName = nameof(ActualizarTarea_SinMarcarComoCompletada_DebeRetornarNoContent);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "updaterUser2";
            var tareaId = 11L;
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "updater2@t.com", PasswordHash = "h" });
            dbContext.Tareas.Add(new ElementoTarea { Id = tareaId, Titulo = "Tarea a Actualizar", UsuarioId = usuarioId, Estado = EstadoTarea.Pendiente });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);
            var tareaActualizadaDto = new TareasController.ActualizarTareaDto(
                "Tarea Actualizada", "Desc actualizada", EstadoTarea.EnProgreso, null); // Cambiando estado pero NO a completada

            // Act
            var actionResult = await controller.ActualizarTarea(tareaId, tareaActualizadaDto);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);

            // Verificar cambio en DB
            var tareaEnDb = await dbContext.Tareas.FindAsync(tareaId);
            Assert.Equal(EstadoTarea.EnProgreso, tareaEnDb!.Estado);
            Assert.Equal("Tarea Actualizada", tareaEnDb!.Titulo);

        }

        [Fact]
        public async Task EliminarTarea_CuandoTareaExisteYPerteneceAlUsuario_DebeRetornarNoContent()
        {
            // Arrange
            var dbName = nameof(EliminarTarea_CuandoTareaExisteYPerteneceAlUsuario_DebeRetornarNoContent);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "deleterUser";
            var tareaId = 15L;
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "deleter@t.com", PasswordHash = "h" });
            dbContext.Tareas.Add(new ElementoTarea { Id = tareaId, Titulo = "Tarea a Eliminar", UsuarioId = usuarioId });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);

            // Verificar que existe antes
            Assert.NotNull(await dbContext.Tareas.FindAsync(tareaId));

            // Act
            var actionResult = await controller.EliminarTarea(tareaId);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);

            // Verificar que se eliminó de la "base de datos"
            var tareaEnDb = await dbContext.Tareas.FindAsync(tareaId);
            Assert.Null(tareaEnDb);
        }

        [Fact]
        public async Task EliminarTarea_CuandoTareaNoExiste_DebeRetornarNotFound()
        {
            // Arrange
            var dbName = nameof(EliminarTarea_CuandoTareaNoExiste_DebeRetornarNotFound);
            var dbContext = CrearDbContext(dbName);
            var usuarioId = "deleterUser2";
            dbContext.Usuarios.Add(new Usuario { NombreUsuario = usuarioId, Email = "deleter2@t.com", PasswordHash = "h" });
            await dbContext.SaveChangesAsync();

            var controller = new TareasController(dbContext, _mockMotivacionService.Object, _mockLogger.Object);
            SimularUsuarioAutenticado(controller, usuarioId);

            // Act
            var actionResult = await controller.EliminarTarea(999L); // ID que no existe

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
            Assert.NotNull(notFoundResult.Value);
        }
    }
}