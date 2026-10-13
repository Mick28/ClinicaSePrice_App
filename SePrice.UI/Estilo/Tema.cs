using System.Collections.Generic;
using System.Drawing;

namespace SePrice.UI
{
  /// <summary>
  /// Colores y tipografía de la especificación de interfaz (2.1 y 2.2).
  /// Todos los textos sobre blanco cumplen contraste AA.
  /// </summary>
  static class Tema
  {
    public static readonly Color Primario = Hex("#0B5E7E");
    public static readonly Color PrimarioHover = Hex("#094C66");
    public static readonly Color PrimarioSuave = Hex("#E3F0F5");
    public static readonly Color Fondo = Hex("#F5F7FA");
    public static readonly Color Superficie = Color.White;
    public static readonly Color Borde = Hex("#D9DEE5");
    public static readonly Color Texto = Hex("#1B2430");
    public static readonly Color Texto2 = Hex("#5B6675");
    public static readonly Color Exito = Hex("#1E7D45");
    public static readonly Color Advertencia = Hex("#B26B00");
    public static readonly Color Error = Hex("#C62828");
    public static readonly Color Info = Hex("#1565C0");
    public static readonly Color CircuitoA = Hex("#1D6FB8");
    public static readonly Color CircuitoB = Hex("#6A4C93");
    public static readonly Color Auditoria = Hex("#E8EEF3");

    // Segoe UI viene con Windows; la especificación sugiere Inter, que no
    // siempre está instalada (alternativas: Roboto, Arial).
    const string Familia = "Segoe UI";
    static readonly Dictionary<string, Font> fuentes =
      new Dictionary<string, Font>();

    /// <summary>Fuente del tamaño (en puntos) y estilo pedidos.</summary>
    public static Font Fuente(float puntos, FontStyle estilo =
      FontStyle.Regular)
    {
      string clave = puntos + "-" + estilo;
      Font f;
      if (!fuentes.TryGetValue(clave, out f))
      {
        f = new Font(Familia, puntos, estilo);
        fuentes[clave] = f;
      }
      return f;
    }

    /// <summary>Color de un circuito: «A» azul, «B» violeta.</summary>
    public static Color DeCircuito(string c)
    {
      return c == "B" ? CircuitoB : CircuitoA;
    }

    /// <summary>
    /// Color de cada estado (lista única 2.5 y estados afines).
    /// </summary>
    public static Color DeEstado(string estado)
    {
      switch ((estado ?? "").ToLowerInvariant())
      {
        case "reservado": case "info": case "propuesto":
        case "para el sector": return Info;
        case "acreditado": case "enviado": case "abierta": return Primario;
        case "en atención": case "pendiente": case "bajo mínimo":
        case "egreso no registrado": case "modificación":
        case "en proceso": return Advertencia;
        case "atendido": case "realizado": case "entregado": case "válida":
        case "presente": case "habilitado": case "cerrada": case "ok":
        case "recibido": case "activo": case "alta":
        case "listo para retirar": return Exito;
        case "cancelado": case "rechazada": case "baja": return Error;
        default: return Texto2;
      }
    }

    static Color Hex(string h)
    {
      return ColorTranslator.FromHtml(h);
    }
  }
}
