using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-18 Generar reportes de gestión (CU-G-11: CU-A-14 y CU-B-16): totales
  /// e indicadores del período, nunca listados de pacientes. Exporta como
  /// planilla o como PDF (impresión).
  /// </summary>
  class PReportes : Pantalla
  {
    readonly DateTimePicker dtpDesde = Ui.Fecha("Desde",
      new DateTime(2026, 9, 1));
    readonly DateTimePicker dtpHasta = Ui.Fecha("Hasta",
      new DateTime(2026, 9, 30));
    readonly ComboBox cboCircuito = Ui.Combo("Circuito", new[] { "Ambos",
      "Consultorios externos", "Estudios clínicos" }, false);
    readonly Columna salida = new Columna { Espacio = 16 };
    Reporte reporte;

    protected override void ConstruirAuditoria() { ConstruirNormal(); }

    protected override void ConstruirNormal()
    {
      cboCircuito.SelectedIndex = 0;
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(new Campo("Período desde *", dtpDesde, true),
        new Campo("Hasta *", dtpHasta, true), new Campo("Circuito",
        cboCircuito, false), Ui.Botones(Ui.Boton("Generar reporte",
        TipoBoton.Primario, (s, e) => Generar()))));
      Contenido.Controls.Add(t);
      Contenido.Controls.Add(salida);
    }

    string Circuito
    {
      get { return cboCircuito.SelectedIndex == 1 ? "A"
        : cboCircuito.SelectedIndex == 2 ? "B" : null; }
    }

    void Generar()
    {
      salida.Controls.Clear();
      if (!Intentar(() => reporte = Reportes.Generar(dtpDesde.Value,
        dtpHasta.Value, Circuito))) return;
      Tablero();
      salida.Controls.Add(Ui.EnFila(Grafico("Por especialidad o estudio",
        reporte.PorRubro), Grafico("Turnos por día", reporte.PorDia)));
      TablaInsumos();
      if (!Auditoria) salida.Controls.Add(Exportar());
      Contenido.PerformLayout();
    }

    // Indicadores del período (totales; nunca listados de pacientes).
    void Tablero()
    {
      salida.Controls.Add(Indicadores(new[] { I("Turnos dados",
        reporte.Dados), I("Atendidos", reporte.Atendidos), I("Ausentismo",
        reporte.Ausentismo.ToString("0.0") + " %"), I("Sobreturnos",
        reporte.Sobreturnos) }));
      salida.Controls.Add(Indicadores(new[] { I("Cancelaciones",
        reporte.Cancelados), I("Espera promedio", reporte.EsperaPromedio +
        " min"), I("Ocupación de salas *", reporte.OcupacionSalas
        .ToString("0") + " %"), I("Entregados a término",
        reporte.EntregadosATermino) }));
      salida.Controls.Add(Ui.Nota("* Indicador a confirmar con el cliente."));
    }

    static object[] I(string titulo, object valor)
    {
      return new[] { titulo, valor };
    }

    static Control Indicadores(object[][] l)
    {
      Fila f = new Fila();
      foreach (object[] i in l) f.Controls.Add(Ui.Indicador((string)i[0],
        i[1].ToString(), null, Tema.Primario));
      return f;
    }

    Control Grafico(string titulo, List<KeyValuePair<string, int>> datos)
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo(titulo));
      t.Controls.Add(new Barras(datos, Circuito == "B" ? Tema.CircuitoB
        : Tema.CircuitoA));
      return t;
    }

    void TablaInsumos()
    {
      if (reporte.Insumos.Rows.Count == 0) return;
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Consumo de insumos del período"));
      t.Controls.Add(Ui.Tabla("Insumos", reporte.Insumos, 8));
      salida.Controls.Add(t);
    }

    Control Exportar()
    {
      return Ui.BotonesIzq(Ui.Boton("Exportar planilla", TipoBoton.Secundario,
        (s, e) => Planilla()), Ui.Boton("Imprimir o guardar PDF",
        TipoBoton.Secundario, (s, e) => Imprimir()));
    }

    string Periodo
    {
      get { return Formato.Iso(dtpDesde.Value) + " a " +
        Formato.Iso(dtpHasta.Value); }
    }

    void Planilla()
    {
      using (SaveFileDialog d = new SaveFileDialog { Filter = "Planilla CSV" +
        "|*.csv", FileName = "reporte_" + Formato.Iso(dtpDesde.Value) +
        ".csv" })
        if (d.ShowDialog() == DialogResult.OK && Intentar(() =>
          Reportes.Exportar(reporte, d.FileName, Periodo)))
          Dialogo.Informar("Reporte exportado. La exportación quedó " +
            "registrada.");
    }

    void Imprimir()
    {
      PrintDocument doc = new PrintDocument { DocumentName = "Reporte" };
      doc.PrintPage += (s, e) => e.Graphics.DrawString("Clínica SePrice · " +
        "Reporte de gestión\n" + Periodo + "\n\nTurnos dados: " +
        reporte.Dados + "\nAtendidos: " + reporte.Atendidos + "\nAusentismo:" +
        " " + reporte.Ausentismo.ToString("0.0") + " %\nSobreturnos: " +
        reporte.Sobreturnos + "\nCancelaciones: " + reporte.Cancelados,
        Tema.Fuente(12f), Brushes.Black, 60, 60);
      using (PrintPreviewDialog p = new PrintPreviewDialog { Document = doc })
        p.ShowDialog();
    }
  }

  /// <summary>
  /// Gráfico de barras simple, con el valor escrito en cada barra.
  /// </summary>
  [DesignerCategory("Code")]
  class Barras : Control
  {
    readonly List<KeyValuePair<string, int>> datos;
    readonly Color color;

    public Barras(List<KeyValuePair<string, int>> datos, Color color)
    {
      this.datos = datos.Take(16).ToList();
      this.color = color;
      Height = 220;
      DoubleBuffered = true;
      ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      if (datos.Count == 0) return;
      float max = datos.Max(d => d.Value), ancho = (Width - 20f) /
        datos.Count, alto = Height - 50f;
      using (SolidBrush b = new SolidBrush(color))
        for (int i = 0; i < datos.Count; i++)
          Barra(e.Graphics, b, datos[i], 10 + i * ancho, ancho, alto, max);
    }

    static void Barra(Graphics g, Brush b, KeyValuePair<string, int> d,
      float x, float ancho, float alto, float max)
    {
      float h = max == 0 ? 0 : d.Value / max * alto;
      g.FillRectangle(b, x + 4, 20 + alto - h, ancho - 8, h);
      g.DrawString(d.Value.ToString(), Tema.Fuente(8f), Brushes.Black,
        x + 4, 4 + alto - h);
      g.DrawString(d.Key, Tema.Fuente(7.5f), Brushes.DimGray,
        new RectangleF(x, 24 + alto, ancho, 28));
    }
  }
}
