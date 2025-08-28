// RepositorioLecturaPostgreSQL.cs --- Implementación concreta para leer datos usando Npgsql y PostgreSQL.
// Principios SOLID aplicados:
// - SRP: esta clase solo se encarga de leer datos desde PostgreSQL.
// - DIP: implementa IRepositorioLecturaTabla (abstracción) y usa IProveedorConexion.
// - OCP: implementación específica para PostgreSQL sin tocar otras clases.

using System; // Para excepciones y tipos básicos del sistema
using System.Collections.Generic; // Para usar List<> y Dictionary<> genéricos
using System.Threading.Tasks; // Para programación asíncrona con async/await
using Npgsql; // Para conectar y ejecutar comandos en PostgreSQL
using webapicsharp.Repositorios.Abstracciones; // Para implementar la interfaz IRepositorioLecturaTabla
using webapicsharp.Servicios.Conexion; // Para usar IProveedorConexion y obtener cadenas de conexión

namespace webapicsharp.Repositorios
{
    /// <summary>
    /// Implementación específica para leer datos de PostgreSQL usando Npgsql.
    /// Esta clase se encarga únicamente de acceso a datos en PostgreSQL, cumpliendo el principio SRP.
    /// </summary>
    public class RepositorioLecturaPostgreSQL : IRepositorioLecturaTabla
    {
        // Campo privado que mantiene la referencia al proveedor de conexión inyectado
        private readonly IProveedorConexion _proveedorConexion;

        /// <summary>
        /// Constructor que recibe el proveedor de conexión mediante inyección de dependencias.
        /// Aplica DIP: depende de la abstracción IProveedorConexion, no de implementaciones concretas.
        /// </summary>
        /// <param name="proveedorConexion">Proveedor que entrega cadenas de conexión según configuración</param>
        public RepositorioLecturaPostgreSQL(IProveedorConexion proveedorConexion)
        {
            // Validación defensiva para evitar referencias nulas en tiempo de ejecución
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }

        /// <summary>
        /// Obtiene filas de una tabla de PostgreSQL como lista de diccionarios.
        /// Cada diccionario representa una fila donde la clave es el nombre de columna y el valor es el dato.
        /// </summary>
        /// <param name="nombreTabla">Nombre de la tabla a consultar (obligatorio)</param>
        /// <param name="esquema">Esquema de la tabla (opcional, por defecto usa 'public')</param>
        /// <param name="limite">Número máximo de filas a retornar (opcional, por defecto 1000)</param>
        /// <returns>Lista inmutable de diccionarios representando las filas obtenidas</returns>
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla, // Nombre de la tabla que se desea consultar
            string? esquema,     // Esquema de la base de datos (puede ser null)
            int? limite          // Límite máximo de registros (puede ser null)
        )
        {
            // VALIDACIONES: verificar que los parámetros de entrada sean válidos
            // Validar que el nombre de la tabla no sea nulo, vacío o solo espacios en blanco
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            // NORMALIZACIÓN: aplicar valores por defecto para parámetros opcionales
            // Si no se especifica esquema, usar 'public' que es el esquema por defecto en PostgreSQL
            string esquemaFinal = string.IsNullOrWhiteSpace(esquema) ? "public" : esquema.Trim();
            
            // Si no se especifica límite, usar 1000 como valor por defecto para evitar consultas masivas
            int limiteFinal = limite ?? 1000;

            // CONSTRUCCIÓN DE CONSULTA SQL: crear la consulta SELECT con LIMIT para PostgreSQL
            // PostgreSQL usa comillas dobles para identificadores y LIMIT en lugar de TOP
            string consultaSql = $@"
                SELECT * 
                FROM ""{esquemaFinal}"".""{nombreTabla}""
                LIMIT @limite";

            // EJECUCIÓN DE CONSULTA: crear lista para almacenar los resultados
            var resultados = new List<Dictionary<string, object?>>();
            
            try
            {
                // Obtener la cadena de conexión a través del proveedor (aplicando DIP)
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
                
                // Crear la conexión PostgreSQL usando la cadena obtenida del proveedor
                using var conexion = new NpgsqlConnection(cadenaConexion);
                
                // Abrir la conexión de forma asíncrona para no bloquear el hilo
                await conexion.OpenAsync();
                
                // Crear el comando SQL con la consulta construida y la conexión activa
                using var comando = new NpgsqlCommand(consultaSql, conexion);
                
                // Agregar el parámetro para el límite (evita inyección SQL)
                comando.Parameters.AddWithValue("@limite", limiteFinal);
                
                // Ejecutar la consulta de forma asíncrona y obtener el lector de datos
                using var lector = await comando.ExecuteReaderAsync();
                
                // PROCESAMIENTO DE RESULTADOS: leer cada fila devuelta por la consulta
                // Iterar sobre todas las filas obtenidas del lector
                while (await lector.ReadAsync())
                {
                    // Crear un diccionario para representar la fila actual
                    var fila = new Dictionary<string, object?>();
                    
                    // Iterar sobre todas las columnas de la fila actual
                    for (int indiceColumna = 0; indiceColumna < lector.FieldCount; indiceColumna++)
                    {
                        // Obtener el nombre de la columna actual
                        string nombreColumna = lector.GetName(indiceColumna);
                        
                        // Obtener el valor de la columna, convertir DBNull a null
                        object? valorColumna = lector.IsDBNull(indiceColumna) ? null : lector.GetValue(indiceColumna);
                        
                        // Agregar el par clave-valor al diccionario de la fila
                        fila[nombreColumna] = valorColumna;
                    }
                    
                    // Agregar la fila completa a la lista de resultados
                    resultados.Add(fila);
                }
            }
            catch (NpgsqlException excepcionPostgres)
            {
                // Capturar errores específicos de PostgreSQL y re-lanzar con contexto claro
                throw new InvalidOperationException($"Error al consultar la tabla '{esquemaFinal}.{nombreTabla}' en PostgreSQL: {excepcionPostgres.Message}", excepcionPostgres);
            }
            catch (Exception excepcionGeneral)
            {
                // Capturar cualquier otro error inesperado y re-lanzar con contexto
                throw new InvalidOperationException($"Error inesperado al acceder a PostgreSQL: {excepcionGeneral.Message}", excepcionGeneral);
            }

            // Devolver la lista como IReadOnlyList para cumplir el contrato de la interfaz
            // Esto previene modificaciones accidentales de los resultados por parte del consumidor
            return resultados;
        }
    }
}