using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-16 Entregar resultado del estudio (CU-G-09, CU-B-15): contra el
  /// comprobante de retiro; en internación se carga el informe y el
  /// receptor es el sector.
  /// </summary>
  class PEntrega : Pantalla
  {
    readonly TextBox txtBuscar = Ui.Caja("Buscar");
    readonly Columna resultado = new Columna { Espacio = 12 };

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Resultados de estudios y entregas",
        Consultas.Resultados());
    }

    protected override void ConstruirNormal()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(new Campo("N.° de comprobante de retiro o " +
        "DNI *", txtBuscar, true), Ui.BotonesIzq(Ui.Boton("Buscar",
        TipoBoton.Primario, (s, e) => Buscar())).Tap(f => { f.Padding =
        new Padding(0, 22, 0, 0); f.Height = 66; f.MaximumSize =
        Ui.Tope(110); })));
      Contenido.Controls.Add(t);
      Contenido.Controls.Add(resultado);
      Pendientes();
      TablaDeLectura("Últimas entregas", Resultados.EntregasRecientes(),
        "Fecha de entrega, quién retiró y si se cumplió la fecha estimada.");
    }

    // Lista de lo que falta entregar; un clic en una fila lo busca.
    void Pendientes()
    {
      DataGridView g = Ui.Tabla("Pendientes", Resultados.Pendientes(), 10);
      g.CellClick += (s, e) => Elegir(g, e.RowIndex);
      Tarjeta c = new Tarjeta();
      c.Controls.Add(Ui.Subtitulo("Pendientes de entrega"));
      c.Controls.Add(Ui.Nota("Hacé clic en una fila para buscarla. Los de " +
        "internación se entregan al sector, con el informe del episodio."));
      c.Controls.Add(g);
      Contenido.Controls.Add(c);
    }

    void Elegir(DataGridView g, int fila)
    {
      if (fila < 0) return;
      string comprobante = Convert.ToString(g.Rows[fila].Cells[
        "Comprobante"].Value);
      txtBuscar.Text = comprobante.Length > 0 ? comprobante
        : Convert.ToString(g.Rows[fila].Cells["DNI"].Value);
      Buscar();
    }

    void Buscar()
    {
      resultado.Controls.Clear();
      OlvidarCampos();
      List<ResultadoPendiente> l = null;
      if (!Intentar(() => l = Resultados.Buscar(txtBuscar.Text))) return;
      if (l.Count == 0) resultado.Controls.Add(Ui.Aviso(TipoAviso.Info,
        "No hay estudios realizados pendientes de entrega con ese dato."));
      foreach (ResultadoPendiente r in l) resultado.Controls.Add(Tarjeta(r));
    }

    // Cada resultado encontrado tiene su propia tarjeta con sus controles.
    Control Tarjeta(ResultadoPendiente r)
    {
      Tarjeta t = new Tarjeta();
      FormEntrega f = new FormEntrega(r);
      t.Controls.Add(Ui.Subtitulo(r.ToString()));
      bool listo = string.CompareOrdinal(r.Estimada, Formato.Iso(Reloj.Hoy))
        <= 0;
      t.Controls.Add(Ui.Pildora("Realizado " + Formato.Mostrar(r.Realizado) +
        " · estimado " + Formato.Mostrar(r.Estimada), listo || r.EsInternacion
        ? Tema.Exito : Tema.Advertencia));
      if (r.EsInternacion) Internacion(t, f);
      else Retira(t, f);
      t.Controls.Add(Ui.Botones(Ui.Boton("Registrar entrega",
        TipoBoton.Primario, (s, e) => Entregar(f))));
      return t;
    }

    void Retira(Columna t, FormEntrega f)
    {
      f.Paciente.Checked = true;
      t.Controls.Add(Ui.Nota("¿QUIÉN RETIRA?"));
      t.Controls.Add(Ui.Grupo(f.Paciente, f.Otra));
      t.Controls.Add(NuevoCampo("receptor" + f.R.Id, "Si es otra persona: " +
        "nombre y DNI", f.Receptor));
    }

    void Internacion(Columna t, FormEntrega f)
    {
      f.Receptor.Text = "Sector " + f.R.Sector + " · " + f.R.Episodio;
      f.Informe.Multiline = true;
      f.Informe.Height = 90;
      t.Controls.Add(NuevoCampo("receptor" + f.R.Id, "Receptor (sector)",
        f.Receptor, true));
      t.Controls.Add(NuevoCampo("informe" + f.R.Id, "Informe para el " +
        "episodio", f.Informe, true));
    }

    void Entregar(FormEntrega f)
    {
      ResultadoPendiente r = f.R;
      string receptor = !r.EsInternacion && f.Paciente.Checked
        ? r.Paciente + " (paciente)" : f.Receptor.Text;
      string informe = r.EsInternacion ? f.Informe.Text : null;
      if (Guardar(() => Resultados.Entregar(r, receptor, informe),
        "Entrega registrada: el estudio quedó «entregado».")) Ir("P-16");
    }
  }

  /// <summary>Controles de la entrega de un resultado (una tarjeta).</summary>
  class FormEntrega
  {
    public readonly ResultadoPendiente R;
    public readonly RadioButton Paciente = Ui.Opcion("Paciente",
      "El/la paciente");
    public readonly RadioButton Otra = Ui.Opcion("Otra", "Otra persona");
    public readonly TextBox Receptor = Ui.Caja("Receptor");
    public readonly TextBox Informe = Ui.Caja("Informe");

    public FormEntrega(ResultadoPendiente r)
    {
      R = r;
    }
  }
}
