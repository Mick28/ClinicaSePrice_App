using System;
using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>
  /// Un día de agenda a publicar (vista previa de P-21 y P-26).
  /// </summary>
  public class DiaAgenda
  {
    public DateTime Dia;
    public string Desde, Hasta, Lugar;
    public int IdLugar, Turnos;
  }

  /// <summary>
  /// CU-G-03 Configurar agendas: publicar la agenda de un profesional
  /// (CU-A-04, P-21) y la de un recurso de estudios (CU-B-05, P-26). Una
  /// agenda publicada es una fila de AGENDA por día.
  /// </summary>
  public static class Agendas
  {
    const string Cu = "CU-G-03";
    const string Modulo = "Publicar agenda (CU-G-03)";

    // ---------- Profesional (Circuito A) ----------

    /// <summary>Días que se van a publicar, según su asignación.</summary>
    public static List<DiaAgenda> Previsualizar(int idProfesional,
      DateTime desde, DateTime hasta)
    {
      Sesion.Exigir(Cu);
      ValidarPeriodo(desde, hasta);
      int duracion = Duracion(idProfesional);
      List<DiaAgenda> dias = new List<DiaAgenda>();
      for (DateTime d = desde.Date; d <= hasta.Date; d = d.AddDays(1))
        dias.AddRange(DelDia(idProfesional, d, duracion));
      ReglaException.Exigir(dias.Count > 0, Nombre(idProfesional) +
        " no tiene consultorio asignado para ese período. Asignalo antes " +
        "de publicar.", "sinConsultorio");
      return dias;
    }

    static List<DiaAgenda> DelDia(int idProf, DateTime d, int duracion)
    {
      return Consulta.Lista("SELECT a.*, c.numero_nombre FROM " +
        "asignacion_de_consultorio a JOIN consultorio_sala c ON " +
        "c.id_consultorio = a.id_consultorio WHERE a.id_profesional = @p0 " +
        "AND a.dia_semana = @p1 AND a.vigencia_desde <= @p2 AND " +
        "(a.vigencia_hasta IS NULL OR a.vigencia_hasta >= @p2) AND NOT " +
        "EXISTS (SELECT 1 FROM agenda g WHERE g.id_profesional = @p0 AND " +
        "g.dia = @p2 AND g.hora_desde = a.hora_desde)",
        f => Dia(f, d, duracion), idProf, Formato.DiaSemana(d),
        Formato.Iso(d));
    }

    static DiaAgenda Dia(Fila f, DateTime d, int duracion)
    {
      DiaAgenda a = new DiaAgenda
      {
        Dia = d, Desde = f.Texto("hora_desde"), Hasta = f.Texto("hora_hasta"),
        IdLugar = f.Entero("id_consultorio"), Lugar = f.Texto("numero_nombre")
      };
      a.Turnos = (Formato.Minutos(a.Hasta) - Formato.Minutos(a.Desde))
        / duracion;
      return a;
    }

    /// <summary>Publica la agenda: devuelve la cantidad de turnos.</summary>
    public static int Publicar(int idProfesional, DateTime desde,
      DateTime hasta)
    {
      List<DiaAgenda> dias = Previsualizar(idProfesional, desde, hasta);
      int turnos = 0;
      Consulta.EnTransaccion(() =>
      {
        foreach (DiaAgenda d in dias) turnos += Insertar(idProfesional, d);
      });
      Auditoria.Alta("A", Modulo, "AGENDA", idProfesional, Nombre(
        idProfesional) + " · " + dias.Count + " días · " + turnos + " turnos");
      return turnos;
    }

    static int Insertar(int idProfesional, DiaAgenda d)
    {
      Consulta.Ejecutar("INSERT INTO agenda (id_profesional, " +
        "id_consultorio, dia, hora_desde, hora_hasta, profesional_ausente)" +
        " VALUES (@p0, @p1, @p2, @p3, @p4, 0)", idProfesional, d.IdLugar,
        Formato.Iso(d.Dia), d.Desde, d.Hasta);
      return d.Turnos;
    }

    /// <summary>
    /// Duración del turno según la especialidad del profesional.
    /// </summary>
    public static int Duracion(int idProfesional)
    {
      int d = Consulta.Entero("SELECT e.duracion_minima FROM profesional p " +
        "JOIN especialidad e ON e.id_especialidad = p.id_especialidad " +
        "WHERE p.id_profesional = @p0", idProfesional);
      return d > 0 ? d : Parametros.Numero("duracion_minima_general");
    }

    static string Nombre(int idProfesional)
    {
      return Consulta.Texto("SELECT apellido_nombre FROM profesional WHERE " +
        "id_profesional = @p0", idProfesional);
    }

    static void ValidarPeriodo(DateTime desde, DateTime hasta)
    {
      ReglaException.Exigir(hasta.Date >= desde.Date, "La fecha hasta no " +
        "puede ser anterior a la fecha desde.", "periodo");
      ReglaException.Exigir((hasta - desde).TotalDays <= 62, "Publicá " +
        "períodos de hasta dos meses.", "periodo");
    }

    /// <summary>
    /// Agenda ya publicada del profesional, de hoy en adelante: cuántos
    /// turnos ofrece cada día y cuántos ya se dieron.
    /// </summary>
    public static DataTable Publicada(int idProfesional)
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT g.id_agenda AS Id, g.dia AS Día, " +
        "g.hora_desde || ' a ' || g.hora_hasta AS Horario, c.numero_nombre" +
        " AS Consultorio, ((substr(g.hora_hasta, 1, 2) * 60 + " +
        "substr(g.hora_hasta, 4, 2)) - (substr(g.hora_desde, 1, 2) * 60 + " +
        "substr(g.hora_desde, 4, 2))) / @p2 AS Turnos, (SELECT COUNT(*) " +
        "FROM turno t WHERE t.id_agenda = g.id_agenda AND t.estado <> " +
        "'cancelado') AS Dados, CASE g.profesional_ausente WHEN 1 THEN " +
        "'Profesional ausente' ELSE '' END AS Observación FROM agenda g " +
        "JOIN consultorio_sala c ON c.id_consultorio = g.id_consultorio " +
        "WHERE g.id_profesional = @p0 AND g.dia >= @p1 ORDER BY g.dia, " +
        "g.hora_desde", idProfesional, Formato.Iso(Reloj.Hoy),
        Duracion(idProfesional));
    }

    /// <summary>
    /// Anula un día de agenda publicado por error, para corregirlo y volver
    /// a publicarlo. Sólo si es de hoy en adelante y no tiene ningún turno
    /// registrado; la anulación queda en la auditoría con el día anulado.
    /// </summary>
    public static void Anular(int idAgenda)
    {
      Sesion.Exigir(Cu);
      Fila g = Consulta.Uno("SELECT * FROM agenda WHERE id_agenda = @p0",
        f => f, idAgenda);
      ReglaException.Exigir(g != null, "Ese día de agenda no existe.");
      ReglaException.Exigir(string.CompareOrdinal(g.Texto("dia"),
        Formato.Iso(Reloj.Hoy)) >= 0, "No se anulan días pasados.");
      int dados = Consulta.Entero("SELECT COUNT(*) FROM turno WHERE " +
        "id_agenda = @p0 AND estado <> 'cancelado'", idAgenda);
      ReglaException.Exigir(dados == 0, "Ese día tiene " + dados +
        " turno(s) dado(s): reprogramalos o cancelalos desde Turnos antes " +
        "de anularlo.");
      ReglaException.Exigir(Consulta.Entero("SELECT COUNT(*) FROM turno " +
        "WHERE id_agenda = @p0", idAgenda) == 0, "Ese día tiene turnos " +
        "cancelados en su historial: no se puede anular.");
      Consulta.Ejecutar("DELETE FROM agenda WHERE id_agenda = @p0", idAgenda);
      Auditoria.Registrar("A", Modulo, "baja", "AGENDA", idAgenda,
        "agenda del " + g.Texto("dia") + " de " + g.Texto("hora_desde") +
        " a " + g.Texto("hora_hasta") + " · profesional " +
        g.Entero("id_profesional"), null);
    }

    /// <summary>
    /// Profesionales con consultorio asignado y sin agenda publicada para
    /// la semana próxima (indicador del Inicio).
    /// </summary>
    public static int SinPublicar()
    {
      return Consulta.Entero("SELECT COUNT(DISTINCT a.id_profesional) FROM " +
        "asignacion_de_consultorio a WHERE (a.vigencia_hasta IS NULL OR " +
        "a.vigencia_hasta >= @p0) AND NOT EXISTS (SELECT 1 FROM agenda g " +
        "WHERE g.id_profesional = a.id_profesional AND g.dia > @p1)",
        Formato.Iso(Reloj.Hoy), Formato.Iso(Reloj.Hoy.AddDays(7)));
    }

    // ---------- Recurso de estudios (Circuito B) ----------

    /// <summary>Vista previa de la agenda de una sala (P-26).</summary>
    public static List<DiaAgenda> PrevisualizarRecurso(AgendaRecurso r)
    {
      Sesion.Exigir(Cu);
      ValidarRecurso(r);
      List<DiaAgenda> dias = new List<DiaAgenda>();
      for (DateTime d = r.Desde.Date; d <= r.Hasta.Date; d = d.AddDays(1))
        if (r.Dias.Contains(Formato.DiaSemana(d)))
          dias.Add(DiaRecurso(r, d));
      ReglaException.Exigir(dias.Count > 0, "No hay días de atención en " +
        "ese período.", "periodo");
      return dias;
    }

    static DiaAgenda DiaRecurso(AgendaRecurso r, DateTime d)
    {
      string choque = Choque(r, d);
      ReglaException.Exigir(choque == null, choque, "horario");
      return new DiaAgenda
      {
        Dia = d, Desde = r.HoraDesde, Hasta = r.HoraHasta,
        IdLugar = r.IdSala, Lugar = r.Sala,
        Turnos = (Formato.Minutos(r.HoraHasta) -
          Formato.Minutos(r.HoraDesde)) / Math.Max(1, r.DuracionEstudio)
      };
    }

    // Impide la doble reserva del mismo recurso (CU-B-05).
    static string Choque(AgendaRecurso r, DateTime d)
    {
      return Consulta.Uno("SELECT 'La sala ' || c.numero_nombre || ' ya " +
        "tiene agenda publicada el ' || @p3 || ' de ' || g.hora_desde || " +
        "' a ' || g.hora_hasta || '. Elegí otro horario.' AS m FROM agenda " +
        "g JOIN consultorio_sala c ON c.id_consultorio = g.id_consultorio " +
        "WHERE g.id_consultorio = @p0 AND g.dia = @p1 AND g.hora_desde < " +
        "@p4 AND g.hora_hasta > @p2", f => f.Texto("m"), r.IdSala,
        Formato.Iso(d), r.HoraDesde, Formato.Corta(d), r.HoraHasta);
    }

    static void ValidarRecurso(AgendaRecurso r)
    {
      ReglaException.Exigir(r.IdSala > 0, "Elegí la sala.", "sala");
      ReglaException.Exigir(r.IdTecnico > 0, "Elegí el técnico/a.",
        "tecnico");
      ReglaException.Exigir(r.Dias.Count > 0, "Elegí al menos un día.",
        "dias");
      ReglaException.Exigir(Formato.Minutos(r.HoraHasta) >
        Formato.Minutos(r.HoraDesde), "La hora de fin tiene que ser " +
        "posterior a la de inicio.", "horario");
      ValidarPeriodo(r.Desde, r.Hasta);
    }

    /// <summary>Publica la agenda de la sala; devuelve los turnos.</summary>
    public static int PublicarRecurso(AgendaRecurso r)
    {
      List<DiaAgenda> dias = PrevisualizarRecurso(r);
      int turnos = 0;
      Consulta.EnTransaccion(() =>
      {
        foreach (DiaAgenda d in dias) turnos += Insertar(r.IdTecnico, d);
      });
      Auditoria.Alta("B", Modulo, "AGENDA", r.IdSala, r.Sala + " · " +
        dias.Count + " días · " + r.HoraDesde + "-" + r.HoraHasta);
      return turnos;
    }
  }

  /// <summary>Datos para publicar la agenda de un recurso (P-26).</summary>
  public class AgendaRecurso
  {
    public int IdSala, IdTecnico, DuracionEstudio;
    public string Sala, HoraDesde, HoraHasta;
    public DateTime Desde, Hasta;
    public List<string> Dias = new List<string>();
  }
}
