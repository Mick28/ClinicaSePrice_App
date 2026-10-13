using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-09 Reprogramar o cancelar turno (CU-A-08, CU-B-07): turnos del
  /// paciente y turnos afectados por un profesional ausente (CU-A-17).
  /// </summary>
  class PReprogramar : Pantalla
  {
    DataGridView tabla;
    readonly Columna opciones = new Columna();
    string pestana;

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Turnos de hoy", Consultas.TurnosDelDia(Reloj.Hoy));
    }

    /// <summary>Parámetro para abrir directo en «Turnos afectados».</summary>
    public const string VerAfectados = "afectados";

    protected override void ConstruirNormal()
    {
      AgregarBarraPaciente();
      Pestanas p = new Pestanas { AlElegir = Elegir };
      p.Agregar("Turnos del paciente");
      p.Agregar("Turnos afectados (" + Turnos.Afectados().Rows.Count + ")");
      Contenido.Controls.Add(p);
      Contenido.Controls.Add(opciones);
      p.Iniciar(Equals(Parametro, VerAfectados) ? 1 : 0);
    }

    void Elegir(string nombre, Columna area)
    {
      pestana = nombre;
      opciones.Controls.Clear();
      DataTable datos = Datos();
      if (Afectados && datos.Rows.Count > 0) area.Controls.Add(Ui.Aviso(
        TipoAviso.Advertencia, "Agenda marcada «profesional ausente»: " +
        Presentismo.AusentesHoy() + ". Hay " + datos.Rows.Count + " turnos " +
        "para reprogramar o cancelar. Avisá a los pacientes."));
      if (!Afectados && PacienteEnCurso.Actual == null) area.Controls.Add(
        Ui.Nota("Buscá un paciente (Pacientes) para ver sus turnos."));
      tabla = Ui.Tabla("Turnos", datos, 10);
      area.Controls.Add(tabla);
      area.Controls.Add(Ui.BotonesIzq(Ui.Boton("Reprogramar",
        TipoBoton.Secundario, (s, e) => Reprogramar()), Ui.Boton("Cancelar " +
        "turno", TipoBoton.Peligro, (s, e) => Cancelar())));
    }

    bool Afectados
    {
      get { return pestana != null && pestana.StartsWith("Turnos afectados"); }
    }

    DataTable Datos()
    {
      if (Afectados) return Turnos.Afectados();
      return PacienteEnCurso.Actual == null ? new DataTable()
        : Turnos.DelPaciente(PacienteEnCurso.Actual.Id);
    }

    int? Turno()
    {
      int? id = Ui.Elegido(tabla);
      if (id == null) Dialogo.Error("Elegí un turno de la tabla.");
      return id;
    }

    // Reprogramar: franjas del mismo profesional o recurso; al elegir, se
    // libera la anterior y se emite el comprobante actualizado.
    void Reprogramar()
    {
      int? id = Turno();
      if (id == null) return;
      opciones.Controls.Clear();
      List<Franja> l = Turnos.ParaReprogramar(id.Value);
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Elegí el turno nuevo"));
      Botonera b = new Botonera();
      foreach (Franja f in l.Take(40)) b.Controls.Add(Opcion(id.Value, f));
      t.Controls.Add(l.Count == 0 ? (Control)Ui.Nota("No hay franjas " +
        "libres en las próximas dos semanas.") : b);
      opciones.Controls.Add(t);
    }

    Button Opcion(int idTurno, Franja f)
    {
      Button b = Ui.Boton(Formato.Corta(f.Inicio).Substring(0, 9) + " " +
        Formato.Hora(f.Inicio), TipoBoton.Secundario, null);
      b.Click += (s, e) => { if (Guardar(() => Turnos.Reprogramar(idTurno,
        f), null)) Ir("P-08", idTurno); };
      return b;
    }

    void Cancelar()
    {
      int? id = Turno();
      if (id == null) return;
      string motivo = Dialogo.Ingresar("Ingresá el motivo de la " +
        "cancelación.", "Cancelar turno", Turnos.Motivos);
      if (motivo == null || !Dialogo.Confirmar("La franja vuelve a quedar " +
        "libre y el turno queda «cancelado». ¿Cancelar el turno?",
        "Cancelar turno", "Cancelar turno", true)) return;
      if (Intentar(() => Turnos.Cancelar(id.Value, motivo)))
        Ui.Cargar(tabla, Datos(), 10);
    }
  }
}
