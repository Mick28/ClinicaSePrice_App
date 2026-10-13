using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SePrice.UI
{
  /// <summary>
  /// Botón con el ícono de salida (CU-G-15). Variante suave para el
  /// encabezado de todas las pantallas y roja para confirmar el cierre.
  /// </summary>
  [DesignerCategory("Code")]
  class BotonSalir : Button
  {
    readonly bool peligro;
    bool encima;

    public BotonSalir(string texto, bool peligro)
    {
      this.peligro = peligro;
      Name = peligro ? "btnConfirmarCierre" : "btnCerrarSesion";
      Text = texto;
      Font = Tema.Fuente(10.5f, peligro ? FontStyle.Bold : FontStyle.Regular);
      Cursor = Cursors.Hand;
      FlatStyle = FlatStyle.Flat;
      FlatAppearance.BorderSize = 0;
      Size = new Size(TextRenderer.MeasureText(texto, Font).Width + 64, 42);
      SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
        ControlStyles.OptimizedDoubleBuffer, true);
    }

    Color Fondo
    {
      get
      {
        if (peligro) return encima ? Color.FromArgb(166, 31, 31) : Tema.Error;
        return encima ? Tema.PrimarioSuave : Color.FromArgb(241, 244, 249);
      }
    }

    protected override void OnMouseEnter(System.EventArgs e)
    {
      encima = true; Invalidate(); base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(System.EventArgs e)
    {
      encima = false; Invalidate(); base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      Graphics g = e.Graphics;
      g.Clear(Parent != null ? Parent.BackColor : Tema.Superficie);
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (GraphicsPath p = Iconos.Redondeado(new RectangleF(0.5f, 0.5f,
        Width - 1.5f, Height - 1.5f), 8))
      using (SolidBrush b = new SolidBrush(Fondo)) g.FillPath(b, p);
      Color tinta = peligro ? Color.White : Tema.Texto;
      Iconos.Salir(g, 16, (Height - 18) / 2f, 18, tinta);
      TextRenderer.DrawText(g, Text, Font, new Rectangle(42, 0, Width - 52,
        Height), tinta, TextFormatFlags.VerticalCenter |
        TextFormatFlags.Left | TextFormatFlags.NoPadding);
    }
  }
}
