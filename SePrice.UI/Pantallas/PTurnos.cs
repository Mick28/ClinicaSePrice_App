using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-06 Asignar turno (CU-G-04: CU-A-05, CU-A-06 y CU-B-06): busca
  /// franjas libres que respeten las reglas y las reserva para el paciente
  /// en curso. Incluye el legajo (CU-G-02) y emite el comprobante.
  /// </summary>
  partial class PTurnos : Pantalla
  {
    readonly ComboBox cboEspecialidad = Ui.Combo("Especialidad",
      DatosBase.ListaEspecialidades());
    readonly ComboBox cboProfesional = Ui.Combo("Profesional");
    readonly ComboBox cboEstudio = Ui.Combo("Estudio", Catalogo.Estudios());
    readonly DateTimePicker dtpDesde = Ui.Fecha("Desde", Reloj.Hoy);
    readonly RadioButton optPersonal = Ui.Opcion("Personal", "Personal");
    readonly RadioButton optTelefonica = Ui.Opcion("Telefonica",
      "Telefónica");
    readonly Columna calendario = new Columna { Espacio = 10 };
    readonly Columna resumen = new Columna { Espacio = 8 };
    string circuito = "A";
    Franja elegida;

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Turnos de hoy", Consultas.TurnosDelDia(Reloj.Hoy));
    }

    protected override void ConstruirNormal()
    {
      if (!ExigirPaciente()) return;
      AgregarBarraPaciente();
      optPersonal.Checked = true;
      cboEspecialidad.SelectedIndexChanged += (s, e) => Profesionales();
      Pestanas p = new Pestanas { AlElegir = Elegir };
      p.Agregar("Consultorios externos");
      p.Agregar("Estudios clínicos");
      Contenido.Controls.Add(p);
      p.Iniciar();
      Contenido.Controls.Add(Ui.BotonesIzq(Ui.Boton("Reprogramar o " +
        "cancelar turnos", TipoBoton.Enlace, (s, e) => Ir("P-09"))));
      Controles();
    }

    void Elegir(string nombre, Columna area)
    {
      circuito = nombre.StartsWith("Consultorios") ? "A" : "B";
      elegida = null;
      area.Controls.Add(Ui.Circuito(circuito));
      area.Controls.Add(Filtros());
      calendario.Controls.Clear();
      area.Controls.Add(Ui.EnFila(calendario, Resumen()));
    }

    Control Filtros()
    {
      Tarjeta t = new Tarjeta();
      Control modalidad = new Campo("Modalidad de toma *", Ui.Grupo(
        optPersonal, optTelefonica), true);
      t.Controls.Add(circuito == "A" ? Ui.EnFila(new Campo("Especialidad *",
        cboEspecialidad, true), new Campo("Profesional (opcional)",
        cboProfesional, false), new Campo("Semana del", dtpDesde, false),
        modalidad) : Ui.EnFila(new Campo("Estudio *", cboEstudio, true),
        new Campo("Semana del", dtpDesde, false), modalidad));
      t.Controls.Add(Ui.Botones(Ui.Boton("Consultar disponibilidad",
        TipoBoton.Primario, (s, e) => Buscar())));
      return t;
    }

    void Profesionales()
    {
      cboProfesional.Items.Clear();
      cboProfesional.Items.Add(Ui.Seleccionar);
      Opcion esp = cboEspecialidad.SelectedItem as Opcion;
      if (esp != null) foreach (Opcion o in DatosBase.ListaProfesionales(
        "médico").Where(o => o.Texto.EndsWith(esp.Texto.Split('·')[0]
        .Trim()))) cboProfesional.Items.Add(o);
      cboProfesional.SelectedIndex = 0;
    }

    string Modalidad
    {
      get { return optTelefonica.Checked ? "telefónica" : "personal"; }
    }
  }
}
