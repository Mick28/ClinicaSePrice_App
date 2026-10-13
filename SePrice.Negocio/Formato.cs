using System;
using System.Globalization;

namespace SePrice.Negocio
{
  /// <summary>
  /// Formatos de la interfaz (especificación, 2.7) y de la base de datos
  /// (diccionario: fechas aaaa-mm-dd, horas hh:mm).
  /// </summary>
  public static class Formato
  {
    static readonly CultureInfo Ar = new CultureInfo("es-AR");
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly string[] Dias =
      { "dom", "lun", "mar", "mié", "jue", "vie", "sáb" };
    static readonly string[] DiasLargos = { "domingo", "lunes", "martes",
      "miércoles", "jueves", "viernes", "sábado" };

    /// <summary>Fecha para la base: 2026-10-15.</summary>
    public static string Iso(DateTime f)
    {
      return f.ToString("yyyy-MM-dd", Inv);
    }

    /// <summary>Hora para la base: 09:15.</summary>
    public static string Hora(DateTime f) { return f.ToString("HH:mm", Inv); }

    /// <summary>Fecha y hora para la base: 2026-10-15 09:15.</summary>
    public static string IsoHora(DateTime f)
    {
      return f.ToString("yyyy-MM-dd HH:mm", Inv);
    }

    /// <summary>Lee una fecha o fecha y hora guardada en la base.</summary>
    public static DateTime Leer(string texto)
    {
      string[] f = { "yyyy-MM-dd HH:mm", "yyyy-MM-dd" };
      return DateTime.ParseExact(texto, f, Inv, DateTimeStyles.None);
    }

    /// <summary>Minutos desde las 00:00 de una hora «hh:mm».</summary>
    public static int Minutos(string hora)
    {
      return int.Parse(hora.Substring(0, 2)) * 60
        + int.Parse(hora.Substring(3, 2));
    }

    /// <summary>Hora «hh:mm» a partir de minutos.</summary>
    public static string HoraDe(int minutos)
    {
      return (minutos / 60).ToString("00") + ":" +
        (minutos % 60).ToString("00");
    }

    /// <summary>Fecha corta de la interfaz: jue 15/10/2026.</summary>
    public static string Corta(DateTime f)
    {
      return Dias[(int)f.DayOfWeek] + " " + f.ToString("dd/MM/yyyy", Inv);
    }

    /// <summary>Fecha larga: jueves 15/10/2026.</summary>
    public static string Larga(DateTime f)
    {
      return DiasLargos[(int)f.DayOfWeek] + " " + f.ToString("dd/MM/yyyy", Inv);
    }

    /// <summary>Día de la semana como en la base: «jueves».</summary>
    public static string DiaSemana(DateTime f)
    {
      return DiasLargos[(int)f.DayOfWeek];
    }

    /// <summary>Fecha de la base mostrada como 15/10/2026.</summary>
    public static string Mostrar(string iso)
    {
      if (string.IsNullOrEmpty(iso)) return "—";
      DateTime f = Leer(iso);
      return iso.Length > 10 ? f.ToString("dd/MM/yyyy HH:mm", Inv)
        : f.ToString("dd/MM/yyyy", Inv);
    }

    /// <summary>Importe: $ 18.000.</summary>
    public static string Pesos(decimal importe)
    {
      return "$ " + importe.ToString("#,##0", Ar);
    }

    /// <summary>DNI con puntos: 30.123.456.</summary>
    public static string Dni(int dni) { return dni.ToString("#,##0", Ar); }

    /// <summary>Edad en años cumplidos a la fecha de hoy.</summary>
    public static int Edad(string nacimiento)
    {
      if (string.IsNullOrEmpty(nacimiento)) return 0;
      DateTime n = Leer(nacimiento), hoy = Reloj.Hoy;
      int edad = hoy.Year - n.Year;
      return n.AddYears(edad) > hoy ? edad - 1 : edad;
    }
  }
}
