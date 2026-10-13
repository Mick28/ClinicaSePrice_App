using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-21 Publicar agenda de profesional (CU-A-04, CU-G-03): genera las
  /// franjas según la duración de la especialidad, dentro de la asignación
  /// de consultorio. Sin asignación, no se publica.
  /// </summary>
  class PPublicarAgenda : Pantalla
  {
    readonly ComboBox cboProfesional = Ui.Combo("Profesional",
      DatosBase.ListaProfesionales("médico"));
    readonly DateTimePicker dtpDesde = Ui.Fecha("Desde",
      Reloj.Hoy.AddDays(1));
    readonly DateTimePicker dtpHasta = Ui.Fecha("Hasta",
      Reloj.Hoy.AddDays(14));
    readonly Columna vista = new Columna();

    protected override void ConstruirNormal()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(NuevoCampo("profesional", "Profesional",
        cboProfesional, true), NuevoCampo("periodo", "Desde", dtpDesde,
        true), new Campo("Hasta", dtpHasta, true)));
      t.Controls.Add(Ui.Botones(Ui.Boton("Ver vista previa",
        TipoBoton.Secundario, (s, e) => Previa())));
      Contenido.Controls.Add(t);
      Contenido.Controls.Add(vista);
    }

    int Profesional
    {
      get { return (cboProfesional.SelectedItem as Opcion)?.Id ?? 0; }
    }

    void Previa()
    {
      vista.Controls.Clear();
      if (Profesional == 0) { Dialogo.Error("Elegí el profesional.");
        return; }
      List<DiaAgenda> dias = null;
      try { dias = Agendas.Previsualizar(Profesional, dtpDesde.Value,
        dtpHasta.Value); }
      catch (ReglaException e) { SinConsultorio(e.Message); return; }
      vista.Controls.Add(Resumen(dias));
    }

    void SinConsultorio(string mensaje)
    {
      vista.Controls.Add(Ui.Aviso(TipoAviso.Advertencia, mensaje));
      if (mensaje.Contains("consultorio asignado"))
        vista.Controls.Add(Ui.BotonesIzq(Ui.Boton("Asignar consultorio",
          TipoBoton.Secundario, (s, e) => Ir("P-20"))));
    }

    Control Resumen(List<DiaAgenda> dias)
    {
      Tarjeta t = new Tarjeta();
      int total = dias.Sum(d => d.Turnos);
      t.Controls.Add(Ui.Subtitulo("Se van a generar " + total + " turnos"));
      t.Controls.Add(Ui.Tabla("Vista", Tablas.De(dias.Select(d => new {
        Día = Formato.Corta(d.Dia), Horario = d.Desde + " a " + d.Hasta,
        Consultorio = d.Lugar, Turnos = d.Turnos })), 12));
      t.Controls.Add(Ui.Botones(Ui.Boton("Publicar agenda",
        TipoBoton.Primario, (s, e) => Publicar())));
      return t;
    }

    void Publicar()
    {
      int n = 0;
      if (Guardar(() => n = Agendas.Publicar(Profesional, dtpDesde.Value,
        dtpHasta.Value), null))
      {
        Dialogo.Informar("Se publicaron " + n + " turnos.");
        vista.Controls.Clear();
      }
    }
  }
}
