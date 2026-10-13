using System;
using System.Collections.Generic;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Paciente en la sala de espera o en la cola (P-13).</summary>
  public class EnEspera
  {
    public int IdTurno, Edad;
    public string Hora, Paciente, Estado, Etiqueta, Detalle, Lugar;
    public int? IdEstudio;
    public int Espera;
  }

  /// <summary>
  /// CU-G-08 Atender al paciente y registrar la prestación: sala de espera
  /// y llamado (CU-A-12), registro de la atención y turno de control
  /// (CU-A-13), cola de estudios (CU-B-12). El profesional y el técnico no
  /// ven obra social, teléfono ni DNI (5.3).
  /// </summary>
  public static partial class Atencion
  {
    const string Cu = "CU-G-08", Modulo = "Atención (CU-G-08)";

    static int Yo
    {
      get { return Sesion.Actual.IdProfesional ?? 0; }
    }

    const string Select = "SELECT t.id_turno, substr(t.fecha_hora, 12) AS " +
      "hora, t.fecha_hora, p.apellido_nombre AS pac, p.fecha_nacimiento, " +
      "t.estado, t.procedencia, t.prioridad, x.id_estudio, e.nombre AS est," +
      " COALESCE(c.numero_nombre, e.sala_equipo) AS lugar FROM turno t " +
      "JOIN paciente p ON p.id_paciente = t.id_paciente LEFT JOIN agenda a " +
      "ON a.id_agenda = t.id_agenda LEFT JOIN consultorio_sala c ON " +
      "c.id_consultorio = a.id_consultorio LEFT JOIN detalle_turno_estudio" +
      " x ON x.id_turno = t.id_turno LEFT JOIN estudio e ON e.id_estudio = " +
      "x.id_estudio ";

    /// <summary>
    /// Sala de espera del médico: sólo con ingreso registrado.
    /// </summary>
    public static List<EnEspera> SalaDeEspera()
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(Presentismo.Presente(Yo), "Para ver tu sala " +
        "de espera, registrá tu ingreso.", "presentismo");
      return Consulta.Lista(Select + "WHERE a.id_profesional = @p0 AND " +
        "a.dia = @p1 AND t.estado IN ('acreditado', 'en atención') ORDER " +
        "BY t.fecha_hora, t.procedencia", Leer, Yo, Formato.Iso(Reloj.Hoy));
    }

    /// <summary>
    /// Cola del técnico: primero las urgencias, luego los turnos por
    /// horario (CU-B-12). Sólo las salas donde atiende hoy.
    /// </summary>
    public static List<EnEspera> ColaDeEstudios()
    {
      Sesion.Exigir(Cu);
      return Consulta.Lista(Select + "WHERE t.estado IN ('acreditado', " +
        "'en atención') AND t.fecha_hora LIKE @p1 AND x.id_estudio IS NOT " +
        "NULL AND COALESCE(c.numero_nombre, e.sala_equipo) IN (SELECT " +
        "c2.numero_nombre FROM agenda g JOIN consultorio_sala c2 ON " +
        "c2.id_consultorio = g.id_consultorio WHERE g.id_profesional = @p0" +
        " AND g.dia = @p2) ORDER BY CASE t.procedencia WHEN 'guardia' THEN " +
        "0 WHEN 'agenda' THEN 1 ELSE 2 END, CASE t.prioridad WHEN " +
        "'urgente' THEN 0 ELSE 1 END, t.fecha_hora", Leer, Yo,
        Formato.Iso(Reloj.Hoy) + "%", Formato.Iso(Reloj.Hoy));
    }

    /// <summary>
    /// Datos del paciente de un turno (sin cobertura ni contacto).
    /// </summary>
    public static EnEspera Detalle(int idTurno)
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Uno(Select + "WHERE t.id_turno = @p0", Leer, idTurno);
    }

    /// <summary>Salas donde atiende hoy el técnico de la sesión.</summary>
    public static string MisSalas()
    {
      string s = Consulta.Texto("SELECT group_concat(DISTINCT " +
        "c.numero_nombre) FROM agenda g JOIN consultorio_sala c ON " +
        "c.id_consultorio = g.id_consultorio WHERE g.id_profesional = @p0" +
        " AND g.dia = @p1", Yo, Formato.Iso(Reloj.Hoy));
      return s == null ? null : s.Replace(",", ", ");
    }

    static EnEspera Leer(Fila f)
    {
      DateTime t = Formato.Leer(f.Texto("fecha_hora"));
      return new EnEspera
      {
        IdTurno = f.Entero("id_turno"), Hora = f.Texto("hora"),
        Paciente = f.Texto("pac"),
        Edad = Formato.Edad(f.Texto("fecha_nacimiento")),
        Estado = f.Texto("estado"), Etiqueta = Etiqueta(f),
        IdEstudio = f.EnteroONulo("id_estudio"), Detalle = f.Texto("est"),
        Lugar = f.Texto("lugar"),
        Espera = Math.Max(0, (int)(Reloj.Ahora - t).TotalMinutes)
      };
    }

    // Origen del turno: no es un estado, es una etiqueta (11.5).
    static string Etiqueta(Fila f)
    {
      switch (f.Texto("procedencia"))
      {
        case "sobreturno": return "Sobreturno";
        case "guardia": return "Urgencia";
        case "orden de llegada": return "Sin turno";
        default: return null;
      }
    }

    /// <summary>Llama al paciente: el turno pasa a «en atención».</summary>
    public static void Llamar(int idTurno)
    {
      Sesion.Exigir(Cu);
      int n = Consulta.Ejecutar("UPDATE turno SET estado = 'en atención' " +
        "WHERE id_turno = @p0 AND estado = 'acreditado'", idTurno);
      ReglaException.Exigir(n == 1, "Ese paciente ya no está en espera.");
      if (Sesion.Actual.Perfil == Perfil.Medico)
        Consulta.Ejecutar("INSERT INTO atencion (id_turno, hora_inicio, " +
          "requiere_control) VALUES (@p0, @p1, 0)", idTurno,
          Formato.Hora(Reloj.Ahora));
      Auditoria.Cambio(Sesion.Actual.Perfil == Perfil.Medico ? "A" : "B",
        Modulo, "TURNO", idTurno, "estado: acreditado", "estado: en atención");
    }

    /// <summary>Hora de inicio de la atención (al llamar).</summary>
    public static string Inicio(int idTurno)
    {
      return Consulta.Texto("SELECT hora_inicio FROM atencion WHERE " +
        "id_turno = @p0", idTurno);
    }

    /// <summary>
    /// Registra la atención (hora de fin) y, si corresponde, el pedido de
    /// turno de control con su plazo (CU-A-13). El turno queda «atendido».
    /// </summary>
    public static void RegistrarAtencion(int idTurno, bool control,
      int plazoDias)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(!control || plazoDias > 0, "Elegí el plazo " +
        "del turno de control.", "plazo");
      Consulta.EnTransaccion(() =>
      {
        Consulta.Ejecutar("UPDATE atencion SET hora_fin = @p0, " +
          "requiere_control = @p1, plazo_control_dias = @p2 WHERE id_turno" +
          " = @p3", Formato.Hora(Reloj.Ahora), control,
          control ? (object)plazoDias : null, idTurno);
        Consulta.Ejecutar("UPDATE turno SET estado = 'atendido' WHERE " +
          "id_turno = @p0 AND estado = 'en atención'", idTurno);
      });
      Auditoria.Cambio("A", Modulo, "ATENCIÓN", idTurno, "estado: en " +
        "atención", "estado: atendido" + (control ? " · control a " +
        plazoDias + " días" : ""));
    }
  }
}
