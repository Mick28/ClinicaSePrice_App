using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-20 Asignar consultorio e insumos al profesional (CU-G-12): sin
  /// superposición en el mismo consultorio; dotación por consultorio.
  /// </summary>
  partial class PAsignacion : Pantalla
  {
    readonly ComboBox cboProfesional = Ui.Combo("Profesional",
      DatosBase.ListaProfesionales("médico"));
    readonly ComboBox cboConsultorio = Ui.Combo("Consultorio",
      DatosBase.ListaLugares("consultorio"));
    readonly ComboBox cboDia = Ui.Combo("Dia", Asignaciones.Dias);
    readonly ComboBox cboDesde = Ui.Hora("Desde"), cboHasta = Ui.Hora("Hasta");
    readonly DateTimePicker dtpVigencia = Ui.Fecha("Vigencia", Reloj.Hoy);
    readonly DateTimePicker dtpVigenciaHasta = Ui.Fecha("VigenciaHasta",
      Reloj.Hoy);
    readonly Columna derecha = new Columna();

    protected override void ConstruirNormal()
    {
      // Fecha opcional: sin tildar la casilla = sin fecha de fin.
      dtpVigenciaHasta.ShowCheckBox = true;
      dtpVigenciaHasta.Checked = false;
      cboConsultorio.SelectedIndexChanged += (s, e) => Ocupacion();
      cboDotConsultorio.SelectedIndexChanged += (s, e) => Dot();
      cboFiltroProfesional.SelectedIndexChanged += (s, e) => Filtrar();
      cboFiltroConsultorio.SelectedIndexChanged += (s, e) => Filtrar();
      Pestanas p = new Pestanas { AlElegir = Elegir };
      p.Agregar("Consultorios asignados");
      p.Agregar("Nueva asignación");
      p.Agregar("Dotación de insumos");
      Contenido.Controls.Add(p);
      p.Iniciar();
    }

    void Elegir(string nombre, Columna area)
    {
      OlvidarCampos();
      if (nombre == "Consultorios asignados") area.Controls.Add(Vigentes());
      else if (nombre == "Nueva asignación") area.Controls.Add(Ui.EnFila(
        Formulario(), derecha));
      else area.Controls.Add(Dotacion());
    }

    Control Formulario()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(380) };
      t.Controls.Add(Ui.Subtitulo("Nueva asignación"));
      t.Controls.Add(NuevoCampo("profesional", "Profesional",
        cboProfesional, true));
      t.Controls.Add(NuevoCampo("consultorio", "Consultorio",
        cboConsultorio, true));
      t.Controls.Add(NuevoCampo("dia", "Día", cboDia, true));
      t.Controls.Add(Ui.EnFila(NuevoCampo("franja", "Hora desde",
        cboDesde, true), NuevoCampo("hasta", "Hora hasta", cboHasta, true)));
      t.Controls.Add(Ui.EnFila(NuevoCampo("vigencia", "Vigencia desde",
        dtpVigencia), new Campo("Vigencia hasta (opcional)",
        dtpVigenciaHasta, false)));
      t.Controls.Add(Ui.Botones(Ui.Boton("Asignar", TipoBoton.Primario,
        (s, e) => Asignar()), BotonBlanquear()));
      return t;
    }

    void Asignar()
    {
      DatosAsignacion d = new DatosAsignacion {
        IdProfesional = (cboProfesional.SelectedItem as Opcion)?.Id ?? 0,
        IdConsultorio = (cboConsultorio.SelectedItem as Opcion)?.Id ?? 0,
        DiaSemana = cboDia.Text,
        HoraDesde = (cboDesde.SelectedItem as string) ?? "",
        HoraHasta = (cboHasta.SelectedItem as string) ?? "",
        VigenciaDesde = Formato.Iso(dtpVigencia.Value),
        VigenciaHasta = dtpVigenciaHasta.Checked
          ? Formato.Iso(dtpVigenciaHasta.Value) : null };
      if (!Horas(d)) return;
      if (Guardar(() => Asignaciones.Asignar(d), "Asignación registrada. " +
        Afectados(d))) Ocupacion();
    }

    bool Horas(DatosAsignacion d)
    {
      TimeSpan h;
      bool ok = TimeSpan.TryParse(d.HoraDesde, out h) &&
        TimeSpan.TryParse(d.HoraHasta, out h) && d.HoraDesde.Length == 5 &&
        d.HoraHasta.Length == 5;
      if (!ok) Dialogo.Error("Elegí la hora desde y la hora hasta.");
      return ok;
    }

    // Reasignación (CU-A-16): turnos futuros en otro consultorio.
    static string Afectados(DatosAsignacion d)
    {
      int n = Asignaciones.TurnosAfectados(d.IdProfesional,
        d.IdConsultorio);
      return n == 0 ? "" : "Atención: el profesional tiene " + n + " turnos" +
        " futuros en otro consultorio; avisá a los pacientes el nuevo lugar.";
    }
  }
}
