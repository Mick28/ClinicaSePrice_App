using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SePrice.UI
{
  /// <summary>
  /// Íconos de línea dibujados con el mismo trazo que la especificación de
  /// interfaz (sin imágenes externas, se ven nítidos en cualquier pantalla).
  /// Cada método dibuja dentro del cuadrado de lado «l» que empieza en (x, y).
  /// </summary>
  static class Iconos
  {
    static Pen Trazo(Color c, float l)
    {
      return new Pen(c, Math.Max(1.6f, l / 10f)) { StartCap = LineCap.Round,
        EndCap = LineCap.Round, LineJoin = LineJoin.Round };
    }

    /// <summary>Salir: marco abierto y flecha hacia afuera.</summary>
    public static void Salir(Graphics g, float x, float y, float l, Color c)
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (Pen p = Trazo(c, l))
      {
        g.DrawLines(p, new[] { new PointF(x + l * .45f, y + l * .12f),
          new PointF(x + l * .12f, y + l * .12f), new PointF(x + l * .12f,
          y + l * .88f), new PointF(x + l * .45f, y + l * .88f) });
        g.DrawLine(p, x + l * .38f, y + l * .5f, x + l * .9f, y + l * .5f);
        g.DrawLines(p, new[] { new PointF(x + l * .7f, y + l * .3f),
          new PointF(x + l * .9f, y + l * .5f), new PointF(x + l * .7f,
          y + l * .7f) });
      }
    }

    /// <summary>Reloj: círculo con agujas.</summary>
    public static void Reloj(Graphics g, float x, float y, float l, Color c)
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (Pen p = Trazo(c, l))
      {
        g.DrawEllipse(p, x + l * .08f, y + l * .08f, l * .84f, l * .84f);
        g.DrawLines(p, new[] { new PointF(x + l * .5f, y + l * .26f),
          new PointF(x + l * .5f, y + l * .5f), new PointF(x + l * .68f,
          y + l * .62f) });
      }
    }

    /// <summary>Candado.</summary>
    public static void Candado(Graphics g, float x, float y, float l, Color c)
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (Pen p = Trazo(c, l))
      {
        g.DrawRectangle(p, x + l * .18f, y + l * .45f, l * .64f, l * .45f);
        g.DrawArc(p, x + l * .3f, y + l * .1f, l * .4f, l * .6f, 180, 180);
      }
    }

    /// <summary>Escudo con tilde (registro hecho).</summary>
    public static void Escudo(Graphics g, float x, float y, float l, Color c)
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (Pen p = Trazo(c, l))
      {
        g.DrawPolygon(p, new[] { new PointF(x + l * .5f, y + l * .06f),
          new PointF(x + l * .88f, y + l * .2f), new PointF(x + l * .82f,
          y + l * .62f), new PointF(x + l * .5f, y + l * .94f), new PointF(
          x + l * .18f, y + l * .62f), new PointF(x + l * .12f, y + l * .2f) });
        g.DrawLines(p, new[] { new PointF(x + l * .34f, y + l * .5f),
          new PointF(x + l * .46f, y + l * .62f), new PointF(x + l * .68f,
          y + l * .38f) });
      }
    }

    /// <summary>Advertencia: triángulo con signo de exclamación.</summary>
    public static void Alerta(Graphics g, float x, float y, float l, Color c)
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (SolidBrush b = new SolidBrush(c))
      using (Pen p = new Pen(Color.White, Math.Max(1.6f, l / 9f)))
      {
        g.FillPolygon(b, new[] { new PointF(x + l * .5f, y + l * .06f),
          new PointF(x + l * .96f, y + l * .9f), new PointF(x + l * .04f,
          y + l * .9f) });
        g.DrawLine(p, x + l * .5f, y + l * .36f, x + l * .5f, y + l * .6f);
        g.FillEllipse(Brushes.White, x + l * .44f, y + l * .68f, l * .12f,
          l * .12f);
      }
    }

    /// <summary>Rectángulo de esquinas redondeadas (radio «r»).</summary>
    public static GraphicsPath Redondeado(RectangleF a, float r)
    {
      GraphicsPath p = new GraphicsPath();
      float d = r * 2;
      p.AddArc(a.X, a.Y, d, d, 180, 90);
      p.AddArc(a.Right - d, a.Y, d, d, 270, 90);
      p.AddArc(a.Right - d, a.Bottom - d, d, d, 0, 90);
      p.AddArc(a.X, a.Bottom - d, d, d, 90, 90);
      p.CloseFigure();
      return p;
    }
  }
}
