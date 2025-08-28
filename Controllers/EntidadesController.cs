// EntidadesController.cs — Controlador mínimo para listar registros.
// Principios SOLID aplicados:
// - SRP: el controlador solo coordina la solicitud HTTP; no contiene reglas de negocio.
// - DIP: depende de la interfaz IServicioCrud, no de una clase concreta.
// - ISP: consume solo el método ListarAsync que la interfaz expone por ahora.

using Microsoft.AspNetCore.Authorization;                 // Permite decorar acciones con [AllowAnonymous] o políticas.
using Microsoft.AspNetCore.Mvc;                           // Tipos base para controladores y respuestas HTTP.
using System.Threading.Tasks;                             // Habilita métodos asincrónicos con Task.
using Microsoft.Extensions.Logging;                       // Para registrar eventos y errores del controlador.
using webapicsharp.Servicios.Abstracciones;               // Importa la interfaz IServicioCrud.

namespace webapicsharp.Controllers
{
    // Define la ruta base con un segmento variable {tabla}.
    // Ejemplo: GET /api/clientes  → "tabla" = "clientes"
    [Route("api/{tabla}")]
    [ApiController]                                       // Activa comportamientos de API (validación automática, 400, etc.).
    public class EntidadesController : ControllerBase
    {
        // Campo privado para almacenar la referencia al servicio CRUD.
        private readonly IServicioCrud _servicioCrud;

        // Campo privado para el logger que registra eventos del controlador.
        private readonly ILogger<EntidadesController> _logger;

        // Constructor con Inyección de Dependencias.
        // Recibe la ABSTRACCIÓN (IServicioCrud) registrada en Program.cs (DIP).
        // También recibe el logger para registrar eventos importantes.
        public EntidadesController(
            IServicioCrud servicioCrud,                   // Servicio que contiene la lógica de negocio CRUD.
            ILogger<EntidadesController> logger           // Logger específico para este controlador.
        )
        {
            // Validación defensiva para evitar referencias nulas.
            _servicioCrud = servicioCrud
                ?? throw new System.ArgumentNullException(nameof(servicioCrud));
            _logger = logger
                ?? throw new System.ArgumentNullException(nameof(logger));
        }

        // Acción HTTP GET para listar registros.
        // Ruta completa: GET /api/{tabla}?esquema=dbo&limite=100
        // [AllowAnonymous] permite probar sin autenticación mientras se construye el proyecto.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ListarAsync(
            string tabla,                 // Se llena desde {tabla} en la ruta.
            [FromQuery] string? esquema,  // Parámetro opcional: ?esquema=dbo
            [FromQuery] int? limite       // Parámetro opcional: ?limite=100
        )
        {
            try
            {
                // Registrar el inicio de la operación para auditoría y debugging.
                _logger.LogInformation("Consultando tabla: {Tabla}, Esquema: {Esquema}, Límite: {Limite}",
                    tabla, esquema ?? "por defecto", limite?.ToString() ?? "por defecto");

                // Delegar la operación al servicio (el controlador no conoce detalles de BD).
                var filas = await _servicioCrud.ListarAsync(tabla, esquema, limite);

                // Registrar el resultado exitoso de la operación.
                _logger.LogInformation("Consulta completada. Registros obtenidos: {Cantidad}", filas.Count);

                // Si no hay datos, responder 204 (No Content) para indicar éxito sin contenido.
                if (filas.Count == 0)
                {
                    _logger.LogInformation("No se encontraron registros para la tabla: {Tabla}", tabla);
                    return NoContent();
                }

                // Si hay datos, responder 200 (OK) con la lista de filas y metadatos útiles.
                return Ok(new
                {
                    tabla = tabla,                        // Nombre de la tabla consultada.
                    esquema = esquema ?? "dbo",           // Esquema usado (con valor por defecto si era null).
                    limite = limite,                      // Límite aplicado (puede ser null si no se especificó).
                    total = filas.Count,                  // Cantidad total de registros obtenidos.
                    datos = filas                         // Los registros reales obtenidos.
                });
            }
            catch (System.ArgumentException excepcionArgumento)
            {
                // Errores de validación de entrada (tabla vacía, límite inválido, etc.).
                // Registrar la advertencia para debugging sin exponer detalles internos.
                _logger.LogWarning("Error de validación para tabla {Tabla}: {Mensaje}", tabla, excepcionArgumento.Message);

                return BadRequest(new
                {
                    estado = 400,
                    mensaje = "Parámetros de entrada inválidos.",
                    detalle = excepcionArgumento.Message,
                    tabla = tabla
                });
            }
            catch (System.InvalidOperationException excepcionOperacion)
            {
                // Errores de operación (tabla no existe, conexión fallida, etc.).
                // Registrar el error para investigación posterior.
                _logger.LogError(excepcionOperacion, "Error de operación para tabla {Tabla}: {Mensaje}", tabla, excepcionOperacion.Message);

                return NotFound(new
                {
                    estado = 404,
                    mensaje = "El recurso solicitado no fue encontrado.",
                    detalle = excepcionOperacion.Message,
                    tabla = tabla
                });
            }
            catch (System.Exception excepcionGeneral)
            {
                // Cualquier otra excepción se responde como 500 para no filtrar detalles internos.
                // Registrar el error completo para análisis posterior.
                _logger.LogError(excepcionGeneral, "Error inesperado al consultar tabla {Tabla}", tabla);

                return StatusCode(500, new
                {
                    estado = 500,
                    mensaje = "Error interno del servidor.",
                    tabla = tabla,
                    // Solo incluir detalle en desarrollo, no en producción.
                    detalle = "Contacte al administrador del sistema."
                });
            }
        }

