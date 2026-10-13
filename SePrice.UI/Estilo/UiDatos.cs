using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SePrice.UI
{
  static partial class Ui
  {
    /// <summary>
    /// Tabla (tbl) de sólo lectura: encabezado fijo, filas de 34 px, cebra
    /// suave, estados con color y texto. Oculta la columna «Id».
    /// </summary>
    public static DataGridView Tabla(string nombre, DataTable datos,
      int maxFilas = 10)
    {
      DataGridView g = new DataGridView { Name = "tbl" + nombre,
        ReadOnly = true, AllowUserToAddRows = false, MultiSelect = false,
        AllowUserToDeleteRows = false, RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Tema.Superficie, BorderStyle = BorderStyle.None,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        EnableHeadersVisualStyles = false, GridColor = Tema.Borde,
        AllowUserToResizeRows = false };
      Estilo(g);
      g.CellFormatting += ColorearEstado;
      g.CellFormatting += Formatear;
      g.DataBindingComplete += (s, e) => Ocultar(g);
      g.Resize += (s, e) => Repartir(g);
      Cargar(g, datos, maxFilas);
      return g;
    }

    static void Estilo(DataGridView g)
    {
      g.ColumnHeadersDefaultCellStyle.BackColor = Tema.Fondo;
      g.ColumnHeadersDefaultCellStyle.ForeColor = Tema.Texto2;
      g.ColumnHeadersDefaultCellStyle.Font = Tema.Fuente(9f, FontStyle.Bold);
      g.ColumnHeadersHeightSizeMode =
        DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
      g.ColumnHeadersHeight = 34;
      g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
      g.ScrollBars = ScrollBars.Vertical;
      g.DefaultCellStyle.Font = Tema.Fuente(9.75f);
      g.DefaultCellStyle.ForeColor = Tema.Texto;
      g.DefaultCellStyle.SelectionBackColor = Tema.PrimarioSuave;
      g.DefaultCellStyle.SelectionForeColor = Tema.Texto;
      g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251,
        253);
      g.RowTemplate.Height = 32;
    }

    static void Ocultar(DataGridView g)
    {
      if (g.Columns.Contains("Id")) g.Columns["Id"].Visible = false;
      foreach (DataGridViewColumn c in g.Columns)
        c.FillWeight = Peso(g, c.Index);
      Repartir(g);
      g.ClearSelection();
    }

    // Reparte el ancho de la tabla (sin barra horizontal: la tabla entra
    // en su tarjeta). Si sobra lugar, en proporción; si falta, primero las
    // columnas angostas reciben lo que necesitan y las anchas, el resto.
    static void Repartir(DataGridView g)
    {
      bool barra = g.Controls.OfType<VScrollBar>().Any(v => v.Visible);
      int libre = g.ClientSize.Width - 2 - (barra
        ? SystemInformation.VerticalScrollBarWidth : 0);
      List<DataGridViewColumn> cols = g.Columns.Cast<DataGridViewColumn>()
        .Where(c => c.Visible).OrderBy(c => c.FillWeight).ToList();
      if (libre <= 0 || cols.Count == 0) return;
      float total = cols.Sum(c => c.FillWeight);
      for (int i = 0; i < cols.Count; i++)
      {
        int ancho = total <= libre ? (int)(libre * cols[i].FillWeight / total)
          : (int)Math.Min(cols[i].FillWeight, libre / (cols.Count - i));
        cols[i].Width = Math.Max(30, ancho);
        if (total > libre) libre -= ancho;
      }
    }

    // Ancho que necesita la columna (en píxeles, hasta 320): su texto más
    // largo o la palabra más larga del encabezado.
    static float Peso(DataGridView g, int col)
    {
      DataTable d = g.DataSource as DataTable;
      string nombre = g.Columns[col].DataPropertyName, largo = g.Columns[col]
        .HeaderText.Split(' ').OrderByDescending(x => x.Length).First();
      if (d != null && d.Columns.Contains(nombre))
        foreach (DataRow r in d.Rows.Cast<DataRow>().Take(30))
        {
          string t = Mostrar(nombre, r[nombre]) ?? Convert.ToString(r[nombre]);
          if (t.Length > largo.Length) largo = t;
        }
      int px = TextRenderer.MeasureText(largo, g.DefaultCellStyle.Font).Width;
      return Math.Min(320, Math.Max(40, px + 14));
    }

    /// <summary>
    /// Carga los datos y ajusta la altura a la cantidad de filas.
    /// </summary>
    public static void Cargar(DataGridView g, DataTable datos, int maxFilas)
    {
      g.DataSource = datos;
      Ocultar(g);
      int filas = Math.Max(2, Math.Min(maxFilas, datos == null ? 0
        : datos.Rows.Count));
      g.Height = 36 + filas * 32 + 2;
      foreach (DataGridViewColumn c in g.Columns)
        c.SortMode = DataGridViewColumnSortMode.Automatic;
    }

    static readonly System.Text.RegularExpressions.Regex FechaIso =
      new System.Text.RegularExpressions.Regex(
        @"^\d{4}-\d{2}-\d{2}( \d{2}:\d{2}(:\d{2})?)?$");
    static readonly string[] Dinero = { "Total", "Valor", "Importe",
      "Arancel" };

    // La base guarda fechas ISO e importes sin formato: en pantalla se
    // muestran como 15/10/2026 09:30 y $ 24.000 (el orden no cambia).
    static void Formatear(object s, DataGridViewCellFormattingEventArgs e)
    {
      string texto = Mostrar(((DataGridView)s).Columns[e.ColumnIndex].Name,
        e.Value);
      if (texto == null) return;
      e.Value = texto;
      e.FormattingApplied = true;
    }

    // Texto con formato de una fecha o un importe; null si no hace falta.
    static string Mostrar(string columna, object valor)
    {
      string v = valor as string;
      if (v != null && FechaIso.IsMatch(v))
        return v.Substring(8, 2) + "/" + v.Substring(5, 2) + "/" +
          v.Substring(0, 4) + v.Substring(10, Math.Min(6, v.Length - 10));
      if (Dinero.Contains(columna) && (valor is long || valor is double ||
        valor is decimal || valor is int))
        return Negocio.Formato.Pesos(Convert.ToDecimal(valor));
      return null;
    }

    // Estado, Acción y Resultado: texto en negrita y con su color (nunca
    // sólo el color: el texto dice el estado).
    static void ColorearEstado(object s, DataGridViewCellFormattingEventArgs e)
    {
      DataGridView g = (DataGridView)s;
      string col = g.Columns[e.ColumnIndex].Name;
      if (col != "Estado" && col != "Acción" && col != "Resultado") return;
      e.CellStyle.ForeColor = Tema.DeEstado(Convert.ToString(e.Value));
      e.CellStyle.Font = Tema.Fuente(9.5f, FontStyle.Bold);
    }

    /// <summary>Id de la fila elegida (columna «Id»), o null.</summary>
    public static int? Elegido(DataGridView g, string columna = "Id")
    {
      if (g.CurrentRow == null || !g.Columns.Contains(columna)) return null;
      object v = g.CurrentRow.Cells[columna].Value;
      return v == null || v == DBNull.Value ? (int?)null : Convert.ToInt32(v);
    }

    /// <summary>
    /// Aviso en línea con ícono de texto y borde izquierdo de color.
    /// </summary>
    public static Aviso Aviso(TipoAviso tipo, string texto)
    {
      return new Aviso(tipo, texto);
    }

    /// <summary>Píldora de estado: texto con color de fondo suave.</summary>
    public static Label Pildora(string texto, Color color)
    {
      Label l = new Label { Text = texto, AutoSize = true,
        ForeColor = color, BackColor = Suave(color),
        Font = Tema.Fuente(9f, FontStyle.Bold), Padding = new Padding(8, 3,
        8, 3), Margin = new Padding(0, 6, 8, 0) };
      l.MaximumSize = Ui.Tope(l.PreferredWidth + 2);
      return l;
    }

    /// <summary>
    /// Etiqueta de circuito: «Consultorios externos» o «Estudios».
    /// </summary>
    public static Label Circuito(string c)
    {
      return Pildora(c == "B" ? "Estudios clínicos · Circuito B"
        : "Consultorios externos · Circuito A", Tema.DeCircuito(c));
    }

    /// <summary>Versión muy clara de un color (fondos de píldoras).</summary>
    public static Color Suave(Color c)
    {
      return Color.FromArgb((c.R + 255 * 7) / 8, (c.G + 255 * 7) / 8,
        (c.B + 255 * 7) / 8);
    }

    /// <summary>Tarjeta de indicador: título, valor grande y nota.</summary>
    public static Tarjeta Indicador(string titulo, string valor, string nota,
      Color color)
    {
      Tarjeta t = new Tarjeta { Espacio = 2 };
      t.Controls.Add(new Parrafo { Text = titulo.ToUpperInvariant(),
        Font = Tema.Fuente(8.5f, FontStyle.Bold), ForeColor = Tema.Texto2 });
      t.Controls.Add(new Parrafo { Text = valor, ForeColor = color,
        Font = Tema.Fuente(22f, FontStyle.Bold) });
      if (nota != null) t.Controls.Add(Nota(nota));
      return t;
    }
  }

  /// <summary>
  /// Aviso en línea (2.4): ícono, texto y borde izquierdo de color.
  /// </summary>
  [DesignerCategory("Code")]
  class Aviso : Columna
  {
    readonly Color color;
    readonly Parrafo texto;

    public Aviso(TipoAviso tipo, string mensaje)
    {
      color = tipo == TipoAviso.Error ? Tema.Error : tipo ==
        TipoAviso.Advertencia ? Tema.Advertencia : tipo == TipoAviso.Exito
        ? Tema.Exito : Tema.Info;
      BackColor = Ui.Suave(color);
      Padding = new Padding(16, 10, 12, 10);
      texto = new Parrafo { ForeColor = Tema.Texto, Font = Tema.Fuente(10f),
        Text = Icono(tipo) + "  " + mensaje };
      Controls.Add(texto);
    }

    static string Icono(TipoAviso t)
    {
      return t == TipoAviso.Error ? "⚠ " : t == TipoAviso.Advertencia ? "⚠ "
        : t == TipoAviso.Exito ? "✔ " : "ℹ ";
    }

    /// <summary>Cambia el texto del aviso.</summary>
    public string Mensaje
    {
      set { texto.Text = value; }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      using (SolidBrush b = new SolidBrush(color))
        e.Graphics.FillRectangle(b, 0, 0, 4, Height);
    }
  }
}
