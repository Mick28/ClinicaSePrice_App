using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-10 Acreditar paciente y registrar modalidad de pago (CU-G-06), con
  /// el panel Validar cobertura (CU-G-07) en el mismo paso. En estudios se
  /// exige la orden médica.
  /// </summary>
  partial class PAcreditar : Pantalla
  {
    readonly ComboBox cboTurno = Ui.Combo("Turno");
    readonly RadioButton optObra = Ui.Opcion("Obra", "Obra social");
    readonly RadioButton optParticular = Ui.Opcion("Particular",
      "Particular");
    readonly Columna panel = new Columna { Espacio = 10 };
    Validacion validacion;

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Acreditaciones y validaciones de hoy",
        Consultas.Acreditaciones(Reloj.Hoy), "Incluye la modalidad de pago" +
        " y el resultado de la validación con la obra social.");
    }

    protected override void ConstruirNormal()
    {
      if (!ExigirPaciente()) { Pendientes(); return; }
      AgregarBarraPaciente();
      foreach (TurnoDeHoy t in Acreditacion.TurnosDeHoy(
        PacienteEnCurso.Actual.Id)) cboTurno.Items.Add(t);
      Contenido.Controls.Add(Ui.EnFila(Paso1(), Paso2(), Paso3()));
      Contenido.Controls.Add(Ui.Botones(Ui.Boton("Acreditar paciente",
        TipoBoton.Primario, (s, e) => Acreditar()), BotonBlanquear()));
      bool tieneOs = PacienteEnCurso.Actual.IdObraSocial != null;
      optObra.Checked = tieneOs;
      optParticular.Checked = !tieneOs;
      Eventos();
      Pendientes();
    }

    void Eventos()
    {
      optObra.CheckedChanged += (s, e) => Panel();
      cboTurno.SelectedIndexChanged += (s, e) => Panel();
      if (cboTurno.Items.Count > 1) cboTurno.SelectedIndex = 1;
      Panel();
    }

    Control Paso1()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("1 · Turno de hoy"));
      t.Controls.Add(NuevoCampo("turno", "Turno a acreditar", cboTurno,
        true));
      if (cboTurno.Items.Count == 1) t.Controls.Add(Ui.Nota("El paciente " +
        "no tiene turnos reservados para hoy."));
      return t;
    }

    Control Paso2()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("2 · Modalidad de pago *"));
      t.Controls.Add(Ui.Grupo(optObra, optParticular));
      return t;
    }

    Control Paso3()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("3 · Validar cobertura"));
      t.Controls.Add(panel);
      return t;
    }

    TurnoDeHoy Turno
    {
      get { return cboTurno.SelectedItem as TurnoDeHoy; }
    }

    void Acreditar()
    {
      if (Turno == null) { Dialogo.Error("Elegí el turno a acreditar.");
        return; }
      string aviso = null;
      string modalidad = optObra.Checked ? "obra social" : "particular";
      if (!Guardar(() => aviso = Acreditacion.Acreditar(Turno.IdTurno,
        modalidad, validacion, Turno.EsEstudio), null)) return;
      if (aviso != null) Dialogo.Informar(aviso, "Llegada fuera de horario");
      Dialogo.Informar("Paciente acreditado. Ya figura en la " + (Turno
        .EsEstudio ? "cola de atención." : "sala de espera."));
      Ir("P-10");
    }
  }
}
