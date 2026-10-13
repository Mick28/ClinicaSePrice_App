using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class DlgCerrarSesion
  {
    // «Inicio: 08:02 hs · Activa: 2 h 42 min» y, a la derecha, que el
    // cierre queda registrado en REGISTRO DE SESIÓN.
    static Control Tiempo()
    {
      return new Dibujo(40, g =>
      {
        Caja(g, new RectangleF(0, 0, Util, 40), Tema.Fondo);
        Iconos.Reloj(g, 12, 12, 16, Tema.Texto2);
        int x = Tramo(g, "Inicio: ", false, 34);
        x = Tramo(g, Formato.Hora(Sesion.Inicio) + " hs", true, x);
        x = Tramo(g, " · Activa: ", false, x);
        Tramo(g, Duracion(Reloj.Ahora - Sesion.Inicio), true, x);
        Registrado(g);
      });
    }

    // Escribe un tramo de texto (normal o en negrita) y devuelve dónde sigue.
    static int Tramo(Graphics g, string t, bool negrita, int x)
    {
      Font f = Tema.Fuente(10f, negrita ? FontStyle.Bold : FontStyle.Regular);
      TextRenderer.DrawText(g, t, f, new Rectangle(x, 0, Util, 40),
        negrita ? Tema.Texto : Tema.Texto2, TextFormatFlags.VerticalCenter |
        TextFormatFlags.NoPadding);
      return x + AnchoTexto(g, t, f);
    }

    // Ancho real del texto, contando los espacios del final.
    static int AnchoTexto(Graphics g, string t, Font f)
    {
      TextFormatFlags n = TextFormatFlags.NoPadding;
      return TextRenderer.MeasureText(g, t + "|", f, Size.Empty, n).Width -
        TextRenderer.MeasureText(g, "|", f, Size.Empty, n).Width;
    }

    static void Registrado(Graphics g)
    {
      const string t = "Cierre registrado";
      Font f = Tema.Fuente(9.5f, FontStyle.Bold);
      int ancho = TextRenderer.MeasureText(g, t, f, Size.Empty,
        TextFormatFlags.NoPadding).Width;
      Iconos.Escudo(g, Util - ancho - 36, 12, 16, Tema.Exito);
      TextRenderer.DrawText(g, t, f, new Rectangle(Util - ancho - 14, 0,
        ancho + 4, 40), Tema.Exito, TextFormatFlags.VerticalCenter |
        TextFormatFlags.NoPadding);
    }

    /// <summary>«2 h 42 min» o «35 min».</summary>
    public static string Duracion(TimeSpan t)
    {
      int min = Math.Max(0, (int)t.TotalMinutes);
      return min < 60 ? min + " min" : min / 60 + " h " + min % 60 + " min";
    }

    // «(Presioná Esc para cancelar o Enter para confirmar)», con las teclas.
    static Control Atajos()
    {
      return new Dibujo(26, g =>
      {
        string[] partes = { "(Presioná ", "Esc", " para cancelar o ",
          "Enter", " para confirmar)" };
        int x = Util - AnchoAtajos(g, partes);
        for (int i = 0; i < partes.Length; i++)
          x = i % 2 == 1 ? Tecla(g, partes[i], x) : Texto(g, partes[i], x);
      });
    }

    static int AnchoAtajos(Graphics g, string[] partes)
    {
      int ancho = 0;
      for (int i = 0; i < partes.Length; i++)
        ancho += Medir(g, partes[i]) + (i % 2 == 1 ? 14 : 0);
      return ancho;
    }

    static int Medir(Graphics g, string t)
    {
      return AnchoTexto(g, t, Tema.Fuente(10f));
    }

    static int Texto(Graphics g, string t, int x)
    {
      TextRenderer.DrawText(g, t, Tema.Fuente(10f), new Rectangle(x, 0,
        Util, 26), Tema.Texto2, TextFormatFlags.VerticalCenter |
        TextFormatFlags.NoPadding);
      return x + Medir(g, t);
    }

    static int Tecla(Graphics g, string t, int x)
    {
      int ancho = Medir(g, t) + 14;
      Caja(g, new RectangleF(x, 1, ancho, 24), Tema.Fondo);
      TextRenderer.DrawText(g, t, Tema.Fuente(9.5f, FontStyle.Bold),
        new Rectangle(x, 0, ancho, 26), Tema.Texto,
        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
      return x + ancho;
    }

    // Línea divisoria, candado y qué pasa al confirmar (CU-G-15).
    static Control Pie()
    {
      const string t = "Al confirmar volverás a la pantalla de inicio de " +
        "sesión. El sistema registra la hora de cierre y cierra la conexión" +
        " con la base de datos; tus datos guardados no se modifican.";
      Font f = Tema.Fuente(9.5f);
      int alto = 18 + Alto(t, f, Util - 28);
      return new Dibujo(alto, g =>
      {
        using (Pen p = new Pen(Tema.Borde)) g.DrawLine(p, 0, 0, Util, 0);
        Iconos.Candado(g, 0, 16, 15, Tema.Texto2);
        TextRenderer.DrawText(g, t, f, new Rectangle(26, 14, Util - 28,
          alto), Tema.Texto2, Ajustar);
      });
    }
  }
}
