// ServicioCrud.cs --- Implementación del contrato IServicioCrud que usa el repositorio de lectura.
// Objetivo: aplicar DIP (Dependency Inversion Principle) delegando el acceso a datos al repositorio.
// Principios SOLID aplicados:
// - SRP: esta clase orquesta la lógica de negocio, pero delega el acceso a datos.
// - DIP: depende de IRepositorioLecturaTabla (abstracción), no de una implementación concreta.
// - OCP: se puede cambiar el repositorio sin tocar este servicio.

using System.Collections.Generic;                                    // Usar listas y diccionarios genéricos.
using System.Threading.Tasks;                                        // Usar Task y async/await.
using webapicsharp.Servicios.Abstracciones;                         // Usar la interfaz IServicioCrud definida anteriormente.
using webapicsharp.Repositorios.Abstracciones;                      // Usar la interfaz IRepositorioLecturaTabla que acabamos de crear.

namespace webapicsharp.Servicios
{
    /// <summary>
    /// Implementación del servicio CRUD que usa un repositorio para acceso a datos.
    /// En este paso: ya no devuelve lista vacía, sino que delega al repositorio.
    /// </summary>
    public class ServicioCrud : IServicioCrud
    {
        private readonly IRepositorioLecturaTabla _repositorioLectura; // Campo que mantiene la referencia al repositorio inyectado.

        /// <summary>
        /// Constructor que recibe el repositorio por inyección de dependencias.
        /// Aplica DIP: este servicio depende de la abstracción (interfaz), no de la implementación concreta.
        /// </summary>
        /// <param name="repositorioLectura">Repositorio que sabe cómo leer datos desde la base de datos.</param>
        public ServicioCrud(IRepositorioLecturaTabla repositorioLectura)
        {
            // Validación defensiva: asegurar que la dependencia no sea nula.
            _repositorioLectura = repositorioLectura ?? throw new System.ArgumentNullException(nameof(repositorioLectura));
        }

        /// <summary>
        /// Listar registros de una tabla delegando al repositorio.
        /// Este método implementa la lógica de negocio (validaciones, transformaciones) 
        /// pero delega el acceso a datos al repositorio.
        /// </summary>
        public async Task<IReadOnlyList<Dictionary<string, object?>>> ListarAsync(
            string nombreTabla, // Nombre de la tabla que se desea leer.
            string? esquema,     // Esquema (opcional); si viene null, se usará el predeterminado.
            int? limite          // Límite de filas (opcional); si viene null, se aplicará un valor por defecto.
        )
        {
            // VALIDACIONES BÁSICAS (lógica de negocio). Mantienen el servicio robusto.
            // Verificar que el nombre de la tabla tenga contenido válido (no nulo, no vacío, no solo espacios).
            if (string.IsNullOrWhiteSpace(nombreTabla))
                throw new System.ArgumentException("El nombre de la tabla no puede estar vacío.", nameof(nombreTabla));

            // Normalizar el esquema: si es null o vacío, se deja como null; el repositorio aplicará su valor por defecto.
            string? esquemaNormalizado = string.IsNullOrWhiteSpace(esquema) ? null : esquema.Trim();

            // Normalizar el límite: si no viene o es <= 0, se deja null; el repositorio aplicará su valor por defecto.
            int? limiteNormalizado = (limite is null || limite <= 0) ? null : limite;

            // DELEGACIÓN AL REPOSITORIO (DIP en acción):
            // El servicio no sabe cómo se conecta a la BD ni qué proveedor se usa.
            // Solo llama al repositorio con los parámetros validados y normalizados.
            var filas = await _repositorioLectura.ObtenerFilasAsync(nombreTabla, esquemaNormalizado, limiteNormalizado);

            // TRANSFORMACIONES ADICIONALES (lógica de negocio):
            // En este paso básico, se devuelven las filas tal como vienen del repositorio.
            // En pasos futuros, aquí se podrían aplicar filtros, transformaciones, 
            // validaciones de políticas de seguridad, etc.

            return filas;
        }
    }
}