using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Datos de una asignación de consultorio (P-20).</summary>
  public class DatosAsignacion
  {
    public int IdProfesional, IdConsultorio;
    public string DiaSemana, HoraDesde, HoraHasta, VigenciaDesde, VigenciaHasta;
  }

  /// <summary>
  /// CU-G-12 Asignar consultorio e insumos al profesional (CU-A-16): sin
  /// superposiciones en el mismo consultorio, con vigencia, y dotación de
  /// insumos por consultorio.
  /// </summary>
  public static class Asignaciones
  {
    const string Cu = "CU-G-12";
    const string Modulo = "Asignar consultorio e insumos (CU-G-12)";

    /// <summary>Días de la semana posibles (lunes a sábado).</summary>
    public static readonly string[] Dias = { "lunes", "martes",
      "miércoles", "jueves", "viernes", "sábado" };

    /// <summary>Asignaciones vigentes de un consultorio.</summary>
    public static DataTable DelConsultorio(int idConsultorio)
    {
      Sesion.Exigir(Cu);
      return Consulta.Tabla("SELECT a.id_asignacion AS Id, a.dia_semana AS " +
        "Día, a.hora_desde || ' a ' || a.hora_hasta AS Franja, " +
        "p.apellido_nombre AS Profesional, a.vigencia_desde AS Desde, " +
        "COALESCE(a.vigencia_hasta, 'sin vencimiento') AS Hasta FROM " +
        "asignacion_de_consultorio a JOIN profesional p ON " +
        "p.id_profesional = a.id_profesional WHERE a.id_consultorio = @p0 " +
        "AND (a.vigencia_hasta IS NULL OR a.vigencia_hasta >= @p1) " +
        "ORDER BY a.id_asignacion", idConsultorio, Formato.Iso(Reloj.Hoy));
    }

    /// <summary>Registra la asignación si el consultorio está libre.</summary>
    public static int Asignar(DatosAsignacion d)
    {
      Sesion.Exigir(Cu);
      Validar(d);
      string ocupa = Ocupante(d);
      ReglaException.Exigir(ocupa == null, ocupa, "franja");
      string superpuesto = Superpuesto(d);
      ReglaException.Exigir(superpuesto == null, superpuesto, "franja");
      int id = Consulta.Insertar("INSERT INTO asignacion_de_consultorio " +
        "(id_profesional, id_consultorio, dia_semana, hora_desde, " +
        "hora_hasta, vigencia_desde, vigencia_hasta) VALUES (@p0, @p1, " +
        "@p2, @p3, @p4, @p5, @p6)", d.IdProfesional, d.IdConsultorio,
        d.DiaSemana, d.HoraDesde, d.HoraHasta, d.VigenciaDesde,
        string.IsNullOrEmpty(d.VigenciaHasta) ? null : d.VigenciaHasta);
      Auditoria.Alta("A", Modulo, "ASIGNACIÓN DE CONSULTORIO", id,
        d.DiaSemana + " " + d.HoraDesde + "-" + d.HoraHasta);
      return id;
    }

    static void Validar(DatosAsignacion d)
    {
      ReglaException.Exigir(d.IdProfesional > 0, "Elegí el profesional.",
        "profesional");
      ReglaException.Exigir(d.IdConsultorio > 0, "Elegí el consultorio.",
        "consultorio");
      ReglaException.Exigir(Formato.Minutos(d.HoraHasta) >
        Formato.Minutos(d.HoraDesde), "La hora hasta tiene que ser " +
        "posterior a la hora desde.", "franja");
      ReglaException.Exigir(string.IsNullOrEmpty(d.VigenciaHasta) ||
        string.CompareOrdinal(d.VigenciaHasta, d.VigenciaDesde) >= 0,
        "La vigencia hasta no puede ser anterior a la vigencia desde.",
        "vigencia");
    }

    /// <summary>
    /// Mensaje con quién ocupa el consultorio en esa franja, o null si está
    /// libre (CU-A-16, «consultorio ocupado»).
    /// </summary>
    public static string Ocupante(DatosAsignacion d)
    {
      return Consulta.Uno("SELECT 'El ' || c.numero_nombre || ' está " +
        "asignado a ' || p.apellido_nombre || ' los ' || a.dia_semana || " +
        "' de ' || a.hora_desde || ' a ' || a.hora_hasta || '. Elegí otra " +
        "franja u otro consultorio.' AS m FROM asignacion_de_consultorio a " +
        "JOIN profesional p ON p.id_profesional = a.id_profesional JOIN " +
        "consultorio_sala c ON c.id_consultorio = a.id_consultorio WHERE " +
        "a.id_consultorio = @p0 AND a.dia_semana = @p1 AND a.hora_desde < " +
        "@p3 AND a.hora_hasta > @p2 AND (a.vigencia_hasta IS NULL OR " +
        "a.vigencia_hasta >= @p4) AND (@p5 IS NULL OR a.vigencia_desde <= " +
        "@p5)", f => f.Texto("m"), d.IdConsultorio, d.DiaSemana, d.HoraDesde,
        d.HoraHasta, d.VigenciaDesde,
        string.IsNullOrEmpty(d.VigenciaHasta) ? null : d.VigenciaHasta);
    }

    /// <summary>
    /// Mensaje si el profesional ya atiende en otro consultorio en esa
    /// franja, o null si está libre: no puede estar en dos lugares a la vez.
    /// </summary>
    public static string Superpuesto(DatosAsignacion d)
    {
      return Consulta.Uno("SELECT p.apellido_nombre || ' ya está " +
        "asignado/a al ' || c.numero_nombre || ' los ' || a.dia_semana || " +
        "' de ' || a.hora_desde || ' a ' || a.hora_hasta || '. Un " +
        "profesional no puede atender en dos consultorios a la vez.' AS m " +
        "FROM asignacion_de_consultorio a JOIN profesional p ON " +
        "p.id_profesional = a.id_profesional JOIN consultorio_sala c ON " +
        "c.id_consultorio = a.id_consultorio WHERE a.id_profesional = @p0 " +
        "AND a.dia_semana = @p1 AND a.hora_desde < @p3 AND a.hora_hasta > " +
        "@p2 AND (a.vigencia_hasta IS NULL OR a.vigencia_hasta >= @p4) AND " +
        "(@p5 IS NULL OR a.vigencia_desde <= @p5)", f => f.Texto("m"),
        d.IdProfesional, d.DiaSemana, d.HoraDesde, d.HoraHasta,
        d.VigenciaDesde, string.IsNullOrEmpty(d.VigenciaHasta) ? null :
        d.VigenciaHasta);
    }

    /// <summary>
    /// Turnos futuros del profesional en otro consultorio (reasignación,
    /// CU-A-16): hay que avisar a los pacientes el nuevo lugar.
    /// </summary>
    public static int TurnosAfectados(int idProfesional, int idConsultorio)
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno t JOIN agenda a " +
        "ON a.id_agenda = t.id_agenda WHERE a.id_profesional = @p0 AND " +
        "a.id_consultorio <> @p1 AND a.dia > @p2 AND t.estado = " +
        "'reservado'", idProfesional, idConsultorio, Formato.Iso(Reloj.Hoy));
    }

    /// <summary>Profesionales médicos sin consultorio vigente.</summary>
    public static int SinConsultorio()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM profesional p WHERE " +
        "p.tipo = 'médico' AND p.estado = 'activo' AND NOT EXISTS (SELECT " +
        "1 FROM asignacion_de_consultorio a WHERE a.id_profesional = " +
        "p.id_profesional AND (a.vigencia_hasta IS NULL OR " +
        "a.vigencia_hasta >= @p0))", Formato.Iso(Reloj.Hoy));
    }

    // ---------- Dotación de insumos del consultorio ----------

    /// <summary>Dotación de un consultorio.</summary>
    public static DataTable Dotacion(int idConsultorio)
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT d.id_insumo AS Id, i.nombre AS Insumo, " +
        "d.cantidad_minima AS [Cantidad mínima], i.unidad_medida AS Unidad" +
        " FROM dotacion_de_insumos d JOIN insumo i ON i.id_insumo = " +
        "d.id_insumo WHERE d.id_consultorio = @p0 ORDER BY i.nombre",
        idConsultorio);
    }

    /// <summary>Agrega o actualiza un insumo de la dotación.</summary>
    public static void FijarDotacion(int idConsultorio, int idInsumo,
      int cantidad)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(idInsumo > 0, "Elegí el insumo.", "insumo");
      ReglaException.Exigir(cantidad > 0, "La cantidad tiene que ser " +
        "mayor que cero.", "cantidad");
      Consulta.Ejecutar("INSERT OR REPLACE INTO dotacion_de_insumos " +
        "(id_consultorio, id_insumo, cantidad_minima) VALUES (@p0, @p1, " +
        "@p2)", idConsultorio, idInsumo, cantidad);
      Auditoria.Cambio("A", Modulo, "DOTACIÓN DE INSUMOS", idConsultorio +
        "-" + idInsumo, null, "cantidad_minima: " + cantidad);
    }

    /// <summary>Quita un insumo de la dotación.</summary>
    public static void QuitarDotacion(int idConsultorio, int idInsumo)
    {
      Sesion.Exigir(Cu);
      Consulta.Ejecutar("DELETE FROM dotacion_de_insumos WHERE " +
        "id_consultorio = @p0 AND id_insumo = @p1", idConsultorio, idInsumo);
      Auditoria.Registrar("A", Modulo, "baja", "DOTACIÓN DE INSUMOS",
        idConsultorio + "-" + idInsumo);
    }

    /// <summary>Franjas ocupadas del consultorio (grilla semanal).</summary>
    public static List<string[]> Ocupacion(int idConsultorio)
    {
      return Consulta.Lista("SELECT a.dia_semana AS d, a.hora_desde AS h1, " +
        "a.hora_hasta AS h2, p.apellido_nombre AS p FROM " +
        "asignacion_de_consultorio a JOIN profesional p ON p.id_profesional" +
        " = a.id_profesional WHERE a.id_consultorio = @p0 AND " +
        "(a.vigencia_hasta IS NULL OR a.vigencia_hasta >= @p1)",
        f => new[] { f.Texto("d"), f.Texto("h1"), f.Texto("h2"),
          f.Texto("p") }, idConsultorio, Formato.Iso(Reloj.Hoy));
    }
  }
}
