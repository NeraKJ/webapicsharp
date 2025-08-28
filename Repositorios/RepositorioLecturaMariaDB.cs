// RepositorioLecturaMariaDB.cs --- Implementación concreta para leer datos usando MySqlConnector y MariaDB.
// Principios SOLID aplicados:
// - SRP: esta clase solo se encarga de leer datos desde MariaDB.
// - DIP: implementa IRepositorioLecturaTabla (abstracción) y usa IProveedorConexion.
// - OCP: nueva implementación sin modificar código existente.

using System; // Para excepciones y tipos básicos del sistema
using System.Collections.Generic; // Para usar List<> y Dictionary<> genéricos
using System.Threading.Tasks; // Para programación asíncrona con async/await
using MySqlConnector; // Para conectar y ejecutar comandos en MariaDB/MySQL
using webapicsharp.Repositorios.Abstracciones; // Para implementar la interfaz IRepositorioLecturaTabla
using webapicsharp.Servicios.Conexion; // Para usar IProveedorConexion y obtener cadenas de conexión

namespace webapicsharp.Repositorios
{
    /// <summary>
    /// Implementación específica para leer datos de MariaDB usando MySqlConnector.
    /// Esta clase se encarga únicamente de acceso a datos MariaDB, cumpliendo el principio SRP.
    /// </summary>
    public class RepositorioLecturaMariaDB : IRepositorioLecturaTabla
    {
        // Campo privado que mantiene la referencia al proveedor de conexión inyectado
        private readonly IProveedorConexion _proveedorConexion;

        /// <summary>
        /// Constructor que recibe el proveedor de conexión mediante inyección de dependencias.
        /// Aplica DIP: depende de la abstracción IProveedorConexion, no de implementaciones concretas.
        /// </summary>
        /// <param name="proveedorConexion">Proveedor que entrega cadenas de conexión según configuración</param>
        public RepositorioLecturaMariaDB(IProveedorConexion proveedorConexion)
        {
            // Validación defensiva para evitar referencias nulas en tiempo de ejecución
            _proveedorConexion = proveedorConexion ?? throw new ArgumentNullException(nameof(proveedorConexion));
        }

        /// <summary>
        /// Obtiene filas de una tabla de MariaDB como lista de diccionarios.
        /// Cada diccionario representa una fila donde la clave es el nombre de columna y el valor es el dato.
        /// </summary>
        /// <param name="nombreTabla">Nombre de la tabla a consultar (obligatorio)</param>
        /// <param name="esquema">Esquema de la tabla (opcional, por defecto usa la base de datos actual)</param>
        /// <param name="limite">Número máximo de filas a retornar (opcional, por defecto 1000)</param>
        /// <returns>Lista inmutable de diccionarios representando las filas obtenidas</returns>
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla, // Nombre de la tabla que se desea consultar
            string? esquema,     // Esquema de la base de datos (puede ser null en MariaDB)
            int? limite          // Límite máximo de registros (puede ser null)
        )
        {
            // VALIDACIONES: verificar que los parámetros de entrada sean válidos
            // Validar que el nombre de la tabla no sea nulo, vacío o solo espacios en blanco
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            // NORMALIZACIÓN: aplicar valores por defecto para parámetros opcionales
            // Si no se especifica límite, usar 1000 como valor por defecto para evitar consultas masivas
            int limiteFinal = limite ?? 1000;

            // CONSTRUCCIÓN DE CONSULTA SQL: crear la consulta SELECT con LIMIT para MariaDB
            // MariaDB usa backticks para identificadores y LIMIT al final
            string consultaSql;
            if (string.IsNullOrWhiteSpace(esquema))
            {
                // Sin esquema especificado, usar solo el nombre de la tabla
                consultaSql = $"SELECT * FROM `{nombreTabla}` LIMIT @limite";
            }
            else
            {
                // Con esquema especificado (base de datos)
                consultaSql = $"SELECT * FROM `{esquema.Trim()}`.`{nombreTabla}` LIMIT @limite";
            }

            // EJECUCIÓN DE CONSULTA: crear lista para almacenar los resultados
            var resultados = new List<Dictionary<string, object?>>();
            
            try
            {
                // Obtener la cadena de conexión a través del proveedor (aplicando DIP)
                string cadenaConexion = _proveedorConexion.ObtenerCadenaConexion();
                
                // Crear la conexión MariaDB usando la cadena obtenida del proveedor
                using var conexion = new MySqlConnection(cadenaConexion);
                
                // Abrir la conexión de forma asíncrona para no bloquear el hilo
                await conexion.OpenAsync();
                
                // Crear el comando SQL con la consulta construida y la conexión activa
                using var comando = new MySqlCommand(consultaSql, conexion);
                
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
            catch (MySqlException excepcionMariaDB)
            {
                // Capturar errores específicos de MariaDB y re-lanzar con contexto claro
                var tablaCompleta = string.IsNullOrWhiteSpace(esquema) ? nombreTabla : $"{esquema}.{nombreTabla}";
                throw new InvalidOperationException($"Error al consultar la tabla '{tablaCompleta}' en MariaDB: {excepcionMariaDB.Message}", excepcionMariaDB);
            }
            catch (Exception excepcionGeneral)
            {
                // Capturar cualquier otro error inesperado y re-lanzar con contexto
                throw new InvalidOperationException($"Error inesperado al acceder a MariaDB: {excepcionGeneral.Message}", excepcionGeneral);
            }

            // Devolver la lista como IReadOnlyList para cumplir el contrato de la interfaz
            // Esto previene modificaciones accidentales de los resultados por parte del consumidor
            return resultados;
        }
    }
}