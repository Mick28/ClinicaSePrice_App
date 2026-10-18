using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Franja libre de una agenda publicada.</summary>
  public class Franja
  {
    public int IdAgenda, Duracion;
    public int? IdEstudio;
    public DateTime Inicio;
    public string Profesional, Lugar, Ubicacion;

    /// <summary>
    /// Ya reservada (o pasada): se muestra en el calendario, pero no se
    /// puede elegir.
    /// </summary>
    public bool Ocupada;

    public override string ToString()
    {
      return Formato.Corta(Inicio) + " · " + Formato.Hora(Inicio);
    }
  }

  /// <summary>
  /// CU-G-04 Gestionar turnos (CU-A-05, A-06, A-08, B-06, B-07): consultar
  /// disponibilidad, asignar, reprogramar y cancelar. Respeta la duración
  /// por especialidad o estudio, las agendas publicadas y la franja de
  /// ayuno (07:00 a 11:00).
  /// </summary>
  public static partial class Turnos
  {
    const string Cu = "CU-G-04", Modulo = "Turnos (CU-G-04)";

    // ---------- Consultar disponibilidad ----------

    /// <summary>
    /// Franjas libres de una especialidad (Circuito A). Con «todas», también
    /// las ocupadas, marcadas (calendario semanal).
    /// </summary>
    public static List<Franja> Disponibles(int idEspecialidad,
      int? idProfesional, DateTime desde, int dias = 7, bool todas = false)
    {
      Sesion.Exigir(Cu);
      List<Franja> l = new List<Franja>();
      foreach (Fila a in AgendasA(idEspecialidad, idProfesional, desde, dias))
        l.AddRange(Libres(a, Agendas.Duracion(a.Entero("id_profesional")),
          null, 0, 24 * 60, todas));
      return l.OrderBy(f => f.Inicio).ToList();
    }

    static List<Fila> AgendasA(int idEsp, int? idProf, DateTime desde,
      int dias)
    {
      return Consulta.Lista("SELECT a.*, p.apellido_nombre AS prof, " +
        "c.numero_nombre AS lugar, c.ubicacion FROM agenda a JOIN " +
        "profesional p ON p.id_profesional = a.id_profesional JOIN " +
        "consultorio_sala c ON c.id_consultorio = a.id_consultorio WHERE " +
        "p.id_especialidad = @p0 AND (@p1 IS NULL OR a.id_profesional = " +
        "@p1) AND a.dia BETWEEN @p2 AND @p3 AND a.profesional_ausente = 0 " +
        "ORDER BY a.dia, a.hora_desde", f => f, idEsp, idProf,
        Formato.Iso(Desde(desde)), Formato.Iso(Desde(desde).AddDays(dias)));
    }

    static DateTime Desde(DateTime d)
    {
      return d.Date < Reloj.Hoy ? Reloj.Hoy : d.Date;
    }

    /// <summary>Franjas libres de un estudio con turno (Circuito B).</summary>
    public static List<Franja> DisponiblesEstudio(int idEstudio,
      DateTime desde, int dias = 7, bool todas = false)
    {
      Sesion.Exigir(Cu);
      Estudio e = Catalogo.PorId(idEstudio);
      ReglaException.Exigir(e.RequiereTurno, "Los estudios de laboratorio" +
        " no requieren turno: se atienden por orden de llegada.", "lab");
      int[] rango = e.RequiereAyuno ? Parametros.Franja(Parametros.Valor(
        "lab_con_ayuno")) : new[] { 0, 24 * 60 };
      List<Franja> l = new List<Franja>();
      foreach (Fila a in AgendasB(e, desde, dias))
        l.AddRange(Libres(a, e.Duracion ?? 15, e.Id, rango[0], rango[1],
          todas));
      return l.OrderBy(f => f.Inicio).ToList();
    }

    static List<Fila> AgendasB(Estudio e, DateTime desde, int dias)
    {
      return Consulta.Lista("SELECT a.*, p.apellido_nombre AS prof, " +
        "c.numero_nombre AS lugar, c.ubicacion FROM agenda a JOIN " +
        "profesional p ON p.id_profesional = a.id_profesional JOIN " +
        "consultorio_sala c ON c.id_consultorio = a.id_consultorio WHERE " +
        "c.numero_nombre = @p0 AND a.dia BETWEEN @p1 AND @p2 ORDER BY " +
        "a.dia, a.hora_desde", f => f, e.SalaEquipo,
        Formato.Iso(Desde(desde)), Formato.Iso(Desde(desde).AddDays(dias)));
    }

    // Recorre la agenda en pasos de la duración y descarta lo ocupado.
    static List<Franja> Libres(Fila a, int duracion, int? idEstudio,
      int desdeMin, int hastaMin, bool todas = false)
    {
      DateTime dia = Formato.Leer(a.Texto("dia"));
      List<int[]> ocupado = Ocupado(a.Entero("id_agenda"));
      List<Franja> l = new List<Franja>();
      int fin = Math.Min(Formato.Minutos(a.Texto("hora_hasta")), hastaMin);
      for (int m = Formato.Minutos(a.Texto("hora_desde")); m + duracion <=
        fin; m += duracion)
      {
        if (m < desdeMin) continue;
        bool libre = Libre(ocupado, m, duracion) &&
          dia.AddMinutes(m) > Reloj.Ahora;
        if (!libre && !todas) continue;
        Franja f = Nueva(a, dia.AddMinutes(m), duracion, idEstudio);
        f.Ocupada = !libre;
        l.Add(f);
      }
      return l;
    }

    static bool Libre(List<int[]> ocupado, int m, int duracion)
    {
      return !ocupado.Any(o => o[0] < m + duracion && o[1] > m);
    }

    // Intervalos ocupados por turnos de agenda no cancelados (los
    // sobreturnos no ocupan franja: se suman a la hora).
    static List<int[]> Ocupado(int idAgenda)
    {
      return Consulta.Lista("SELECT substr(t.fecha_hora, 12, 5) AS h, " +
        "COALESCE(e.duracion, (SELECT es.duracion_minima FROM agenda g " +
        "JOIN profesional p ON p.id_profesional = g.id_profesional JOIN " +
        "especialidad es ON es.id_especialidad = p.id_especialidad WHERE " +
        "g.id_agenda = t.id_agenda), 15) AS d FROM turno t LEFT JOIN " +
        "detalle_turno_estudio x ON x.id_turno = t.id_turno LEFT JOIN " +
        "estudio e ON e.id_estudio = x.id_estudio WHERE t.id_agenda = @p0 " +
        "AND t.estado <> 'cancelado' AND t.procedencia = 'agenda'",
        f => new[] { Formato.Minutos(f.Texto("h")),
          Formato.Minutos(f.Texto("h")) + f.Entero("d") }, idAgenda);
    }

    static Franja Nueva(Fila a, DateTime inicio, int dur, int? idEstudio)
    {
      return new Franja
      {
        IdAgenda = a.Entero("id_agenda"), Inicio = inicio, Duracion = dur,
        IdEstudio = idEstudio, Profesional = a.Texto("prof"),
        Lugar = a.Texto("lugar"), Ubicacion = a.Texto("ubicacion")
      };
    }

    /// <summary>
    /// Primera fecha con franjas libres en los próximos dos meses (para
    /// ofrecerla cuando la agenda está completa, CU-A-05).
    /// </summary>
    public static DateTime? PrimeraLibre(int idEspecialidad, int? idProf,
      DateTime desde)
    {
      List<Franja> l = Disponibles(idEspecialidad, idProf, desde, 60);
      return l.Count == 0 ? (DateTime?)null : l[0].Inicio.Date;
    }

    // ---------- Asignar ----------

    /// <summary>
    /// Reserva la franja para el paciente y devuelve el turno.
    /// </summary>
    public static int Asignar(int idPaciente, Franja f, string modalidad)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(modalidad == "personal" ||
        modalidad == "telefónica", "Elegí la modalidad de toma: personal " +
        "o telefónica.", "modalidad");
      ExigirLibre(f);
      int id = 0;
      Consulta.EnTransaccion(() => id = Insertar(idPaciente, f, modalidad));
      Auditoria.Alta(f.IdEstudio.HasValue ? "B" : "A", Modulo, "TURNO", id,
        "paciente " + idPaciente + " · " + Formato.IsoHora(f.Inicio));
      return id;
    }

    static void ExigirLibre(Franja f)
    {
      int m = Formato.Minutos(Formato.Hora(f.Inicio));
      ReglaException.Exigir(Libre(Ocupado(f.IdAgenda), m, f.Duracion),
        "Esa franja acaba de ocuparse. Elegí otra.", "franja");
    }

    static int Insertar(int idPaciente, Franja f, string modalidad)
    {
      int id = Consulta.Insertar("INSERT INTO turno (id_paciente, " +
        "id_agenda, id_empleado, fecha_hora, estado, procedencia, " +
        "modalidad_toma) VALUES (@p0, @p1, @p2, @p3, 'reservado', " +
        "'agenda', @p4)", idPaciente, f.IdAgenda, Sesion.Actual.IdEmpleado,
        Formato.IsoHora(f.Inicio), modalidad);
      if (f.IdEstudio.HasValue) Consulta.Ejecutar("INSERT INTO " +
        "detalle_turno_estudio (id_turno, id_estudio) VALUES (@p0, @p1)",
        id, f.IdEstudio.Value);
      return id;
    }

    // ---------- Reprogramar y cancelar ----------

    /// <summary>Turnos vigentes (reservados) del paciente.</summary>
    public static DataTable DelPaciente(int idPaciente)
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla(SelectVigentes + "WHERE t.id_paciente = @p0 " +
        "AND t.estado = 'reservado' AND t.fecha_hora >= @p1 ORDER BY " +
        "t.fecha_hora", idPaciente, Formato.Iso(Reloj.Hoy));
    }

    /// <summary>
    /// Turnos de agendas marcadas «profesional ausente» (CU-A-08), para
    /// reprogramarlos o cancelarlos.
    /// </summary>
    public static DataTable Afectados()
    {
      Sesion.ExigirLectura(Cu);
      Presentismo.ControlarAusencias();
      return Consulta.Tabla(SelectVigentes + "WHERE a.profesional_ausente " +
        "= 1 AND t.estado = 'reservado' AND a.dia >= @p0 ORDER BY " +
        "t.fecha_hora", Formato.Iso(Reloj.Hoy));
    }

    const string SelectVigentes = "SELECT t.id_turno AS Id, t.fecha_hora " +
      "AS [Fecha y hora], pa.apellido_nombre AS Paciente, pa.dni AS DNI, " +
      "pa.telefono AS Teléfono, CASE WHEN x.id_estudio IS NULL THEN 'A' " +
      "ELSE 'B' END AS Circuito, COALESCE(e.nombre, p.apellido_nombre) " +
      "AS [Profesional o estudio], t.procedencia AS Origen, t.estado AS " +
      "Estado FROM turno t JOIN paciente pa ON pa.id_paciente = " +
      "t.id_paciente LEFT JOIN agenda a ON a.id_agenda = t.id_agenda LEFT " +
      "JOIN profesional p ON p.id_profesional = a.id_profesional LEFT JOIN" +
      " detalle_turno_estudio x ON x.id_turno = t.id_turno LEFT JOIN " +
      "estudio e ON e.id_estudio = x.id_estudio ";

    /// <summary>Mueve el turno a otra franja (libera la anterior).</summary>
    public static void Reprogramar(int idTurno, Franja nueva)
    {
      Sesion.Exigir(Cu);
      ExigirLibre(nueva);
      string antes = Consulta.Texto("SELECT fecha_hora FROM turno WHERE " +
        "id_turno = @p0 AND estado = 'reservado'", idTurno);
      ReglaException.Exigir(antes != null, "Sólo se reprograman turnos " +
        "reservados.");
      Consulta.Ejecutar("UPDATE turno SET id_agenda = @p0, fecha_hora = " +
        "@p1 WHERE id_turno = @p2", nueva.IdAgenda,
        Formato.IsoHora(nueva.Inicio), idTurno);
      Auditoria.Cambio(nueva.IdEstudio.HasValue ? "B" : "A", Modulo,
        "TURNO", idTurno, "fecha_hora: " + antes, "fecha_hora: " +
        Formato.IsoHora(nueva.Inicio));
    }

    /// <summary>Motivos de cancelación (especificación, P-09).</summary>
    public static readonly string[] Motivos = { "Pedido del paciente",
      "Profesional ausente", "Error de carga", "Otro" };

    /// <summary>Cancela el turno con su motivo y libera la franja.</summary>
    public static void Cancelar(int idTurno, string motivo)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(motivo),
        "Ingresá el motivo de la cancelación.", "motivo");
      int n = Consulta.Ejecutar("UPDATE turno SET estado = 'cancelado', " +
        "motivo_cancelacion = @p0 WHERE id_turno = @p1 AND estado = " +
        "'reservado'", motivo.Trim(), idTurno);
      ReglaException.Exigir(n == 1, "Sólo se cancelan turnos reservados.");
      Auditoria.Cambio("ambos", Modulo, "TURNO", idTurno, "estado: " +
        "reservado", "estado: cancelado · " + motivo);
    }

    // ---------- Indicadores ----------

    /// <summary>Turnos del día (Inicio del personal administrativo).</summary>
    public static int DelDia()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno WHERE fecha_hora " +
        "LIKE @p0 AND estado <> 'cancelado'", Formato.Iso(Reloj.Hoy) + "%");
    }

    /// <summary>Pacientes acreditados que esperan ser llamados hoy.</summary>
    public static int EnEspera()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno WHERE fecha_hora " +
        "LIKE @p0 AND estado = 'acreditado'", Formato.Iso(Reloj.Hoy) + "%");
    }
  }
}
