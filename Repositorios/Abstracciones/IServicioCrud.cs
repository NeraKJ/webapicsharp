// IServicioCrud.cs — Contrato para operaciones CRUD (Crear, Leer, Actualizar, Eliminar) en la capa de servicios.
// Principios SOLID:
// - DIP (Dependency Inversion Principle): el controlador trabajará contra esta interfaz (abstracción), no contra clases concretas.
// - ISP (Interface Segregation Principle): empezamos solo con un método, para no forzar a implementar operaciones que aún no se necesitan.

using System.Collections.Generic; // Permite usar colecciones como List<T> y Dictionary<TKey, TValue>.
using System.Threading.Tasks;     // Permite trabajar con tareas asincrónicas (async/await).

// El namespace organiza el código por carpetas y contexto lógico.
// Aquí se usa el mismo nombre de carpeta "Servicios.Abstracciones" para mantener la coherencia.
namespace webapicsharp.Servicios.Abstracciones
{
    /// <summary>
    /// Interfaz que define el contrato para un servicio CRUD genérico.
    /// Por ahora solo se incluye el método ListarAsync.
    /// </summary>
    public interface IServicioCrud
    {
        /// <summary>
        /// Método para listar registros de una tabla en la base de datos.
        /// </summary>
        /// <param name="nombreTabla">
        /// Nombre de la tabla que se quiere consultar (por ejemplo "Clientes").
        /// </param>
        /// <param name="esquema">
        /// Nombre del esquema de base de datos (por ejemplo "dbo" en SQL Server). 
        /// Puede ser null si se usa el esquema predeterminado.
        /// </param>
        /// <param name="limite">
        /// Cantidad máxima de filas que se desean obtener.
        /// Puede ser null si se desea aplicar un valor por defecto.
        /// </param>
        /// <returns>
        /// Devuelve una lista de diccionarios donde:
        ///  - La clave del diccionario es el nombre de la columna.
        ///  - El valor es el contenido de la celda para esa fila.
        /// </returns>
        Task<IReadOnlyList<Dictionary<string, object?>>> ListarAsync(
            string nombreTabla, // Nombre de la tabla.
            string? esquema,    // Esquema de la base de datos.
            int? limite         // Límite de registros.
        );
    }
}
