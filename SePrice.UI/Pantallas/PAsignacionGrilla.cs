using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PAsignacion
  {
    readonly ComboBox cboDotConsultorio = Ui.Combo("DotConsultorio",
      DatosBase.ListaLugares("consultorio"));
    readonly ComboBox cboInsumo = Ui.Combo("Insumo", Insumos.Lista());
    readonly NumericUpDown numCantidad = Ui.Numero("Cantidad", 1, 999, 10);
    DataGridView tblDotacion;

    // Grilla semanal de ocupación del consultorio elegido.
    void Ocupacion()
    {
      Opcion c = cboConsultorio.SelectedItem as Opcion;
      if (c == null) return;
      derecha.Controls.Clear();
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Ocupación semanal · " + c.Texto));
      t.Controls.Add(new GrillaSemanal(Asignaciones.Ocupacion(c.Id)));
      t.Controls.Add(Ui.Tabla("Asignaciones", Asignaciones.DelConsultorio(
        c.Id), 8));
      derecha.Controls.Add(t);
    }

    Control Dotacion()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Nota("Qué insumos básicos debe tener cada " +
        "consultorio y cuánto. La alerta de stock la dispara el mínimo " +
        "global de cada insumo (CU-A-15)."));
      t.Controls.Add(Ui.EnFila(new Campo("Consultorio", cboDotConsultorio,
        true), NuevoCampo("insumo", "Insumo", cboInsumo, true),
        NuevoCampo("cantidad", "Cantidad mínima", numCantidad, true)));
      t.Controls.Add(Ui.Botones(Ui.Boton("Agregar insumo",
        TipoBoton.Primario, (s, e) => Agregar()), Ui.Boton("Quitar " +
        "elegido", TipoBoton.Secundario, (s, e) => Quitar())));
      tblDotacion = Ui.Tabla("Dotacion", null, 8);
      t.Controls.Add(tblDotacion);
      if (cboDotConsultorio.Items.Count > 1)
        cboDotConsultorio.SelectedIndex = 1;
      return t;
    }

    int Consultorio
    {
      get { return (cboDotConsultorio.SelectedItem as Opcion)?.Id ?? 0; }
    }

    void Dot()
    {
      Ui.Cargar(tblDotacion, Asignaciones.Dotacion(Consultorio), 8);
      Contenido.PerformLayout();
    }

    void Agregar()
    {
      Opcion i = cboInsumo.SelectedItem as Opcion;
      if (Guardar(() => Asignaciones.FijarDotacion(Consultorio, i?.Id ?? 0,
        (int)numCantidad.Value), null, false)) Dot();
    }

    void Quitar()
    {
      int? id = Ui.Elegido(tblDotacion);
      if (id != null && Dialogo.Confirmar("¿Quitar el insumo de la " +
        "dotación?", "Quitar", "Quitar", true) && Intentar(() =>
        Asignaciones.QuitarDotacion(Consultorio, id.Value))) Dot();
    }
  }

  /// <summary>
  /// Grilla lunes a sábado, 8 a 20 h, con bloques por profesional.
  /// </summary>
  [DesignerCategory("Code")]
  class GrillaSemanal : Control
  {
    readonly List<string[]> bloques;

    public GrillaSemanal(List<string[]> bloques)
    {
      this.bloques = bloques;
      Height = 300;
      DoubleBuffered = true;
      ResizeRedraw = true;
      BackColor = Tema.Superficie;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      Graphics g = e.Graphics;
      float col = (Width - 50) / 6f, fila = (Height - 24) / 12f;
      Pen borde = new Pen(Tema.Borde);
      for (int d = 0; d < 6; d++)
        g.DrawString(Asignaciones.Dias[d].Substring(0, 3), Tema.Fuente(8f),
          Brushes.Gray, 50 + d * col + 4, 4);
      for (int h = 0; h <= 12; h++)
      {
        g.DrawLine(borde, 50, 24 + h * fila, Width, 24 + h * fila);
        g.DrawString((8 + h) + ":00", Tema.Fuente(7.5f), Brushes.Gray, 4,
          18 + h * fila);
      }
      foreach (string[] b in bloques) Bloque(g, b, col, fila);
    }

    void Bloque(Graphics g, string[] b, float col, float fila)
    {
      int d = System.Array.IndexOf(Asignaciones.Dias, b[0]);
      float y1 = 24 + (Formato.Minutos(b[1]) / 60f - 8) * fila;
      float y2 = 24 + (Formato.Minutos(b[2]) / 60f - 8) * fila;
      RectangleF r = new RectangleF(52 + d * col, y1, col - 4, y2 - y1);
      using (SolidBrush f = new SolidBrush(Tema.PrimarioSuave))
        g.FillRectangle(f, r);
      g.DrawRectangle(new Pen(Tema.Primario), r.X, r.Y, r.Width, r.Height);
      g.DrawString(b[3] + "\n" + b[1] + "-" + b[2], Tema.Fuente(7.5f),
        new SolidBrush(Tema.Texto), r);
    }
  }
}
