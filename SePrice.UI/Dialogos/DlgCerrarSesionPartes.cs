using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class DlgCerrarSesion
  {
    static readonly Color Naranja = Color.FromArgb(214, 120, 22);
    static readonly Color AvisoFondo = Color.FromArgb(255, 244, 229);
    const TextFormatFlags Ajustar = TextFormatFlags.WordBreak |
      TextFormatFlags.NoPadding;

    static int Alto(string texto, Font f, int ancho)
    {
      return TextRenderer.MeasureText(texto, f, new Size(ancho, 0),
        Ajustar).Height;
    }

    // Ícono de pregunta, procesos del cierre, título y quién tiene la sesión.
    static Control Cabecera()
    {
      Usuario u = Sesion.Actual;
      string quien = "Sesión activa de " + FrmPrincipal.NombreVisible(u) +
        " · " + Permisos.Nombres[u.Perfil];
      Font f = Tema.Fuente(10.5f);
      int alto = Math.Max(64, 50 + Alto(quien, f, Util - 80));
      return new Dibujo(alto, g =>
      {
        Pregunta(g);
        Codigos(g, 80, 0);
        TextRenderer.DrawText(g, "¿Querés cerrar la sesión?", Tema.Fuente(
          15f, FontStyle.Bold), new Point(78, 18), Tema.Texto);
        TextRenderer.DrawText(g, quien, f, new Rectangle(80, 50, Util - 80,
          alto), Tema.Texto2, Ajustar);
      });
    }

    static void Pregunta(Graphics g)
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (SolidBrush b = new SolidBrush(Tema.PrimarioSuave))
        g.FillEllipse(b, 0, 0, 60, 60);
      using (SolidBrush b = new SolidBrush(Tema.Primario))
        g.FillEllipse(b, 16, 16, 28, 28);
      TextRenderer.DrawText(g, "?", Tema.Fuente(12f, FontStyle.Bold),
        new Rectangle(16, 16, 28, 28), Color.White,
        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    // «CU-G-15 · CU-A-19 · CU-B-17» con el punto de proceso compartido.
    static void Codigos(Graphics g, int x, int y)
    {
      const string t = "CU-G-15 · CU-A-19 · CU-B-17";
      Font f = Tema.Fuente(9f, FontStyle.Bold);
      TextRenderer.DrawText(g, t, f, new Point(x, y), Tema.Texto2,
        TextFormatFlags.NoPadding);
      int fin = x + TextRenderer.MeasureText(t, f, Size.Empty,
        TextFormatFlags.NoPadding).Width;
      using (SolidBrush b = new SolidBrush(Naranja))
        g.FillEllipse(b, fin + 6, y + 6, 7, 7);
    }

    // Aviso de datos sin guardar en la pantalla actual.
    static Control Aviso(string pantalla)
    {
      string texto = "Si cerrás la sesión ahora, los datos que estabas " +
        "cargando en «" + pantalla + "» no se guardarán en el sistema.";
      Font f = Tema.Fuente(10f);
      int alto = 46 + Alto(texto, f, Util - 66);
      return new Dibujo(alto, g =>
      {
        FondoAviso(g, alto);
        Iconos.Alerta(g, 22, 15, 18, Tema.Advertencia);
        TextRenderer.DrawText(g, "Tenés cambios sin guardar en el " +
          "formulario actual", Tema.Fuente(10.5f, FontStyle.Bold),
          new Point(50, 14), Tema.Texto, TextFormatFlags.NoPadding);
        TextRenderer.DrawText(g, texto, f, new Rectangle(50, 38, Util - 66,
          alto), Tema.Texto2, Ajustar);
      });
    }

    static void FondoAviso(Graphics g, int alto)
    {
      Caja(g, new RectangleF(0, 0, Util, alto), AvisoFondo);
      using (SolidBrush b = new SolidBrush(Tema.Advertencia))
        g.FillRectangle(b, 0, 0, 5, alto);
    }

    static void Caja(Graphics g, RectangleF r, Color fondo)
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (GraphicsPath p = Iconos.Redondeado(r, 8))
      using (SolidBrush b = new SolidBrush(fondo)) g.FillPath(b, p);
    }
  }

  /// <summary>Parte de la ventana dibujada a mano, de alto fijo.</summary>
  [DesignerCategory("Code")]
  class Dibujo : Control
  {
    readonly Action<Graphics> pintar;

    public Dibujo(int alto, Action<Graphics> pintar)
    {
      this.pintar = pintar;
      Height = alto;
      SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
        ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
        true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      e.Graphics.Clear(Tema.Superficie);
      pintar(e.Graphics);
    }
  }
}
