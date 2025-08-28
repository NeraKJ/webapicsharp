// ProveedorConexion.cs — Encapsula cómo se obtiene la cadena de conexión según "DatabaseProvider".
// Principios SOLID:
// - SRP: esta clase solo sabe "leer configuración y entregar una cadena de conexión".
// - DIP: se expone una interfaz (IProveedorConexion) para que el repositorio dependa de la abstracción.
// - OCP: si mañana se agrega MySQL/Oracle, se extiende sin tocar el resto del sistema.

using Microsoft.Extensions.Configuration; // Permite leer appsettings.*.json
using System;                              // Para InvalidOperationException

namespace webapicsharp.Servicios.Conexion
{
    /// <summary>
    /// Contrato que define cómo obtener la cadena de conexión activa y el nombre del proveedor.
    /// </summary>
    public interface IProveedorConexion
    {
        string ProveedorActual { get; }           // Ej: "SqlServer", "SqlServerEXPRESS", "LocalDb", "Postgres"
        string ObtenerCadenaConexion();           // Devuelve la cadena de conexión lista para usar
    }

    /// <summary>
    /// Implementación que lee "DatabaseProvider" y "ConnectionStrings" desde IConfiguration.
    /// </summary>
    public class ProveedorConexion : IProveedorConexion
    {
        private readonly IConfiguration _configuracion; // Mantiene una referencia a la configuración cargada en Program.cs

        /// <summary>
        /// Recibe la configuración por inyección de dependencias.
        /// </summary>
        public ProveedorConexion(IConfiguration configuracion)
        {
            _configuracion = configuracion ?? throw new ArgumentNullException(nameof(configuracion));
        }

        /// <summary>
        /// Lee el valor de "DatabaseProvider" (appsettings.*.json). 
        /// Si no existe, por defecto usa "SqlServer".
        /// </summary>
        public string ProveedorActual
        {
            get
            {
                // Obtiene "DatabaseProvider" del archivo de configuración (Paso 9).
                // Si es null o vacío, se devuelve "SqlServer" como valor seguro para desarrollo.
                var valor = _configuracion.GetValue<string>("DatabaseProvider");
                return string.IsNullOrWhiteSpace(valor) ? "SqlServer" : valor.Trim();
            }
        }

        /// <summary>
        /// Entrega la cadena de conexión correspondiente al proveedor actual.
        /// Lanza excepción clara si no se encuentra.
        /// </summary>
        public string ObtenerCadenaConexion()
        {
            // Busca en ConnectionStrings la entrada cuyo nombre coincide con ProveedorActual.
            // Ejemplos válidos: "SqlServer", "SqlServerEXPRESS", "LocalDb", "Postgres"
            string? cadena = _configuracion.GetConnectionString(ProveedorActual);

            // Si no existe, se lanza una excepción con mensaje entendible para novatos.
            if (string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException(
                    $"No se encontró la cadena de conexión para el proveedor '{ProveedorActual}'. " +
                    $"Verificar 'ConnectionStrings' y 'DatabaseProvider' en appsettings.*.json."
                );
            }

            // Devuelve la cadena lista para ser usada por ADO.NET o EF Core en pasos siguientes.
            return cadena;
        }
    }
}
