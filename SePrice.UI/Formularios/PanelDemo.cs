using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Accesos rápidos de prueba y reloj de la demostración. Sólo aparece con
  /// ModoDemo=si en SePrice.config.txt (en producción no se muestra).
  /// </summary>
  [DesignerCategory("Code")]
  class PanelDemo : Columna
  {
    static readonly string[,] usuarios = {
      { "lgomez", "Recepción" }, { "sibarra", "Médica" },
      { "mruiz", "Admin" }, { "cmendez", "Coordinación" },
      { "tvera", "Técnico" }, { "nrios", "Guardia" },
      { "dacosta", "Insumos" }, { "gfunes", "Jefatura" } };

    readonly FrmLogin login;
    readonly Label reloj = new Label { AutoSize = true,
      Font = Tema.Fuente(9f), ForeColor = Tema.Texto2 };

    public PanelDemo(FrmLogin login)
    {
      this.login = login;
      Espacio = 8;
      Padding = new Padding(0, 10, 0, 0);
      Controls.Add(Ui.EnFila(Ui.Texto("▣  Accesos rápidos de prueba:", 9f),
        Derecha(Ui.Nota("Clic para autocompletar"))));
      Controls.Add(Chips(0));
      Controls.Add(Chips(4));
      Controls.Add(Ui.Nota("Contraseña común: SePrice2026"));
      Controls.Add(RelojDemo());
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      using (Pen p = new Pen(Tema.Borde)) e.Graphics.DrawLine(p, 0, 0,
        Width, 0);
    }

    static Parrafo Derecha(Parrafo p)
    {
      p.TextAlign = ContentAlignment.TopRight;
      p.Font = Tema.Fuente(8.5f, FontStyle.Italic);
      return p;
    }

    Fila Chips(int desde)
    {
      Fila f = new Fila { Espacio = 6 };
      for (int i = desde; i < desde + 4; i++)
        f.Controls.Add(Chip(usuarios[i, 0], usuarios[i, 1]));
      return f;
    }

    Button Chip(string usuario, string perfil)
    {
      Button b = new Button { Text = usuario + "\n" + perfil, Height = 46,
        FlatStyle = FlatStyle.Flat, BackColor = Tema.Fondo,
        ForeColor = Tema.Texto, TextAlign = ContentAlignment.MiddleLeft,
        Font = Tema.Fuente(8.5f), Cursor = Cursors.Hand };
      b.FlatAppearance.BorderColor = Tema.Borde;
      b.Click += (s, e) => login.Autocompletar(usuario);
      return b;
    }

    // Reloj de la demostración: permite probar franjas y tolerancias.
    Control RelojDemo()
    {
      Button cambiar = Ui.Boton("Cambiar hora", TipoBoton.Enlace, (s, e) =>
        CambiarHora());
      Timer t = new Timer { Interval = 1000, Enabled = true };
      Tag = t;   // referencia: que el recolector no lo detenga
      t.Tick += (s, e) => Mostrar();
      Mostrar();
      return Ui.BotonesIzq(reloj, cambiar);
    }

    void Mostrar()
    {
      reloj.Text = "🕘 Demo: " +
        Formato.Corta(Negocio.Reloj.Ahora) + " " +
        Formato.Hora(Negocio.Reloj.Ahora);
      reloj.Margin = new Padding(0, 10, 4, 0);
    }

    void CambiarHora()
    {
      string h = Dialogo.Ingresar("Elegí la hora de la demostración.",
        "Reloj de demostración", new[] { "07:30", "08:50", "09:40",
        "10:20", "11:30", "17:30" });
      TimeSpan hora;
      if (h != null && TimeSpan.TryParse(h, out hora))
        Negocio.Reloj.Fijar(Negocio.Reloj.Hoy + hora);
      Mostrar();
    }
  }
}
