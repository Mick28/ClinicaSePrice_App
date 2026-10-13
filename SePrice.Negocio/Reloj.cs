using System;
using System.Globalization;

namespace SePrice.Negocio
{
  /// <summary>
  /// Fecha y hora del sistema. En la demostración arranca en una fecha y
  /// hora fijas (jueves 15/10/2026, 08:50) y avanza como un reloj real,
  /// para que los datos de ejemplo tengan sentido cualquier día.
  /// </summary>
  public static class Reloj
  {
    static DateTime? inicioDemo;
    static DateTime arranque = DateTime.Now;

    /// <summary>Fecha y hora actuales (o de la demostración).</summary>
    public static DateTime Ahora
    {
      get
      {
        if (inicioDemo == null) return DateTime.Now;
        return inicioDemo.Value + (DateTime.Now - arranque);
      }
    }

    /// <summary>Fecha de hoy, sin hora.</summary>
    public static DateTime Hoy
    {
      get { return Ahora.Date; }
    }

    /// <summary>Indica si el reloj es el de la demostración.</summary>
    public static bool EsDemo
    {
      get { return inicioDemo != null; }
    }

    /// <summary>Configura el reloj de la demostración.</summary>
    public static void Configurar(string fecha, string hora)
    {
      DateTime f;
      if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd",
        CultureInfo.InvariantCulture, DateTimeStyles.None, out f)) return;
      TimeSpan h;
      if (!TimeSpan.TryParse(hora, out h)) h = new TimeSpan(8, 50, 0);
      Fijar(f.Date + h);
    }

    /// <summary>Lleva el reloj de la demostración a un momento dado.</summary>
    public static void Fijar(DateTime momento)
    {
      inicioDemo = momento;
      arranque = DateTime.Now;
    }
  }
}
