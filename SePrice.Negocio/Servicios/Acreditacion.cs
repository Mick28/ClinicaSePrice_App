using System;
using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Turno de hoy del paciente, listo para acreditar.</summary>
  public class TurnoDeHoy
  {
    public int IdTurno;
    public int? IdEstudio;
    public string Hora, Detalle, Lugar, Procedencia;

    /// <summary>Estudios clínicos (B) o consultorios externos (A).</summary>
    public bool EsEstudio { get { return IdEstudio.HasValue; } }

    public override string ToString()
    {
      return Hora + " · " + Detalle + " · " + Lugar;
    }
  }

  /// <summary>Resultado de validar la cobertura (CU-G-07).</summary>
  public class Validacion
  {
    public int Id;
    public string Resultado, Referencia, Mensaje;
  }

  /// <summary>Orden médica presentada para un estudio (CU-B-11).</summary>
  public class DatosOrden
  {
    public string Solicitante, Matricula, Estudios;
    public DateTime Emision, Vencimiento;
  }

  /// <summary>
  /// CU-G-06 Acreditar paciente y registrar modalidad de pago (CU-A-10,
  /// CU-B-10), que incluye CU-G-07 Validar cobertura y autorización con la
  /// obra social (CU-A-11, CU-B-11).
  /// </summary>
  public static partial class Acreditacion
  {
    const string Modulo = "Acreditación (CU-G-06)";

    /// <summary>Turnos reservados para hoy del paciente.</summary>
    public static List<TurnoDeHoy> TurnosDeHoy(int idPaciente)
    {
      Sesion.Exigir("CU-G-06");
      return Consulta.Lista("SELECT t.id_turno, substr(t.fecha_hora, 12) " +
        "AS hora, x.id_estudio, t.procedencia, COALESCE(e.nombre, " +
        "es.nombre || ' · ' || p.apellido_nombre) AS det, COALESCE(" +
        "c.numero_nombre, e.sala_equipo) AS lugar FROM turno t LEFT JOIN " +
        "agenda a ON a.id_agenda = t.id_agenda LEFT JOIN profesional p ON " +
        "p.id_profesional = a.id_profesional LEFT JOIN especialidad es ON " +
        "es.id_especialidad = p.id_especialidad LEFT JOIN consultorio_sala" +
        " c ON c.id_consultorio = a.id_consultorio LEFT JOIN " +
        "detalle_turno_estudio x ON x.id_turno = t.id_turno LEFT JOIN " +
        "estudio e ON e.id_estudio = x.id_estudio WHERE t.id_paciente = @p0" +
        " AND t.estado = 'reservado' AND t.fecha_hora LIKE @p1 ORDER BY " +
        "t.fecha_hora", LeerTurno, idPaciente, Formato.Iso(Reloj.Hoy) + "%");
    }

    static TurnoDeHoy LeerTurno(Fila f)
    {
      return new TurnoDeHoy
      {
        IdTurno = f.Entero("id_turno"), Hora = f.Texto("hora"),
        IdEstudio = f.EnteroONulo("id_estudio"), Detalle = f.Texto("det"),
        Lugar = f.Texto("lugar"), Procedencia = f.Texto("procedencia")
      };
    }

    /// <summary>
    /// Arancel particular informado (parámetro, a confirmar).
    /// </summary>
    public static decimal Arancel(bool estudio)
    {
      return decimal.Parse(Parametros.Valor(estudio ? "arancel_estudio"
        : "arancel_consulta"));
    }

    /// <summary>Registra la orden médica presentada (sólo estudios).</summary>
    public static int RegistrarOrden(int idPaciente, DatosOrden o)
    {
      Sesion.Exigir("CU-G-07");
      ValidarOrden(o);
      int id = Consulta.Insertar("INSERT INTO orden_medica (id_paciente, " +
        "profesional_solicitante, matricula_solicitante, " +
        "estudios_indicados, fecha_emision, fecha_vencimiento) VALUES (@p0" +
        ", @p1, @p2, @p3, @p4, @p5)", idPaciente, o.Solicitante.Trim(),
        o.Matricula, o.Estudios, Formato.Iso(o.Emision),
        Formato.Iso(o.Vencimiento));
      Auditoria.Alta("B", Modulo, "ORDEN MÉDICA", id, o.Solicitante + " · " +
        o.Estudios);
      return id;
    }

    static void ValidarOrden(DatosOrden o)
    {
      ReglaException.Exigir(o != null && !string.IsNullOrWhiteSpace(
        o.Solicitante), "Para acreditar un estudio con obra social se " +
        "necesita la orden médica.", "orden");
      ReglaException.Exigir(o.Vencimiento.Date >= Reloj.Hoy, "La orden " +
        "médica está vencida: no habilita la acreditación.", "orden");
      ReglaException.Exigir(o.Emision.Date <= Reloj.Hoy, "La fecha de " +
        "emisión de la orden no puede ser futura.", "orden");
    }
  }
}
