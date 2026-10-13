#if MONO
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Sólo para la verificación en Linux (Mono): recorre pantallas y guarda
  /// capturas. Variable SEPRICE_CAPTURA="usuario:P-01,P-27;usuario2:...".
  /// </summary>
  static class Captura
  {
    static string carpeta = "/tmp/claude-0/capturas";

    public static bool Activa
    {
      get { return Environment.GetEnvironmentVariable("SEPRICE_CAPTURA")
        != null; }
    }

    public static void Correr(Form login)
    {
      Directory.CreateDirectory(carpeta);
      Dialogo.Mostrar = Responder;
      string plan = Environment.GetEnvironmentVariable("SEPRICE_CAPTURA");
      Timer t = new Timer { Interval = 1500 };
      reloj = t;
      var pasos = new System.Collections.Generic.Queue<Action>();
      pasos.Enqueue(() => Guardar(login, "00_login"));
      foreach (string bloque in plan.Split(';')) Planificar(bloque, pasos);
      pasos.Enqueue(() => { Console.Out.Flush(); Environment.Exit(0); });
      t.Tick += (s, e) => { if (pasos.Count == 0) return;
        try { pasos.Dequeue()(); }
        catch (Exception x) { Console.WriteLine("ERROR " + x); } };
      t.Start();
    }

    static FrmPrincipal f;
    static Timer reloj;

    // Un bloque «usuario:paso,paso,...»: inicia sesión y encola los pasos.
    static void Planificar(string bloque,
      System.Collections.Generic.Queue<Action> q)
    {
      string[] p = bloque.Split(new[] { ':' }, 2);
      string usuario = p[0];
      q.Enqueue(() => {
        if (f != null && !f.IsDisposed) f.Hide();
        Sesion.Cerrar();
        Sesion.Iniciar(usuario, "SePrice2026");
        f = new FrmPrincipal(); f.Show(); f.Location = new Point(0, 0); });
      foreach (string paso in p[1].Split(',')) Paso(usuario, paso, q);
    }

    // #dni fija el paciente en curso, @hh:mm la hora, x:... es una acción
    // y P-xx (o P-xxa, en auditoría) abre la pantalla y la captura.
    static void Paso(string usuario, string c,
      System.Collections.Generic.Queue<Action> q)
    {
      if (c.StartsWith("#")) q.Enqueue(() => PacienteEnCurso.Actual =
        Negocio.Pacientes.PorDni(int.Parse(c.Substring(1))));
      else if (c.Length > 2 && c[1] == ':') q.Enqueue(() => Accion(c));
      else if (c.StartsWith("@")) q.Enqueue(() => Negocio.Reloj.Fijar(
        Negocio.Reloj.Hoy + TimeSpan.Parse(c.Substring(1))));
      else Pantalla(usuario + "_" + c, c.TrimEnd('a'), c.EndsWith("a"), q);
    }

    static void Pantalla(string nombre, string codigo, bool auditoria,
      System.Collections.Generic.Queue<Action> q)
    {
      q.Enqueue(() => f.Abrir(codigo, null, auditoria));
      q.Enqueue(() => { Console.WriteLine("captura " + nombre);
        Console.Out.Flush(); Guardar(f, nombre); });
      q.Enqueue(() => Desplazar(f, nombre));
    }

    // Acciones de prueba: t:Nombre=texto, s:Nombre=opción, k:Nombre,
    // c:texto del botón, g:Nombre=fila, f:nombre de captura.
    static void Accion(string a)
    {
      Form activo = Form.ActiveForm ?? f;
      foreach (Form o in Application.OpenForms) if (o.Modal) activo = o;
      string arg = a.Substring(2), nom = arg.Split('=')[0];
      string val = arg.Contains("=") ? arg.Substring(arg.IndexOf('=') + 1)
        : null;
      Console.WriteLine("accion " + a + " en " + activo.Text);
      if (a[0] == 'f') { Guardar(activo, arg); return; }
      if (a[0] == 'c') { Boton(activo, arg).PerformClick(); return; }
      Control c = Buscar(activo, nom);
      if (a[0] == 't') c.Text = val;
      if (a[0] == 'k') ((ButtonBase)c).GetType().GetProperty("Checked")
        .SetValue(c, true);
      if (a[0] == 's') Elegir((ComboBox)c, val);
      if (a[0] == 'g') ((DataGridView)c).CurrentCell = ((DataGridView)c)
        .Rows[int.Parse(val)].Cells[1];
    }

    static void Elegir(ComboBox c, string val)
    {
      for (int i = 0; i < c.Items.Count; i++)
        if (c.Items[i].ToString().StartsWith(val)) { c.SelectedIndex = i;
          return; }
      Console.WriteLine("  (no hay opción " + val + ")");
    }

    static Control Buscar(Control raiz, string nombre)
    {
      foreach (Control c in Todos(raiz))
        if (c.Name == nombre && c.Visible) return c;
      throw new Exception("No encontré " + nombre);
    }

    static Button Boton(Control raiz, string texto)
    {
      Button ultimo = null;
      foreach (Control c in Todos(raiz))
        if (c is Button && c.Visible && c.Text.Trim() == texto)
          return (Button)c;
      foreach (Control c in Todos(raiz))
        if (c is Button && c.Visible && c.Text.Contains(texto))
          ultimo = (Button)c;
      if (ultimo == null) throw new Exception("No encontré el botón " + texto);
      return ultimo;
    }

    static int dialogos;

    // Muestra el diálogo sin bloquear, lo captura y responde «Sí/Aceptar»
    // (en «Ingresar», la primera opción o una contraseña de prueba).
    static DialogResult Responder(Form v)
    {
      v.Show();
      Completar(v);
      for (int i = 0; i < 25; i++)
      { Application.DoEvents(); System.Threading.Thread.Sleep(20); }
      string nombre = "dlg_" + (++dialogos).ToString("00");
      Console.WriteLine("dialogo " + nombre + ": " + v.Text);
      Guardar(v, nombre);
      DialogResult r = ((Button)v.AcceptButton).DialogResult;
      v.DialogResult = r;
      v.Hide();
      return r;
    }

    static void Completar(Form v)
    {
      foreach (Control c in Todos(v))
      {
        if (c is ComboBox && ((ComboBox)c).Items.Count > 0)
          ((ComboBox)c).SelectedIndex = 0;
        if (c is TextBox) c.Text = "Clave2026x";
      }
    }

    static void Desplazar(Form f, string nombre)
    {
      foreach (Control c in Todos(f))
        if (c is Panel && ((Panel)c).AutoScroll && c.Dock == DockStyle.Fill
          && ((Panel)c).VerticalScroll.Visible)
        {
          ((Panel)c).AutoScrollPosition = new Point(0, 100000);
          Application.DoEvents();
          Guardar(f, nombre + "_fin");
          ((Panel)c).AutoScrollPosition = new Point(0, 0);
          Application.DoEvents();
        }
    }

    static System.Collections.Generic.IEnumerable<Control> Todos(Control c)
    {
      foreach (Control h in c.Controls)
      {
        yield return h;
        foreach (Control n in Todos(h)) yield return n;
      }
    }

    public static void Guardar(Form f, string nombre)
    {
      Rectangle r = Screen.PrimaryScreen.Bounds;
      Bitmap b = new Bitmap(r.Width, r.Height);
      using (Graphics g = Graphics.FromImage(b))
        g.CopyFromScreen(0, 0, 0, 0, r.Size);
      Rectangle z = Rectangle.Intersect(r, f.Bounds);
      b.Clone(z, b.PixelFormat).Save(Path.Combine(carpeta, nombre + ".png"));
    }
  }
}
#endif
