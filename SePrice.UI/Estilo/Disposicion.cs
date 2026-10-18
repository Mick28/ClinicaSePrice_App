using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SePrice.UI
{
  /// <summary>
  /// Apila los controles de arriba abajo, a lo ancho del panel, y ajusta su
  /// propia altura al contenido. Un control con MaximumSize.Width conserva
  /// ese ancho como máximo.
  /// </summary>
  [DesignerCategory("Code")]
  class Columna : Panel
  {
    /// <summary>Separación vertical entre controles (grilla de 8 px).</summary>
    public int Espacio = 12;

    public Columna()
    {
      DoubleBuffered = true;
    }

    /// <summary>
    /// Se fija posición y ancho; la altura la decide el control.
    /// </summary>
    public const BoundsSpecified PosicionYAncho = BoundsSpecified.Location |
      BoundsSpecified.Width;

    protected override void OnLayout(LayoutEventArgs e)
    {
      base.OnLayout(e);
      int y = Padding.Top, ancho = ClientSize.Width - Padding.Horizontal;
      foreach (Control c in Controls)
      {
        if (!c.Visible) continue;
        int w = c.MaximumSize.Width > 0 ? Math.Min(ancho, c.MaximumSize.Width)
          : ancho;
        c.SetBounds(Padding.Left, y, Math.Max(10, w), 0, PosicionYAncho);
        y += c.Height + Espacio;
      }
      int alto = Math.Max(y - Espacio, Padding.Top) + Padding.Bottom;
      if (Height != alto) Height = alto;
    }
  }

  /// <summary>
  /// Ubica los controles de izquierda a derecha en partes iguales (o con su
  /// ancho, si tienen MaximumSize.Width). Su altura es la del más alto.
  /// </summary>
  [DesignerCategory("Code")]
  class Fila : Panel
  {
    public int Espacio = 16;

    protected override void OnLayout(LayoutEventArgs e)
    {
      base.OnLayout(e);
      int fijos = 0, libres = 0, x = 0, alto = 0;
      foreach (Control c in Controls)
        if (c.MaximumSize.Width > 0) fijos += c.MaximumSize.Width;
        else libres++;
      int resto = ClientSize.Width - fijos - Espacio * (Controls.Count - 1);
      int parte = libres == 0 ? 0 : Math.Max(40, resto / libres);
      foreach (Control c in Controls)
      {
        int w = c.MaximumSize.Width > 0 ? c.MaximumSize.Width : parte;
        c.SetBounds(x, 0, w, 0, Columna.PosicionYAncho);
        x += w + Espacio;
        alto = Math.Max(alto, c.Height);
      }
      if (Height != alto) Height = alto;
    }
  }

  /// <summary>
  /// Texto que se ajusta al ancho y calcula su propia altura.
  /// </summary>
  [DesignerCategory("Code")]
  class Parrafo : Label
  {
    public Parrafo()
    {
      AutoSize = false;
      Height = 24;
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);
      Ajustar();
    }

    protected override void OnTextChanged(EventArgs e)
    {
      base.OnTextChanged(e);
      Ajustar();
    }

    void Ajustar()
    {
      if (Width < 20) return;
      Size s = TextRenderer.MeasureText(Text + " ", Font, new Size(
        Width - Padding.Horizontal, int.MaxValue), TextFormatFlags.WordBreak);
      int alto = s.Height + Padding.Vertical + 2;
      if (Height != alto) Height = alto;
    }
  }

  /// <summary>Tarjeta blanca con borde y esquinas suaves (2.3).</summary>
  [DesignerCategory("Code")]
  class Tarjeta : Columna
  {
    public Color ColorBorde = Tema.Borde;

    /// <summary>Radio de las esquinas, en píxeles.</summary>
    public int Radio = 10;

    public Tarjeta()
    {
      BackColor = Tema.Superficie;
      Padding = new Padding(20, 16, 20, 16);
      ResizeRedraw = true;
    }

    // Recorta el panel con la forma redondeada (las esquinas no se pintan).
    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);
      if (Width <= Radio * 2 || Height <= Radio * 2) return;
      Region anterior = Region;
      using (GraphicsPath f = Forma(new Rectangle(0, 0, Width, Height)))
        Region = new Region(f);
      if (anterior != null) anterior.Dispose();
    }

    // Dibuja el borde con la misma forma, suavizado.
    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      if (Width <= Radio * 2 || Height <= Radio * 2) return;
      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
      using (Pen p = new Pen(ColorBorde))
      using (GraphicsPath f = Forma(new Rectangle(0, 0, Width - 1,
        Height - 1)))
        e.Graphics.DrawPath(p, f);
    }

    // Rectángulo con las cuatro esquinas redondeadas.
    GraphicsPath Forma(Rectangle r)
    {
      int d = Radio * 2;
      GraphicsPath g = new GraphicsPath();
      g.AddArc(r.X, r.Y, d, d, 180, 90);
      g.AddArc(r.Right - d, r.Y, d, d, 270, 90);
      g.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
      g.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
      g.CloseFigure();
      return g;
    }
  }
}
