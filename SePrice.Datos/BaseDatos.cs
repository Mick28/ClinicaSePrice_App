using System.IO;
using System.Reflection;

namespace SePrice.Datos
{
  /// <summary>
  /// Prepara el archivo de la base de datos la primera vez: crea las 29
  /// tablas del DER (esquema.sql, generado desde el diccionario de datos) y,
  /// si se pide, carga los datos de ejemplo de la demostración.
  /// </summary>
  public static class BaseDatos
  {
    /// <summary>Crea la base si el archivo no existe.</summary>
    public static void Preparar(string ruta, bool conDatosDeEjemplo)
    {
      if (File.Exists(ruta)) return;
      Conexion.Abrir(ruta);
      try
      {
        Consulta.EjecutarScript(Leer("esquema.sql"));
        if (conDatosDeEjemplo)
          Consulta.EjecutarScript(Leer("datos_ejemplo.sql"));
      }
      finally { Conexion.Cerrar(); }
    }

    /// <summary>Lee un script SQL incluido como recurso del proyecto.</summary>
    public static string Leer(string archivo)
    {
      Assembly a = typeof(BaseDatos).Assembly;
      string nombre = "SePrice.Datos.Sql." + archivo;
      using (Stream s = a.GetManifestResourceStream(nombre))
      {
        if (s == null)
          throw new FileNotFoundException("Falta el recurso " + nombre);
        using (StreamReader r = new StreamReader(s))
          return r.ReadToEnd();
      }
    }
  }
}
