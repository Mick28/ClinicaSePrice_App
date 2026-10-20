using System.Data.Common;

namespace SePrice.Datos
{
  /// <summary>
  /// Única conexión con la base de datos. Se abre al iniciar sesión y se
  /// cierra al cerrarla (material de la Etapa 3: «la puerta» de la sesión).
  /// Ninguna pantalla abre su propia conexión.
  /// </summary>
  public static class Conexion
  {
    static DbConnection actual;

    /// <summary>Ruta del archivo de la base (SQLite).</summary>
    public static string Ruta { get; private set; }

    /// <summary>Indica si la conexión está abierta.</summary>
    public static bool Abierta
    {
      get { return actual != null; }
    }

    /// <summary>Abre la conexión, si no estaba abierta.</summary>
    public static void Abrir(string ruta)
    {
      if (actual != null) return;
      Ruta = ruta;
      actual = Proveedor.Crear("Data Source=" + ruta);
      actual.Open();
      Consulta.EjecutarSinSesion("PRAGMA foreign_keys = ON;");
    }

    /// <summary>Cierra la conexión (cierre de sesión).</summary>
    public static void Cerrar()
    {
      DbConnection conexion = actual;
      actual = null;
      if (conexion == null) return;
      try { conexion.Close(); }
      finally { conexion.Dispose(); }
    }

    /// <summary>Conexión en uso. Falla si la sesión no la abrió.</summary>
    internal static DbConnection Actual
    {
      get
      {
        if (actual == null)
          throw new System.InvalidOperationException(
            "No hay una sesión abierta con la base de datos.");
        return actual;
      }
    }
  }
}
