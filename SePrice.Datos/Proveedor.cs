using System.Data.Common;

namespace SePrice.Datos
{
  /// <summary>
  /// Crea la conexión del motor elegido (SQLite, RT-04). Es el único lugar
  /// del sistema que conoce el motor: para pasar a un motor servidor sólo
  /// se cambia esta clase.
  /// </summary>
  static class Proveedor
  {
    public static DbConnection Crear(string cadena)
    {
#if MONO
      // Compilación de prueba con Mono (Linux), usada por el equipo de
      // desarrollo para verificar el código fuera de Windows.
      return new Mono.Data.Sqlite.SqliteConnection(cadena);
#else
      return new Microsoft.Data.Sqlite.SqliteConnection(cadena);
#endif
    }
  }
}
