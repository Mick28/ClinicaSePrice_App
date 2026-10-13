using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-07 Asignar sobreturno (CU-A-07, CU-G-05): cubre la demanda cuando la
  /// agenda está completa. Máximo 1 sobreturno por hora y profesional.
  /// </summary>
  class PSobreturno : Pantalla
  {
    readonly ComboBox cboEspecialidad = Ui.Combo("Especialidad",
      DatosBase.ListaEspecialidades());
    readonly DateTimePicker dtpDia = Ui.Fecha("Dia", Reloj.Hoy);
    readonly ComboBox cboAgenda = Ui.Combo("Agenda");
    readonly ComboBox cboHora = Ui.Combo("Hora");
    readonly RadioButton optPersonal = Ui.Opcion("Personal", "Personal");
    readonly RadioButton optTelefonica = Ui.Opcion("Telefonica",
      "Telefónica");

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Demanda sin turno de hoy (sobreturnos, laboratorio y " +
        "guardia)", Consultas.DemandaDelDia(Reloj.Hoy));
    }

    // Regla de cupo (parámetro de la coordinación).
    static string Regla
    {
      get { return "Se permite " + Parametros.Valor("sobreturnos_por_hora")
        + " sobreturno por hora por profesional. Sólo se ofrecen las horas" +
        " con cupo."; }
    }

    protected override void ConstruirNormal()
    {
      if (!ExigirPaciente()) return;
      AgregarBarraPaciente();
      optPersonal.Checked = true;
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Aviso(TipoAviso.Info, Regla));
      t.Controls.Add(Ui.EnFila(NuevoCampo("especialidad", "Especialidad",
        cboEspecialidad, true), NuevoCampo("dia", "Fecha", dtpDia, true),
        NuevoCampo("agenda", "Profesional y agenda", cboAgenda, true)));
      t.Controls.Add(Ui.EnFila(NuevoCampo("hora", "Hora", cboHora, true),
        new Campo("Modalidad de toma *", Ui.Grupo(optPersonal,
        optTelefonica), true)));
      t.Controls.Add(Acciones());
      Contenido.Controls.Add(t);
      Eventos();
    }

    Control Acciones()
    {
      return Ui.Botones(Ui.Boton("Asignar sobreturno", TipoBoton.Primario,
        (s, e) => Asignar()), Ui.Boton("Cancelar", TipoBoton.Secundario,
        (s, e) => Ir("P-06")));
    }

    void Eventos()
    {
      cboEspecialidad.SelectedIndexChanged += (s, e) => Agendas();
      dtpDia.ValueChanged += (s, e) => Agendas();
      cboAgenda.SelectedIndexChanged += (s, e) => Horas();
    }

    void Agendas()
    {
      cboAgenda.Items.Clear();
      cboAgenda.Items.Add(Ui.Seleccionar);
      Opcion esp = cboEspecialidad.SelectedItem as Opcion;
      if (esp != null) foreach (Opcion o in Turnos.AgendasDelDia(esp.Id,
        dtpDia.Value)) cboAgenda.Items.Add(o);
      cboAgenda.SelectedIndex = 0;
    }

    void Horas()
    {
      cboHora.Items.Clear();
      cboHora.Items.Add(Ui.Seleccionar);
      Opcion a = cboAgenda.SelectedItem as Opcion;
      if (a != null) foreach (string h in Turnos.HorasConCupo(a.Id))
        cboHora.Items.Add(h);
      cboHora.SelectedIndex = 0;
    }

    void Asignar()
    {
      Opcion a = cboAgenda.SelectedItem as Opcion;
      int id = 0;
      if (Guardar(() => id = Turnos.AsignarSobreturno(PacienteEnCurso
        .Actual.Id, a.Id, cboHora.Text, optTelefonica.Checked ? "telefónica"
        : "personal"), null)) Ir("P-08", id);
    }
  }
}
