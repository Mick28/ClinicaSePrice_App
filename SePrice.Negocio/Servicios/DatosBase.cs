using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Pestañas de P-04: cada perfil ve sólo las suyas (5.3).</summary>
  public enum PestanaBase { Especialidades, Profesionales, Consultorios, Salas }

  /// <summary>
  /// CU-G-17 Registrar especialidades, profesionales, consultorios y salas
  /// (CU-A-21, CU-B-19). Altas con control de duplicados; las bajas son
  /// lógicas («inactivo») y avisan lo que queda afectado.
  /// </summary>
  public static class DatosBase
  {
    const string Cu = "CU-G-17";
    const string Modulo = "Datos de base (CU-G-17)";

    /// <summary>Pestañas que ve el perfil (especificación, P-04).</summary>
    public static List<PestanaBase> Pestanas(Perfil p)
    {
      List<PestanaBase> l = new List<PestanaBase>();
      if (p != Perfil.Jefatura) l.Add(PestanaBase.Especialidades);
      if (p == Perfil.Coordinacion) return l;
      l.Add(PestanaBase.Profesionales);
      l.Add(PestanaBase.Consultorios);
      l.Add(PestanaBase.Salas);
      return l;
    }

    static void Exigir(PestanaBase pestana)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(Pestanas(Sesion.Actual.Perfil).Contains(pestana),
        "Esta función no corresponde a tu perfil.");
    }

    // ---------- Especialidades ----------

    /// <summary>Especialidades con su duración mínima.</summary>
    public static DataTable Especialidades()
    {
      return Consulta.Tabla("SELECT id_especialidad AS Id, nombre AS " +
        "Nombre, duracion_minima AS [Duración (min)], estado AS Estado " +
        "FROM especialidad ORDER BY nombre");
    }

    /// <summary>
    /// Alta de especialidad (nombre único, duración ≥ mínimo).
    /// </summary>
    public static int AltaEspecialidad(string nombre, int duracion)
    {
      Exigir(PestanaBase.Especialidades);
      nombre = Requerido(nombre, "Completá el nombre.", "nombre");
      Duplicado("especialidad", "nombre", nombre,
        "Ya existe la especialidad " + nombre + ".");
      Parametros.ValidarDuracion(nombre, duracion);
      int id = Consulta.Insertar("INSERT INTO especialidad (nombre, " +
        "duracion_minima, estado) VALUES (@p0, @p1, 'activo')",
        nombre, duracion);
      Auditoria.Alta("A", Modulo, "ESPECIALIDAD", id, nombre + " · " +
        duracion + " min");
      return id;
    }

    /// <summary>Especialidades activas para un combo.</summary>
    public static List<Opcion> ListaEspecialidades()
    {
      return Consulta.Lista("SELECT id_especialidad AS id, nombre || ' · '" +
        " || duracion_minima || ' min' AS texto FROM especialidad WHERE " +
        "estado = 'activo' ORDER BY nombre", Opcion.Leer);
    }

    // ---------- Profesionales ----------

    /// <summary>Profesionales y técnicos.</summary>
    public static DataTable Profesionales()
    {
      return Consulta.Tabla("SELECT p.id_profesional AS Id, " +
        "p.apellido_nombre AS [Nombre y apellido], p.matricula AS " +
        "Matrícula, p.tipo AS Tipo, COALESCE(e.nombre, 'Estudios') AS " +
        "[Especialidad o estudios], p.estado AS Estado FROM profesional p " +
        "LEFT JOIN especialidad e ON e.id_especialidad = " +
        "p.id_especialidad ORDER BY p.apellido_nombre");
    }

    /// <summary>Alta de profesional (CU-A-21) o técnico (CU-B-19).</summary>
    public static int AltaProfesional(string nombre, string matricula,
      string tipo, int? idEspecialidad)
    {
      Exigir(PestanaBase.Profesionales);
      nombre = Requerido(nombre, "Completá el nombre y apellido.", "nombre");
      matricula = Requerido(matricula, "Completá la matrícula.", "matricula");
      Duplicado("profesional", "matricula", matricula, "Ya existe un " +
        "profesional con la matrícula " + matricula + ".", "matricula");
      ReglaException.Exigir(tipo != "médico" || idEspecialidad.HasValue,
        "Elegí la especialidad. Si no existe, la da de alta la " +
        "coordinación médica o la administración del sistema.",
        "especialidad");
      int id = Consulta.Insertar("INSERT INTO profesional (apellido_nombre" +
        ", matricula, id_especialidad, tipo, estado) VALUES (@p0, @p1, " +
        "@p2, @p3, 'activo')", nombre, matricula,
        tipo == "médico" ? idEspecialidad : null, tipo);
      Auditoria.Alta(tipo == "técnico" ? "B" : "A", Modulo, "PROFESIONAL",
        id, nombre + " · " + matricula);
      return id;
    }

    /// <summary>Profesionales activos de un tipo, para combos.</summary>
    public static List<Opcion> ListaProfesionales(string tipo)
    {
      return Consulta.Lista("SELECT p.id_profesional AS id, " +
        "p.apellido_nombre || COALESCE(' · ' || e.nombre, '') AS texto " +
        "FROM profesional p LEFT JOIN especialidad e ON e.id_especialidad" +
        " = p.id_especialidad WHERE p.tipo = @p0 AND p.estado = 'activo' " +
        "ORDER BY p.apellido_nombre", Opcion.Leer, tipo);
    }

    // ---------- Consultorios y salas ----------

    /// <summary>Consultorios o salas («consultorio» / «sala»).</summary>
    public static DataTable Lugares(string tipo)
    {
      return Consulta.Tabla("SELECT id_consultorio AS Id, numero_nombre " +
        "AS [Número o nombre], ubicacion AS Ubicación, " +
        "COALESCE(equipamiento, '—') AS Equipamiento, estado AS Estado " +
        "FROM consultorio_sala WHERE tipo = @p0 ORDER BY numero_nombre",
        tipo);
    }

    /// <summary>Alta de consultorio (CU-A-21) o sala (CU-B-19).</summary>
    public static int AltaLugar(string tipo, string nombre, string ubicacion,
      string equipamiento)
    {
      Exigir(tipo == "sala" ? PestanaBase.Salas : PestanaBase.Consultorios);
      nombre = Requerido(nombre, "Completá el número o nombre.", "nombre");
      ubicacion = Requerido(ubicacion, "Completá la ubicación.", "ubicacion");
      if (tipo == "sala") Requerido(equipamiento,
        "Completá el equipamiento de la sala.", "equipamiento");
      Duplicado("consultorio_sala", "numero_nombre", nombre,
        "Ya existe " + nombre + ".", "nombre");
      int id = Consulta.Insertar("INSERT INTO consultorio_sala (" +
        "numero_nombre, ubicacion, tipo, equipamiento, estado) VALUES " +
        "(@p0, @p1, @p2, @p3, 'activo')", nombre, ubicacion, tipo,
        tipo == "sala" ? equipamiento : null);
      Auditoria.Alta(tipo == "sala" ? "B" : "A", Modulo, "CONSULTORIO / " +
        "SALA", id, nombre + " · " + ubicacion);
      return id;
    }

    /// <summary>Consultorios o salas activos, para combos.</summary>
    public static List<Opcion> ListaLugares(string tipo)
    {
      return Consulta.Lista("SELECT id_consultorio AS id, numero_nombre " +
        "|| ' · ' || ubicacion AS texto FROM consultorio_sala WHERE tipo = " +
        "@p0 AND estado = 'activo' ORDER BY numero_nombre", Opcion.Leer,
        tipo);
    }

    // ---------- Bajas lógicas ----------

    /// <summary>
    /// Pasa el registro a «inactivo». Devuelve lo afectado (asignaciones o
    /// agendas vigentes) para que la pantalla lo advierta (CU-A-21).
    /// </summary>
    public static List<string> DarDeBaja(PestanaBase pestana, int id)
    {
      Exigir(pestana);
      string[] t = Tabla(pestana);
      List<string> afectado = Afectados(pestana, id);
      Consulta.Ejecutar("UPDATE " + t[0] + " SET estado = 'inactivo' " +
        "WHERE " + t[1] + " = @p0", id);
      Auditoria.Registrar("ambos", Modulo, "baja", t[2], id,
        "estado: activo", "estado: inactivo");
      return afectado;
    }

    static List<string> Afectados(PestanaBase pestana, int id)
    {
      if (pestana == PestanaBase.Especialidades) return new List<string>();
      string col = pestana == PestanaBase.Profesionales ? "id_profesional"
        : "id_consultorio";
      return Consulta.Lista("SELECT 'Agenda del ' || dia || ' de ' || " +
        "hora_desde || ' a ' || hora_hasta AS t FROM agenda WHERE " + col +
        " = @p0 AND dia >= @p1 ORDER BY dia", f => f.Texto("t"), id,
        Formato.Iso(Reloj.Hoy));
    }

    static string[] Tabla(PestanaBase p)
    {
      switch (p)
      {
        case PestanaBase.Especialidades:
          return new[] { "especialidad", "id_especialidad", "ESPECIALIDAD" };
        case PestanaBase.Profesionales:
          return new[] { "profesional", "id_profesional", "PROFESIONAL" };
        default:
          return new[] { "consultorio_sala", "id_consultorio",
            "CONSULTORIO / SALA" };
      }
    }

    // ---------- comunes ----------

    static string Requerido(string valor, string mensaje, string campo)
    {
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(valor), mensaje,
        campo);
      return valor.Trim();
    }

    static void Duplicado(string tabla, string columna, string valor,
      string mensaje, string campo = "nombre")
    {
      ReglaException.Exigir(Consulta.Entero("SELECT COUNT(*) FROM " + tabla +
        " WHERE lower(" + columna + ") = lower(@p0)", valor) == 0, mensaje,
        campo);
    }
  }
}
