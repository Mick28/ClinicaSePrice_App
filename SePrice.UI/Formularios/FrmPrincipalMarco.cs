using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class FrmPrincipal
  {
    // ---------- Encabezado (cmp_encabezado) ----------

    // Logo a la izquierda; a la derecha, siempre visibles, los datos de la
    // sesión (si no entran, se acortan), el avatar y «Cerrar sesión».
    Control Encabezado()
    {
      Panel e = new Panel { Dock = DockStyle.Top, Height = 68,
        BackColor = Tema.Superficie, Padding = new Padding(24, 8, 24, 8) };
      e.Paint += (s, a) => a.Graphics.DrawLine(new Pen(Tema.Borde), 0,
        e.Height - 1, e.Width, e.Height - 1);
      Button salir = new BotonSalir("Cerrar sesión", false);
      salir.Click += (s, a) => CerrarSesion();
      // Orden de acople: lo último agregado se ubica primero.
      e.Controls.Add(DatosSesion());
      e.Controls.Add(Centrado(Avatar(), 12));
      e.Controls.Add(Centrado(salir, 16));
      e.Controls.Add(Logo());
      return e;
    }

    // Caja anclada a la derecha que centra el control en la altura.
    static Control Centrado(Control c, int separacion)
    {
      Panel p = new Panel { Dock = DockStyle.Right,
        Width = c.Width + separacion };
      p.Resize += (s, a) => c.Location = new Point(separacion,
        (p.Height - c.Height) / 2);
      p.Controls.Add(c);
      return p;
    }

    static Control Logo()
    {
      PictureBox p = new PictureBox { Dock = DockStyle.Left, Width = 170,
        SizeMode = PictureBoxSizeMode.Zoom };
      string ruta = Path.Combine(Application.StartupPath, "logo.png");
      if (File.Exists(ruta)) p.Image = Image.FromFile(ruta);
      return p;
    }

    /// <summary>«Martín Ruiz» a partir de «Ruiz, Martín».</summary>
    public static string NombreVisible(Usuario u)
    {
      int coma = u.Nombre.IndexOf(',');
      return coma < 0 ? u.Nombre : u.Nombre.Substring(coma + 1).Trim() +
        " " + u.Nombre.Substring(0, coma);
    }

    // «Martín Ruiz» / «Administrador/a del sistema · Sesión desde 07:45»
    static Control DatosSesion()
    {
      Usuario u = Sesion.Actual;
      return new Label { Dock = DockStyle.Fill, AutoEllipsis = true,
        ForeColor = Tema.Texto, Font = Tema.Fuente(9.5f),
        TextAlign = ContentAlignment.MiddleRight, Text = NombreVisible(u) +
        "\n" + Permisos.Nombres[u.Perfil] + " · Sesión desde " +
        Formato.Hora(Sesion.Inicio) };
    }

    static Control Avatar()
    {
      string n = Sesion.Actual.PrimerNombre;
      return new Label { Text = n.Substring(0, 1).ToUpperInvariant(),
        Size = new Size(36, 36), BackColor = Tema.Primario,
        ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter,
        Font = Tema.Fuente(12f, FontStyle.Bold) };
    }

    // ---------- Menú lateral (cmp_menu): sólo las funciones del perfil

    Control MenuLateral()
    {
      Columna m = new Columna { Espacio = 2, BackColor = Tema.Superficie,
        Padding = new Padding(12, 16, 12, 16) };
      Panel marco = new Panel { Dock = DockStyle.Left, Width = 260,
        BackColor = Tema.Superficie, AutoScroll = true };
      marco.Paint += (s, a) => a.Graphics.DrawLine(new Pen(Tema.Borde),
        marco.Width - 1, 0, marco.Width - 1, marco.Height);
      m.Controls.Add(Marca());
      m.Controls.Add(Item("P-01", "⌂  Inicio"));
      AgregarSecciones(m);
      marco.Controls.Add(m);
      marco.Resize += (s, a) => m.Width = marco.ClientSize.Width - 1;
      m.Width = 259;
      return marco;
    }

    static Control Marca()
    {
      Columna c = new Columna { Espacio = 0, Padding = new Padding(4, 0, 0,
        12) };
      c.Controls.Add(Ui.Subtitulo("Clínica SePrice"));
      c.Controls.Add(Ui.Nota(Permisos.Nombres[Sesion.Actual.Perfil]));
      return c;
    }

    // Una sección sin ítems para el perfil no se muestra (3.1).
    void AgregarSecciones(Columna m)
    {
      string seccion = null;
      foreach (ItemMenu i in Navegacion.Menu(Sesion.Actual.Perfil))
      {
        if (i.Seccion != seccion) m.Controls.Add(Seccion(i.Seccion));
        seccion = i.Seccion;
        m.Controls.Add(Item(i.Codigo, i.Texto));
      }
    }

    static Control Seccion(string nombre)
    {
      string texto = nombre == Navegacion.A ? "CONSULTORIOS" : nombre;
      Color color = nombre == Navegacion.A ? Tema.CircuitoA
        : nombre == Navegacion.B ? Tema.CircuitoB : Tema.Texto;
      Fila f = Ui.EnFila(new Parrafo
      {
        Text = texto,
        ForeColor = color,
        Font = Tema.Fuente(8.5f, FontStyle.Bold),
        Padding = new Padding(4, 14, 0, 0)
      });
      if (nombre != Navegacion.Comun && Configuracion.MostrarCodigos)
      {
        Label l = Ui.Circuito(nombre == Navegacion.A ? "A" : "B");
        l.Text = nombre == Navegacion.A ? "Circuito A" : "Circuito B";
        l.MaximumSize = Ui.Tope(78);
        f.Controls.Add(l);
      }
      return f;
    }

    Button Item(string codigo, string texto)
    {
      Button b = new Button { Text = "   " + texto, Height = 38,
        FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
        Font = Tema.Fuente(10f), ForeColor = Tema.Texto, Cursor =
        Cursors.Hand, BackColor = Tema.Superficie };
      b.FlatAppearance.BorderSize = 0;
      b.Click += (s, e) => Abrir(codigo);
      items[codigo] = b;
      return b;
    }

    // Ítem activo: fondo primario (como «Inicio» en la captura).
    void Activar(string codigo, bool auditoria)
    {
      if (auditoria) codigo = "P-27";
      foreach (var i in items)
      {
        bool activo = i.Key == codigo;
        i.Value.BackColor = activo ? Tema.Primario : Tema.Superficie;
        i.Value.ForeColor = activo ? Color.White : Tema.Texto;
        i.Value.Font = Tema.Fuente(10f, activo ? FontStyle.Bold
          : FontStyle.Regular);
      }
    }
  }
}
