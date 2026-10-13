using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Ventana «¿Querés cerrar la sesión?» (CU-G-15 · CU-A-19 · CU-B-17), la
  /// misma para todos los perfiles. Muestra quién tiene la sesión abierta,
  /// desde cuándo y, si la pantalla actual tiene datos sin guardar, lo
  /// avisa antes. Esc vuelve a trabajar; Enter confirma el cierre.
  /// </summary>
  [DesignerCategory("Code")]
  partial class DlgCerrarSesion : Form
  {
    const int Ancho = 600, Margen = 32, Util = Ancho - 2 * Margen;
    readonly Columna cuerpo = new Columna { Espacio = 18,
      Padding = new Padding(Margen, 30, Margen, 22) };

    /// <summary>
    /// Pregunta si se cierra la sesión. «pantallaConCambios» es el nombre de
    /// la pantalla con datos sin guardar, o null si no hay.
    /// </summary>
    public static bool Confirmar(string pantallaConCambios)
    {
      using (DlgCerrarSesion d = new DlgCerrarSesion(pantallaConCambios))
        return Dialogo.Mostrar(d) == DialogResult.Yes;
    }

    DlgCerrarSesion(string pantalla)
    {
      Text = "Cerrar sesión";
      FormBorderStyle = FormBorderStyle.None;
      StartPosition = FormStartPosition.CenterParent;
      ShowInTaskbar = false;
      BackColor = Tema.Superficie;
      Font = Tema.Fuente(10f);
      Armar(pantalla);
      Controls.Add(cuerpo);
      cuerpo.Paint += Marco;
      cuerpo.Width = Ancho;
      cuerpo.SizeChanged += (s, e) => ClientSize = new Size(Ancho,
        cuerpo.Height);
      Resize += (s, e) => Region = new Region(Iconos.Redondeado(
        new RectangleF(0, 0, Width, Height), 12));
    }

    void Armar(string pantalla)
    {
      cuerpo.Controls.Add(Cabecera());
      if (pantalla != null) cuerpo.Controls.Add(Aviso(pantalla));
      cuerpo.Controls.Add(Tiempo());
      cuerpo.Controls.Add(Acciones(pantalla != null));
      cuerpo.Controls.Add(Atajos());
      cuerpo.Controls.Add(Pie());
    }

    // Franja superior del color primario y borde fino (ventana sin marco).
    static void Marco(object s, PaintEventArgs e)
    {
      Control c = (Control)s;
      Graphics g = e.Graphics;
      g.SmoothingMode = SmoothingMode.AntiAlias;
      using (SolidBrush b = new SolidBrush(Tema.Primario))
        g.FillRectangle(b, 0, 0, c.Width, 6);
      using (Pen p = new Pen(Tema.Borde))
      using (GraphicsPath r = Iconos.Redondeado(new RectangleF(0, 0,
        c.Width - 1, c.Height - 1), 12))
        g.DrawPath(p, r);
    }

    // Botones: «Volver y continuar trabajando» (Esc) y el cierre (Enter).
    Control Acciones(bool hayCambios)
    {
      Panel p = new Panel { Height = 48 };
      Button si = new BotonSalir(hayCambios ? "Cerrar sesión igual"
        : "Cerrar sesión", true) { DialogResult = DialogResult.Yes };
      Button no = Enlace("Volver y continuar trabajando");
      no.DialogResult = DialogResult.No;
      si.Height = 46;
      si.Location = new Point(Util - si.Width, 1);
      no.Location = new Point((Util - si.Width - no.Width) / 2, 1);
      p.Controls.Add(si);
      p.Controls.Add(no);
      AcceptButton = si;
      CancelButton = no;
      Shown += (s, e) => si.Focus();
      return p;
    }

    static Button Enlace(string texto)
    {
      Button b = new Button { Name = "btnVolverATrabajar", Text = texto,
        FlatStyle = FlatStyle.Flat, ForeColor = Tema.Primario, Height = 46,
        Font = Tema.Fuente(10.5f, FontStyle.Bold), Cursor = Cursors.Hand,
        BackColor = Tema.Superficie, UseVisualStyleBackColor = false };
      b.FlatAppearance.BorderSize = 0;
      b.FlatAppearance.MouseOverBackColor = Tema.PrimarioSuave;
      b.Width = TextRenderer.MeasureText(texto, b.Font).Width + 28;
      return b;
    }
  }
}
