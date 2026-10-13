using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Un estudio del catálogo (entidad ESTUDIO).</summary>
  public class Estudio
  {
    public int Id;
    public string Nombre, SalaEquipo;
    public int? Duracion;
    public bool RequiereAyuno, RequiereTurno;

    /// <summary>
    /// Los de laboratorio no requieren turno (orden de llegada).
    /// </summary>
    public bool EsLaboratorio
    {
      get { return !RequiereTurno; }
    }

    public override string ToString()
    {
      return Nombre + (Duracion.HasValue ? " (" + Duracion + " min)" : "");
    }
  }

  /// <summary>Datos del formulario del catálogo (P-24).</summary>
  public class DatosEstudio
  {
    public string Nombre, SalaEquipo;
    public int? Duracion;
    public bool RequiereAyuno, Laboratorio;
    public Dictionary<int, int> Insumos = new Dictionary<int, int>();
  }

  /// <summary>
  /// CU-G-03 Gestionar catálogo de estudios (CU-B-03, P-24): nombre,
  /// duración, ayuno, si requiere turno e insumos estándar por estudio.
  /// </summary>
  public static class Catalogo
  {
    const string Cu = "CU-G-03", Modulo = "Catálogo de estudios (CU-G-03)";

    /// <summary>Tabla del catálogo para la pantalla.</summary>
    public static DataTable Tabla()
    {
      return Consulta.Tabla("SELECT e.id_estudio AS Id, e.nombre AS " +
        "Estudio, COALESCE(e.duracion || ' min', '—') AS Duración, CASE " +
        "e.requiere_turno WHEN 1 THEN 'Sí' ELSE 'No' END AS [Requiere " +
        "turno], CASE e.requiere_ayuno WHEN 1 THEN 'Sí' ELSE 'No' END AS " +
        "[Requiere ayuno], e.sala_equipo AS [Sala/equipo], (SELECT " +
        "group_concat(i.nombre || ' x' || x.cantidad_necesaria, ', ') FROM " +
        "estudio_x_insumo x JOIN insumo i ON i.id_insumo = x.id_insumo " +
        "WHERE x.id_estudio = e.id_estudio) AS Insumos FROM estudio e " +
        "ORDER BY e.requiere_turno DESC, e.nombre");
    }

    /// <summary>Estudios del catálogo (todos, con turno o sin turno).</summary>
    public static List<Estudio> Estudios(bool? conTurno = null)
    {
      return Consulta.Lista("SELECT * FROM estudio WHERE @p0 IS NULL OR " +
        "requiere_turno = @p0 ORDER BY nombre", Leer,
        conTurno.HasValue ? (object)(conTurno.Value ? 1 : 0) : null);
    }

    /// <summary>Estudio por id.</summary>
    public static Estudio PorId(int id)
    {
      return Consulta.Uno("SELECT * FROM estudio WHERE id_estudio = @p0",
        Leer, id);
    }

    /// <summary>Alta de estudio con sus insumos estándar.</summary>
    public static int Alta(DatosEstudio d)
    {
      Sesion.Exigir(Cu);
      Validar(d);
      int id = 0;
      Consulta.EnTransaccion(() =>
      {
        id = Insertar(d);
        foreach (KeyValuePair<int, int> i in d.Insumos) Insumo(id, i);
      });
      Auditoria.Alta("B", Modulo, "ESTUDIO", id, d.Nombre);
      return id;
    }

    static void Validar(DatosEstudio d)
    {
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(d.Nombre),
        "Completá el nombre del estudio.", "nombre");
      ReglaException.Exigir(Consulta.Entero("SELECT COUNT(*) FROM estudio " +
        "WHERE lower(nombre) = lower(@p0)", d.Nombre.Trim()) == 0,
        "Ya existe el estudio " + d.Nombre.Trim() + ".", "nombre");
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(d.SalaEquipo),
        "Elegí la sala o el equipo.", "sala");
      ReglaException.Exigir(d.Laboratorio || (d.Duracion ?? 0) > 0,
        "Completá la duración del estudio.", "duracion");
    }

    static int Insertar(DatosEstudio d)
    {
      return Consulta.Insertar("INSERT INTO estudio (nombre, duracion, " +
        "requiere_ayuno, requiere_turno, sala_equipo) VALUES (@p0, @p1, " +
        "@p2, @p3, @p4)", d.Nombre.Trim(), d.Laboratorio ? null : d.Duracion,
        d.RequiereAyuno, !d.Laboratorio, d.SalaEquipo);
    }

    static void Insumo(int idEstudio, KeyValuePair<int, int> i)
    {
      Consulta.Ejecutar("INSERT INTO estudio_x_insumo (id_estudio, " +
        "id_insumo, cantidad_necesaria) VALUES (@p0, @p1, @p2)", idEstudio,
        i.Key, i.Value);
    }

    /// <summary>
    /// Insumos estándar de un estudio: id, nombre y cantidad.
    /// </summary>
    public static List<string[]> InsumosDe(int idEstudio)
    {
      return Consulta.Lista("SELECT i.id_insumo AS id, i.nombre AS n, " +
        "x.cantidad_necesaria AS c, i.unidad_medida AS u FROM " +
        "estudio_x_insumo x JOIN insumo i ON i.id_insumo = x.id_insumo " +
        "WHERE x.id_estudio = @p0 ORDER BY i.nombre",
        f => new[] { f.Texto("id"), f.Texto("n"), f.Texto("c"),
          f.Texto("u") }, idEstudio);
    }

    static Estudio Leer(Fila f)
    {
      return new Estudio
      {
        Id = f.Entero("id_estudio"), Nombre = f.Texto("nombre"),
        Duracion = f.EnteroONulo("duracion"),
        RequiereAyuno = f.Logico("requiere_ayuno"),
        RequiereTurno = f.Logico("requiere_turno"),
        SalaEquipo = f.Texto("sala_equipo")
      };
    }
  }
}
