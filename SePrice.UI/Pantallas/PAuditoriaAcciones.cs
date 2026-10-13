using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PAuditoria
  {
    void Aplicar()
    {
      FiltroAuditoria f = new FiltroAuditoria { Desde = dtpDesde.Value,
        Hasta = dtpHasta.Value, Modulo = Vacio(txtModulo.Text),
        Usuario = cboUsuario.SelectedIndex <= 0 ? null : cboUsuario.Text,
        Accion = cboAccion.SelectedIndex <= 0 ? null : cboAccion.Text,
        Circuito = cboCircuito.SelectedIndex == 1 ? "A"
          : cboCircuito.SelectedIndex == 2 ? "B" : null };
      if (!Intentar(() => resultado = Auditar.Buscar(f))) return;
      Ui.Cargar(tblOperaciones, resultado, 12);
      if (resultado.Rows.Count == 0) Dialogo.Informar("No hay operaciones " +
        "para ese filtro. Probá ampliar el período.", "Sin resultados");
      Contenido.PerformLayout();
    }

    static string Vacio(string s)
    {
      return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    void Detalle()
    {
      int? id = Ui.Elegido(tblOperaciones);
      if (id == null) return;
      string[] d = Auditar.Detalle(id.Value);
      lblAnterior.Text = d[0].Replace("; ", "\n");
      lblNuevo.Text = d[1].Replace("; ", "\n");
    }

    // Exportar: la exportación también queda registrada (CU-B-20).
    void Exportar()
    {
      if (resultado == null || resultado.Rows.Count == 0) return;
      using (SaveFileDialog d = new SaveFileDialog { Filter = "Planilla " +
        "CSV|*.csv", FileName = "auditoria_" + Formato.Iso(Reloj.Hoy) +
        ".csv" })
      {
        if (d.ShowDialog() != DialogResult.OK) return;
        if (Intentar(() => Auditar.Exportar(resultado, d.FileName)))
          Dialogo.Informar("Se exportaron " + resultado.Rows.Count +
            " operaciones. La exportación quedó registrada.");
      }
    }

    // Pestaña «Módulos en sólo lectura».
    void Modulos(Columna area)
    {
      area.Controls.Add(Ui.Nota("Las pantallas se abren sin campos " +
        "editables ni botones de acción. Las de configuración no están " +
        "acá: el Administrador/a las edita desde su menú."));
      Fila fila = null;
      int n = 0;
      foreach (string[] m in Navegacion.ModulosAuditables)
      {
        if (n++ % 3 == 0) { fila = new Fila(); area.Controls.Add(fila); }
        fila.Controls.Add(Modulo(m));
      }
    }

    Control Modulo(string[] m)
    {
      Tarjeta t = new Tarjeta { Espacio = 8 };
      t.Controls.Add(Ui.Subtitulo(m[0]));
      t.Controls.Add(m[1] == "ambos" ? (Control)Ui.Nota("Circuitos A y B")
        : Ui.Circuito(m[1]));
      for (int i = 2; i < m.Length; i++)
      {
        string cod = m[i];
        t.Controls.Add(Ui.BotonesIzq(Ui.Boton("👁  " +
          Navegacion.Pantallas[cod].Menu, TipoBoton.Enlace, (s, e) =>
          Principal.Abrir(cod, null, true))));
      }
      return t;
    }
  }
}
