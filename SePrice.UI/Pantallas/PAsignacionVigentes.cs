using System.Collections.Generic;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-20, pestaña «Consultorios asignados»: todas las asignaciones
  /// vigentes, con filtro por profesional o consultorio, y la opción de
  /// finalizar una (se le pone fecha de fin; no se borra).
  /// </summary>
  partial class PAsignacion
  {
    readonly ComboBox cboFiltroProfesional = Ui.Combo("FiltroProfesional",
      DatosBase.ListaProfesionales("médico"));
    readonly ComboBox cboFiltroConsultorio = Ui.Combo("FiltroConsultorio",
      DatosBase.ListaLugares("consultorio"));
    readonly DateTimePicker dtpFin = Ui.Fecha("Fin", Reloj.Hoy);
    DataGridView tblVigentes;

    Control Vigentes()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Nota("Asignaciones vigentes de todos los " +
        "consultorios. Filtrá por profesional o por consultorio; con " +
        "«Seleccioná…» se ven todas."));
      t.Controls.Add(Ui.EnFila(new Campo("Profesional",
        cboFiltroProfesional, false), new Campo("Consultorio",
        cboFiltroConsultorio, false)));
      tblVigentes = Ui.Tabla("Vigentes", null, 12);
      t.Controls.Add(tblVigentes);
      t.Controls.Add(Ui.EnFila(NuevoCampo("fin", "Último día de atención",
        dtpFin), Ui.Botones(Ui.Boton("Finalizar asignación elegida",
        TipoBoton.Secundario, (s, e) => Finalizar()))));
      Filtrar();
      return t;
    }

    // Vuelve a cargar la tabla con los filtros elegidos.
    void Filtrar()
    {
      if (tblVigentes == null || tblVigentes.IsDisposed) return;
      Ui.Cargar(tblVigentes, Asignaciones.Vigentes(
        Elegida(cboFiltroProfesional), Elegida(cboFiltroConsultorio)), 12);
      Contenido.PerformLayout();
    }

    // Id de la opción elegida, o null si está en «Seleccioná…».
    static int? Elegida(ComboBox c)
    {
      return (c.SelectedItem as Opcion)?.Id;
    }

    // Pone fecha de fin a la asignación elegida en la tabla. Si ya hay
    // agenda publicada después, lo avisa antes de confirmar.
    void Finalizar()
    {
      int? id = Ui.Elegido(tblVigentes);
      if (id == null) { Dialogo.Error("Elegí en la tabla la asignación " +
        "a finalizar."); return; }
      string fin = Formato.Iso(dtpFin.Value);
      List<string> l = Asignaciones.AgendasPosteriores(id.Value, fin);
      string aviso = l.Count == 0 ? "" : "\n\nYa hay agenda publicada " +
        "después de esa fecha:\n" + string.Join("\n", l) + "\nEsos días " +
        "siguen publicados: hay que anularlos o reprogramar sus turnos.";
      if (!Dialogo.Confirmar("¿Finalizar la asignación? El último día de " +
        "atención será el " + Formato.Corta(dtpFin.Value) + "." + aviso,
        "Finalizar asignación", "Finalizar", true)) return;
      if (Intentar(() => Asignaciones.Finalizar(id.Value, fin))) Filtrar();
    }
  }
}
