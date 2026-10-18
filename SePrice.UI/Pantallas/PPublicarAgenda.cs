using System;
using System.Collections.Generic;
using System.Data;
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
    readonly Columna publicada = new Columna();
    DataGridView tblPublicada;

    protected override void ConstruirNormal()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(NuevoCampo("profesional", "Profesional",
        cboProfesional, true), NuevoCampo("periodo", "Desde", dtpDesde,
        true), new Campo("Hasta", dtpHasta, true)));
      t.Controls.Add(Ui.Botones(Ui.Boton("Ver vista previa",
        TipoBoton.Secundario, (s, e) => Previa())));
      Contenido.Controls.Add(t);
      Contenido.Controls.Add(publicada);
      Contenido.Controls.Add(vista);
      cboProfesional.SelectedIndexChanged += (s, e) => Publicada();
    }

    // Agenda ya publicada del profesional elegido, con opción de anular un
    // día cargado por error (CU-A-04).
    void Publicada()
    {
      publicada.Controls.Clear();
      vista.Controls.Clear();
      if (Profesional == 0) return;
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Agenda ya publicada"));
      DataTable d = Agendas.Publicada(Profesional);
      if (d.Rows.Count == 0) t.Controls.Add(Ui.Nota("Todavía no tiene " +
        "agenda publicada de hoy en adelante."));
      else AgregarPublicada(t, d);
      publicada.Controls.Add(t);
    }

    void AgregarPublicada(Tarjeta t, DataTable d)
    {
      tblPublicada = Ui.Tabla("Publicada", d, 8);
      t.Controls.Add(tblPublicada);
      t.Controls.Add(Ui.Botones(Ui.Boton("Anular día elegido",
        TipoBoton.Secundario, (s, e) => Anular())));
    }

    // Anula el día elegido (sólo si no tiene turnos); se puede volver a
    // publicar después de corregir la asignación.
    void Anular()
    {
      int? id = Ui.Elegido(tblPublicada);
      if (id == null) { Dialogo.Error("Elegí en la tabla el día a anular.");
        return; }
      if (!Dialogo.Confirmar("¿Anular ese día de agenda? Deja de ofrecerse " +
        "para dar turnos. Si fue un error, después podés volver a " +
        "publicarlo.", "Anular día", "Anular", true)) return;
      if (Intentar(() => Agendas.Anular(id.Value))) Publicada();
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
      t.Controls.Add(Ui.Subtitulo("Vista previa · todavía no publicada"));
      t.Controls.Add(Ui.Nota("Si está bien, tocá «Publicar agenda» para " +
        "habilitar estos " + total + " turnos."));
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
        Publicada();
      }
    }
  }
}
