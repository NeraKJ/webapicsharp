// IRepositorioLecturaTabla.cs --- Contrato de lectura genérica por tabla.
// Principios SOLID aplicados:
// - SRP (Responsabilidad Única): esta interfaz solo define operaciones de LECTURA.
// - DIP (Inversión de Dependencias): el servicio pedirá esta ABSTRACCIÓN, no clases concretas.
// - OCP (Abierto/Cerrado): permite agregar nuevas implementaciones (SQL Server, PostgreSQL) sin modificar el servicio.

using System.Collections.Generic;   // Habilita List<> y Dictionary<> para colecciones.
using System.Threading.Tasks;       // Habilita Task y async/await para operaciones asíncronas.

namespace webapicsharp.Repositorios.Abstracciones
{
    /// <summary>
    /// Contrato para repositorios que leen filas desde una tabla de base de datos.
    /// Cada implementación define cómo conecta y ejecuta (ADO.NET, EF Core, proveedor específico, etc.).
    /// </summary>
    public interface IRepositorioLecturaTabla
    {
        /// <summary>
        /// Obtiene filas de una tabla como lista de diccionarios (columna → valor).
        /// </summary>
        /// <param name="nombreTabla">Nombre de la tabla a consultar (obligatorio).</param>
        /// <param name="esquema">Esquema de la tabla (opcional; null permite que la implementación use su propio valor por defecto, p. ej. "dbo").</param>
        /// <param name="limite">Máximo de filas a devolver (opcional; null significa sin límite o un valor por defecto interno).</param>
        /// <returns>Lista inmutable de filas; cada fila es un diccionario con clave = nombre de columna, valor = dato (puede ser null).</returns>
        Task<IReadOnlyList<Dictionary<string, object?>>> ObtenerFilasAsync(
            string nombreTabla, // Nombre de la tabla (obligatorio).
            string? esquema,     // Esquema (opcional; null → por defecto).
            int? limite          // Límite de filas (opcional; null → sin límite/por defecto).
        );
    }
}