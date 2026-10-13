using System;
using System.Collections;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SePrice.UI
{
  /// <summary>
  /// Tipo de botón (especificación 2.4): uno primario por pantalla.
  /// </summary>
  enum TipoBoton { Primario, Secundario, Peligro, Enlace }

  /// <summary>
  /// Tipo de aviso en línea (2.4): info, advertencia, error, éxito.
  /// </summary>
  enum TipoAviso { Info, Advertencia, Error, Exito }

  /// <summary>
  /// Fábrica de controles con el estilo de la especificación. Los nombres
  /// siguen los prefijos de la comunidad: btn, txt, cbo, chk, lbl, tbl.
  /// </summary>
  static partial class Ui
  {
    /// <summary>
    /// Título de pantalla (uno solo, = nombre del caso de uso).
    /// </summary>
    public static Parrafo Titulo(string texto)
    {
      return new Parrafo { Text = texto, ForeColor = Tema.Texto,
        Font = Tema.Fuente(17f, FontStyle.Bold) };
    }

    /// <summary>Subtítulo de sección.</summary>
    public static Parrafo Subtitulo(string texto)
    {
      return new Parrafo { Text = texto, ForeColor = Tema.Texto,
        Font = Tema.Fuente(12.5f, FontStyle.Bold) };
    }

    /// <summary>Texto común.</summary>
    public static Parrafo Texto(string texto, float puntos = 10f)
    {
      return new Parrafo { Text = texto, ForeColor = Tema.Texto,
        Font = Tema.Fuente(puntos) };
    }

    /// <summary>Ayuda o nota (gris).</summary>
    public static Parrafo Nota(string texto)
    {
      return new Parrafo { Text = texto, ForeColor = Tema.Texto2,
        Font = Tema.Fuente(9f) };
    }

    /// <summary>
    /// Ancho máximo de un control (su alto queda libre). En .NET un alto
    /// máximo 0 deja el control con alto 0 (invisible): se usa un tope alto.
    /// </summary>
    public static Size Tope(int ancho)
    {
      return new Size(ancho, 10000);
    }

    /// <summary>Botón con el estilo del tipo pedido.</summary>
    public static Button Boton(string texto, TipoBoton tipo,
      EventHandler clic)
    {
      Button b = new Button { Name = "btn" + Nombre(texto), Text = texto,
        FlatStyle = FlatStyle.Flat, Height = 38, Cursor = Cursors.Hand,
        UseVisualStyleBackColor = false,
        Font = Tema.Fuente(9.75f, FontStyle.Bold) };
      Pintar(b, tipo);
      b.Width = TextRenderer.MeasureText(texto, b.Font).Width + 36;
      b.MaximumSize = Ui.Tope(b.Width);
      if (clic != null) b.Click += clic;
      return b;
    }

    // «Registrar ingreso» → «RegistrarIngreso» (prefijo btn, material S12).
    static string Nombre(string texto)
    {
      string n = "";
      foreach (string p in texto.Split(' '))
      {
        string l = new string(p.Where(char.IsLetterOrDigit).ToArray());
        if (l.Length > 0) n += char.ToUpper(l[0]) + l.Substring(1);
      }
      return n;
    }

    static void Pintar(Button b, TipoBoton tipo)
    {
      Color fondo = tipo == TipoBoton.Primario ? Tema.Primario
        : tipo == TipoBoton.Peligro ? Tema.Error : Tema.Superficie;
      b.BackColor = fondo;
      b.ForeColor = tipo == TipoBoton.Primario || tipo == TipoBoton.Peligro
        ? Color.White : tipo == TipoBoton.Enlace ? Tema.Primario
        : Tema.Primario;
      b.FlatAppearance.BorderColor = tipo == TipoBoton.Enlace
        ? Tema.Superficie : tipo == TipoBoton.Secundario ? Tema.Primario
        : fondo;
      b.FlatAppearance.BorderSize = 1;
    }

    /// <summary>Fila de botones alineada a la derecha.</summary>
    public static FlowLayoutPanel Botones(params Control[] botones)
    {
      FlowLayoutPanel f = new FlowLayoutPanel { Height = 44,
        FlowDirection = FlowDirection.RightToLeft, WrapContents = true };
      f.Layout += (s, e) => Crecer(f);
      for (int i = botones.Length - 1; i >= 0; i--)
        if (botones[i] != null) f.Controls.Add(botones[i]);
      return f;
    }

    // Si los botones no entran en una línea, pasan a la siguiente y la
    // fila crece (nunca se achica por debajo de la altura fijada).
    static void Crecer(FlowLayoutPanel f)
    {
      int alto = 0;
      foreach (Control c in f.Controls)
        if (c.Visible) alto = Math.Max(alto, c.Bottom + c.Margin.Bottom);
      if (alto + f.Padding.Bottom > f.Height)
        f.Height = alto + f.Padding.Bottom;
    }

    /// <summary>Fila de botones alineada a la izquierda.</summary>
    public static FlowLayoutPanel BotonesIzq(params Control[] botones)
    {
      FlowLayoutPanel f = Botones();
      f.FlowDirection = FlowDirection.LeftToRight;
      foreach (Control b in botones) if (b != null) f.Controls.Add(b);
      return f;
    }

    /// <summary>Caja de texto (txt).</summary>
    public static TextBox Caja(string nombre, bool clave = false)
    {
      TextBox t = new TextBox { Name = "txt" + nombre,
        Font = Tema.Fuente(10.5f), BorderStyle = BorderStyle.FixedSingle };
      if (clave) t.UseSystemPasswordChar = true;
      return t;
    }

    /// <summary>Opción inicial de las listas: «Seleccioná…» (2.4).</summary>
    public static readonly object Seleccionar = new Inicial();

    class Inicial
    {
      public override string ToString() { return "Seleccioná…"; }
    }

    /// <summary>
    /// Lista desplegable (cbo). Con «inicial», arranca en «Seleccioná…», que
    /// cuenta como vacío.
    /// </summary>
    public static ComboBox Combo(string nombre, IEnumerable items = null,
      bool inicial = true)
    {
      ComboBox c = new ComboBox { Name = "cbo" + nombre,
        DropDownStyle = ComboBoxStyle.DropDownList, Font = Tema.Fuente(10.5f),
        FlatStyle = FlatStyle.Flat };
      if (inicial) c.Items.Add(Seleccionar);
      if (items != null) foreach (object o in items) c.Items.Add(o);
      if (inicial) c.SelectedIndex = 0;
      c.DropDown += (s, e) => Ensanchar(c);
      return c;
    }

    // La lista desplegada se ensancha para mostrar la opción más larga.
    static void Ensanchar(ComboBox c)
    {
      int ancho = c.Width;
      foreach (object o in c.Items)
        ancho = Math.Max(ancho, TextRenderer.MeasureText(o.ToString(),
          c.Font).Width + 24);
      c.DropDownWidth = Math.Min(ancho, 700);
    }

    /// <summary>Texto elegido, o null si sigue en «Seleccioná…».</summary>
    public static string Elegido(ComboBox c)
    {
      return c.SelectedItem == null || c.SelectedItem == Seleccionar ? null
        : c.Text;
    }

    /// <summary>Selector de fecha (dd/mm/aaaa).</summary>
    static DateTimePicker ConFormato(DateTimePicker d)
    {
      d.Format = DateTimePickerFormat.Custom;
      d.CustomFormat = "dd/MM/yyyy";
      return d;
    }

    /// <summary>Selector de fecha (dd/mm/aaaa).</summary>
    public static DateTimePicker Fecha(string nombre, DateTime valor)
    {
      return ConFormato(new DateTimePicker { Name = "dtp" + nombre,
        Value = valor, Font = Tema.Fuente(10.5f) });
    }

    /// <summary>Número entero con mínimo y máximo.</summary>
    public static NumericUpDown Numero(string nombre, int min, int max,
      int valor)
    {
      return new NumericUpDown { Name = "num" + nombre, Minimum = min,
        Maximum = max, Value = Math.Max(min, Math.Min(max, valor)),
        Font = Tema.Fuente(10.5f) };
    }

    /// <summary>Casilla (chk) con la etiqueta a la derecha.</summary>
    public static CheckBox Casilla(string nombre, string texto)
    {
      return new CheckBox { Name = "chk" + nombre, Text = texto, Height = 30,
        Font = Tema.Fuente(10f), ForeColor = Tema.Texto };
    }

    /// <summary>Opción (opt) de un grupo.</summary>
    public static RadioButton Opcion(string nombre, string texto)
    {
      return new RadioButton { Name = "opt" + nombre, Text = texto,
        AutoSize = true, Font = Tema.Fuente(10f), ForeColor = Tema.Texto,
        Margin = new Padding(0, 4, 24, 0) };
    }

    /// <summary>
    /// Etiqueta fija de sólo lectura (lbl, fondo gris claro).
    /// </summary>
    public static Label Fijo(string nombre, string valor)
    {
      return new Label { Name = "lbl" + nombre, Text = valor ?? "—",
        BackColor = Tema.Fondo, ForeColor = Tema.Texto, AutoSize = false,
        Height = 30, TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(8, 0, 0, 0), Font = Tema.Fuente(10.5f) };
    }

    /// <summary>Grupo horizontal de opciones.</summary>
    public static FlowLayoutPanel Grupo(params RadioButton[] opciones)
    {
      FlowLayoutPanel f = new FlowLayoutPanel { Height = 32,
        WrapContents = false };
      f.Controls.AddRange(opciones);
      return f;
    }

    /// <summary>Fila de controles en partes iguales.</summary>
    public static Fila EnFila(params Control[] controles)
    {
      Fila f = new Fila();
      foreach (Control c in controles) if (c != null) f.Controls.Add(c);
      return f;
    }
  }
}
