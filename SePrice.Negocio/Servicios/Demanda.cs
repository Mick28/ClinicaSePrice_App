using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>
  /// Solicitud de estudio urgente de guardia o internación (P-12).
  /// </summary>
  public class SolicitudUrgente
  {
    public int IdPaciente, IdEstudio;
    public string Sector, Episodio, Prioridad, Solicitante, Observaciones;
  }

  /// <summary>
  /// CU-G-05 Registrar demanda sin turno previo: laboratorio por orden de
  /// llegada (CU-B-08) y estudios urgentes de guardia o internación
  /// (CU-B-09). El sobreturno (CU-A-07) está en <see cref="Turnos"/>.
  /// </summary>
  public static class Demanda
  {
    const string Cu = "CU-G-05";

    // ---------- Laboratorio sin turno (CU-B-08) ----------

    /// <summary>
    /// null si el horario está dentro de la franja; si no, el aviso con el
    /// próximo horario habilitado. Rige la franja más restrictiva.
    /// </summary>
    public static string FueraDeFranja(List<Estudio> estudios, DateTime ahora)
    {
      bool ayuno = estudios.Any(e => e.RequiereAyuno);
      int[] f = Parametros.Franja(Parametros.Valor(ayuno ? "lab_con_ayuno"
        : "lab_sin_ayuno"));
      int m = ahora.Hour * 60 + ahora.Minute;
      if (DiaHabil(ahora) && m >= f[0] && m < f[1]) return null;
      DateTime proximo = Proximo(ahora, f[0], m >= f[0]);
      return "Los estudios " + (ayuno ? "con" : "sin") + " ayuno se " +
        "reciben de " + Formato.HoraDe(f[0]) + " a " + Formato.HoraDe(f[1]) +
        ". Próximo horario: " + Formato.Corta(proximo) + " desde las " +
        Formato.HoraDe(f[0]) + ".";
    }

    static bool DiaHabil(DateTime d)
    {
      string dias = Parametros.Valor("lab_dias");
      return dias.Contains(Formato.Corta(d).Substring(0, 3));
    }

    static DateTime Proximo(DateTime ahora, int desde, bool yaPaso)
    {
      DateTime d = yaPaso ? ahora.Date.AddDays(1) : ahora.Date;
      while (!DiaHabil(d)) d = d.AddDays(1);
      return d.AddMinutes(desde);
    }

    /// <summary>
    /// Registra la llegada y devuelve el número de orden (L-024).
    /// </summary>
    public static string RegistrarLlegada(int idPaciente,
      List<Estudio> estudios)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(estudios.Count > 0, "Elegí al menos un " +
        "estudio.", "estudios");
      string fuera = FueraDeFranja(estudios, Reloj.Ahora);
      ReglaException.Exigir(fuera == null, fuera, "franja");
      int nro = Consulta.Entero("SELECT COALESCE(MAX(nro_orden_llegada), " +
        "0) + 1 FROM turno WHERE fecha_hora LIKE @p0",
        Formato.Iso(Reloj.Hoy) + "%");
      int id = 0;
      Consulta.EnTransaccion(() => id = InsertarLab(idPaciente, estudios,
        nro));
      Auditoria.Alta("B", "Laboratorio sin turno (CU-G-05)", "TURNO", id,
        "orden de llegada L-" + nro.ToString("000"));
      return "L-" + nro.ToString("000");
    }

    static int InsertarLab(int idPaciente, List<Estudio> estudios, int nro)
    {
      int id = Consulta.Insertar("INSERT INTO turno (id_paciente, " +
        "id_empleado, fecha_hora, estado, procedencia, nro_orden_llegada) " +
        "VALUES (@p0, @p1, @p2, 'reservado', 'orden de llegada', @p3)",
        idPaciente, Sesion.Actual.IdEmpleado, Formato.IsoHora(Reloj.Ahora),
        nro);
      foreach (Estudio e in estudios)
        Consulta.Ejecutar("INSERT INTO detalle_turno_estudio (id_turno, " +
          "id_estudio) VALUES (@p0, @p1)", id, e.Id);
      return id;
    }

    /// <summary>Cola de laboratorio del día, por número de orden.</summary>
    public static DataTable ColaLaboratorio()
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT 'L-' || substr('00' || " +
        "t.nro_orden_llegada, -3) AS Orden, p.apellido_nombre AS Paciente," +
        " substr(t.fecha_hora, 12) AS Llegada, t.estado AS Estado FROM " +
        "turno t JOIN paciente p ON p.id_paciente = t.id_paciente WHERE " +
        "t.procedencia = 'orden de llegada' AND t.fecha_hora LIKE @p0 " +
        "ORDER BY t.nro_orden_llegada", Formato.Iso(Reloj.Hoy) + "%");
    }

    /// <summary>Personas que esperan antes en la cola de laboratorio.</summary>
    public static int AntesEnCola()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno WHERE procedencia " +
        "= 'orden de llegada' AND fecha_hora LIKE @p0 AND estado IN " +
        "('reservado', 'acreditado')", Formato.Iso(Reloj.Hoy) + "%") - 1;
    }

    // ---------- Estudio urgente (CU-B-09) ----------

    /// <summary>
    /// Ingresa el estudio a la cola con prioridad, vinculado al episodio.
    /// Queda acreditado: el paciente ya está en la clínica.
    /// </summary>
    public static int SolicitarUrgente(SolicitudUrgente s)
    {
      Sesion.Exigir(Cu);
      ValidarUrgente(s);
      int id = 0;
      Consulta.EnTransaccion(() => id = InsertarUrgente(s));
      Auditoria.Alta("B", "Estudio urgente (CU-G-05)", "TURNO", id,
        s.Sector + " · " + s.Prioridad + " · " + s.Episodio);
      return id;
    }

    static void ValidarUrgente(SolicitudUrgente s)
    {
      ReglaException.Exigir(s.IdPaciente > 0, "Buscá al paciente por DNI.",
        "dni");
      ReglaException.Exigir(s.Sector == "guardia" || s.Sector ==
        "internación", "Elegí el sector.", "sector");
      ReglaException.Exigir(s.Prioridad == "urgente" || s.Prioridad ==
        "alta", "Elegí la prioridad.", "prioridad");
      ReglaException.Exigir(s.IdEstudio > 0, "Elegí el estudio.", "estudio");
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(s.Solicitante),
        "Completá el médico solicitante.", "solicitante");
    }

    static int InsertarUrgente(SolicitudUrgente s)
    {
      int id = Consulta.Insertar("INSERT INTO turno (id_paciente, " +
        "id_empleado, fecha_hora, estado, procedencia, modalidad_pago, " +
        "sector_solicitante, prioridad, nro_episodio) VALUES (@p0, @p1, " +
        "@p2, 'acreditado', 'guardia', @p3, @p4, @p5, @p6)", s.IdPaciente,
        Sesion.Actual.IdEmpleado, Formato.IsoHora(Reloj.Ahora), Pago(s),
        s.Sector, s.Prioridad, s.Episodio);
      Consulta.Ejecutar("INSERT INTO detalle_turno_estudio (id_turno, " +
        "id_estudio) VALUES (@p0, @p1)", id, s.IdEstudio);
      Orden(s);
      return id;
    }

    static string Pago(SolicitudUrgente s)
    {
      return Consulta.Entero("SELECT COUNT(*) FROM paciente WHERE " +
        "id_paciente = @p0 AND id_obra_social IS NOT NULL", s.IdPaciente)
        > 0 ? "obra social" : "particular";
    }

    // El médico solicitante y las observaciones quedan en la orden médica.
    static void Orden(SolicitudUrgente s)
    {
      string estudio = Catalogo.PorId(s.IdEstudio).Nombre;
      Consulta.Ejecutar("INSERT INTO orden_medica (id_paciente, " +
        "profesional_solicitante, estudios_indicados, fecha_emision, " +
        "fecha_vencimiento) VALUES (@p0, @p1, @p2, @p3, @p3)", s.IdPaciente,
        s.Solicitante.Trim(), estudio + (string.IsNullOrWhiteSpace(
        s.Observaciones) ? "" : " · " + s.Observaciones.Trim()),
        Formato.Iso(Reloj.Hoy));
    }

    /// <summary>Solicitudes de hoy del usuario de guardia.</summary>
    public static DataTable MisSolicitudes()
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT substr(t.fecha_hora, 12) AS Hora, " +
        "p.apellido_nombre AS Paciente, e.nombre AS Estudio, t.prioridad " +
        "AS Prioridad, t.sector_solicitante AS Sector, t.nro_episodio AS " +
        "Episodio, t.estado AS Estado FROM turno t JOIN paciente p ON " +
        "p.id_paciente = t.id_paciente JOIN detalle_turno_estudio x ON " +
        "x.id_turno = t.id_turno JOIN estudio e ON e.id_estudio = " +
        "x.id_estudio WHERE t.procedencia = 'guardia' AND t.fecha_hora " +
        "LIKE @p0 AND (@p1 IS NULL OR t.id_empleado = @p1) ORDER BY 1 DESC",
        Formato.Iso(Reloj.Hoy) + "%", Sesion.Actual.Perfil == Perfil.Guardia
        ? Sesion.Actual.IdEmpleado : null);
    }
  }
}
