using System;
using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Datos del comprobante de turno (CU-A-09, P-08).</summary>
  public class Comprobante
  {
    public int Numero;
    public string Paciente, Circuito, Detalle, Lugar, Modalidad, Indicaciones;
    public DateTime FechaHora, Emision;
    public bool Sobreturno;
  }

  public static partial class Turnos
  {
    // ---------- Sobreturno (CU-A-07, demanda sin turno CU-G-05) ----------

    /// <summary>
    /// Horas de la agenda que todavía tienen cupo de sobreturno.
    /// </summary>
    public static List<string> HorasConCupo(int idAgenda)
    {
      Sesion.Exigir("CU-G-05");
      Fila a = Consulta.Uno("SELECT * FROM agenda WHERE id_agenda = @p0",
        f => f, idAgenda);
      List<string> horas = new List<string>();
      for (int m = Formato.Minutos(a.Texto("hora_desde")) / 60 * 60;
        m < Formato.Minutos(a.Texto("hora_hasta")); m += 60)
        if (Sobreturnos(idAgenda, Formato.HoraDe(m)) < Cupo())
          horas.Add(Formato.HoraDe(m));
      return horas;
    }

    static int Cupo() { return Parametros.Numero("sobreturnos_por_hora"); }

    static int Sobreturnos(int idAgenda, string hora)
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno WHERE id_agenda = " +
        "@p0 AND procedencia = 'sobreturno' AND estado <> 'cancelado' AND " +
        "substr(fecha_hora, 12, 2) = @p1", idAgenda, hora.Substring(0, 2));
    }

    /// <summary>
    /// Asigna un sobreturno si la hora tiene cupo (1 por hora).
    /// </summary>
    public static int AsignarSobreturno(int idPaciente, int idAgenda,
      string hora, string modalidad)
    {
      Sesion.Exigir("CU-G-05");
      Fila a = Consulta.Uno("SELECT a.*, p.apellido_nombre AS prof FROM " +
        "agenda a JOIN profesional p ON p.id_profesional = a.id_profesional" +
        " WHERE a.id_agenda = @p0", f => f, idAgenda);
      ReglaException.Exigir(Sobreturnos(idAgenda, hora) < Cupo(), "Ya hay " +
        "un sobreturno a las " + hora + " para " + a.Texto("prof") + ". Se " +
        "permite " + Cupo() + " sobreturno por hora. Elegí otra hora.",
        "hora");
      int id = Consulta.Insertar("INSERT INTO turno (id_paciente, " +
        "id_agenda, id_empleado, fecha_hora, estado, procedencia, " +
        "modalidad_toma) VALUES (@p0, @p1, @p2, @p3, 'reservado', " +
        "'sobreturno', @p4)", idPaciente, idAgenda, Sesion.Actual.IdEmpleado,
        a.Texto("dia") + " " + hora, modalidad);
      Auditoria.Alta("A", "Sobreturno (CU-G-05)", "TURNO", id,
        "sobreturno " + a.Texto("dia") + " " + hora);
      return id;
    }

    /// <summary>
    /// Agendas del día de un profesional (para el sobreturno).
    /// </summary>
    public static List<Opcion> AgendasDelDia(int idEspecialidad, DateTime dia)
    {
      return Consulta.Lista("SELECT a.id_agenda AS id, p.apellido_nombre " +
        "|| ' · ' || a.hora_desde || ' a ' || a.hora_hasta AS texto FROM " +
        "agenda a JOIN profesional p ON p.id_profesional = a.id_profesional" +
        " WHERE p.id_especialidad = @p0 AND a.dia = @p1 AND " +
        "a.profesional_ausente = 0", Opcion.Leer, idEspecialidad,
        Formato.Iso(dia));
    }

    // ---------- Comprobante (CU-A-09) ----------

    /// <summary>Datos del comprobante de un turno.</summary>
    public static Comprobante Comprobante(int idTurno)
    {
      Sesion.ExigirLectura("CU-G-04");
      return Consulta.Uno("SELECT t.*, pa.apellido_nombre AS pac, " +
        "pa.dni, COALESCE(e.nombre, p.apellido_nombre || ' · ' || " +
        "es.nombre) AS det, c.numero_nombre || ' · ' || c.ubicacion AS " +
        "lugar, e.requiere_ayuno AS ayuno, x.id_estudio FROM turno t JOIN " +
        "paciente pa ON pa.id_paciente = t.id_paciente LEFT JOIN agenda a " +
        "ON a.id_agenda = t.id_agenda LEFT JOIN profesional p ON " +
        "p.id_profesional = a.id_profesional LEFT JOIN especialidad es ON " +
        "es.id_especialidad = p.id_especialidad LEFT JOIN consultorio_sala" +
        " c ON c.id_consultorio = a.id_consultorio LEFT JOIN " +
        "detalle_turno_estudio x ON x.id_turno = t.id_turno LEFT JOIN " +
        "estudio e ON e.id_estudio = x.id_estudio WHERE t.id_turno = @p0",
        LeerComprobante, idTurno);
    }

    static Comprobante LeerComprobante(Fila f)
    {
      bool estudio = !f.Vacio("id_estudio");
      return new Comprobante
      {
        Numero = f.Entero("id_turno"), Paciente = f.Texto("pac") + " · DNI " +
          Formato.Dni(f.Entero("dni")),
        Circuito = estudio ? "Estudios clínicos" : "Consultorios externos",
        Detalle = f.Texto("det"), Lugar = f.Texto("lugar"),
        Modalidad = f.Texto("modalidad_toma"),
        FechaHora = Formato.Leer(f.Texto("fecha_hora")),
        Emision = Reloj.Ahora,
        Sobreturno = f.Texto("procedencia") == "sobreturno",
        Indicaciones = Indicaciones(estudio, f.Logico("ayuno"))
      };
    }

    static string Indicaciones(bool estudio, bool ayuno)
    {
      if (!estudio) return "Presentarse 10 minutos antes con el documento " +
        "y la credencial de la obra social.";
      return (ayuno ? "Concurrir con 8 horas de ayuno. " : "") +
        "Traer la orden médica y la credencial de la obra social.";
    }

    /// <summary>
    /// Franjas para reprogramar: del mismo profesional (A) o del mismo
    /// recurso y estudio (B), desde hoy y por dos semanas.
    /// </summary>
    public static List<Franja> ParaReprogramar(int idTurno)
    {
      Fila t = Consulta.Uno("SELECT p.id_especialidad, a.id_profesional, " +
        "x.id_estudio FROM turno t LEFT JOIN agenda a ON a.id_agenda = " +
        "t.id_agenda LEFT JOIN profesional p ON p.id_profesional = " +
        "a.id_profesional LEFT JOIN detalle_turno_estudio x ON x.id_turno =" +
        " t.id_turno WHERE t.id_turno = @p0", f => f, idTurno);
      if (!t.Vacio("id_estudio"))
        return DisponiblesEstudio(t.Entero("id_estudio"), Reloj.Hoy, 14);
      return Disponibles(t.Entero("id_especialidad"),
        t.Entero("id_profesional"), Reloj.Hoy, 14);
    }

    // ---------- Turnos de control pedidos en la atención (CU-A-13) ----------

    /// <summary>
    /// Controles pedidos por el profesional y todavía sin turno.
    /// </summary>
    public static DataTable ControlesPendientes()
    {
      Sesion.ExigirLectura("CU-G-04");
      return Consulta.Tabla("SELECT pa.dni AS DNI, pa.apellido_nombre AS " +
        "Paciente, p.apellido_nombre AS Profesional, date(substr(" +
        "t.fecha_hora, 1, 10), '+' || at.plazo_control_dias || ' days') AS " +
        "[Control sugerido] FROM atencion at JOIN turno t ON t.id_turno = " +
        "at.id_turno JOIN paciente pa ON pa.id_paciente = t.id_paciente " +
        "JOIN agenda a ON a.id_agenda = t.id_agenda JOIN profesional p ON " +
        "p.id_profesional = a.id_profesional WHERE at.requiere_control = 1 " +
        "AND NOT EXISTS (SELECT 1 FROM turno t2 JOIN agenda a2 ON " +
        "a2.id_agenda = t2.id_agenda WHERE t2.id_paciente = t.id_paciente " +
        "AND a2.id_profesional = a.id_profesional AND t2.fecha_hora > " +
        "t.fecha_hora AND t2.estado <> 'cancelado') ORDER BY 4");
    }
  }
}