        // Endpoint adicional para obtener información sobre el controlador.
        // Ruta: GET /api/info (cuando tabla = "info").
        [AllowAnonymous]
        [HttpGet]
        [Route("api/info")]                               // Ruta específica que no interfiere con {tabla}.
        public IActionResult ObtenerInformacion()
        {
            // Devolver información útil sobre el controlador y sus capacidades.
            return Ok(new
            {
                controlador = "EntidadesController",      // Nombre del controlador.
                version = "1.0",                          // Versión actual.
                descripcion = "Controlador genérico para consultar tablas de base de datos.",
                endpoints = new[]                         // Lista de endpoints disponibles.
                {
                    "GET /api/{tabla} - Lista registros de una tabla",
                    "GET /api/{tabla}?esquema={esquema} - Lista con esquema específico",
                    "GET /api/{tabla}?limite={numero} - Lista con límite de registros",
                    "GET /api/info - Muestra esta información"
                },
                ejemplos = new[]                          // Ejemplos de uso prácticos.
                {
                    "GET /api/usuarios",
                    "GET /api/productos?esquema=ventas",
                    "GET /api/clientes?limite=50",
                    "GET /api/pedidos?esquema=ventas&limite=100"
                }
            });
        }
        /// <summary>
        /// Endpoint raíz que proporciona información de bienvenida sobre la API.
        /// Mapea a GET / (ruta raíz de la aplicación)
        /// </summary>
        /// <returns>Información de bienvenida y enlaces útiles</returns>
        [AllowAnonymous] // Permite acceso sin autenticación
        [HttpGet("/")]   // Mapea específicamente a la ruta raíz
        public IActionResult Inicio()
        {
            // Crear objeto anónimo con información de bienvenida y navegación
            var mensaje = new
            {
                Mensaje = "Bienvenido a la API Genérica en C#!",
                Version = "1.0",
                Descripcion = "API genérica para operaciones CRUD sobre cualquier tabla de base de datos",
                Documentacion = "Para más detalles, visita /swagger",
                FechaServidor = DateTime.UtcNow, // Usar UTC para garantizar consistencia
                Enlaces = new
                {
                    Swagger = "/swagger",           // Documentación interactiva
                    Info = "/api/info",            // Información del controlador
                    EjemploTabla = "/api/MiTabla"  // Ejemplo de consulta de tabla
                },
                Uso = new[]
                {
                    "GET /api/{tabla} - Lista registros de una tabla",
                    "GET /api/{tabla}?limite=50 - Lista con límite",
                    "GET /api/{tabla}?esquema=dbo - Lista con esquema específico"
                }
            };

            // Devolver respuesta HTTP 200 con la información de bienvenida
            return Ok(mensaje);
        }
    }
}