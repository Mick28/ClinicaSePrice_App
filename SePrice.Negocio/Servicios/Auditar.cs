using System;
using System.Data;
using System.IO;
using System.Text;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Filtros del registro de operaciones (P-27).</summary>
  public class FiltroAuditoria
  {
    public string Usuario, Circuito, Modulo, Accion;
    public DateTime Desde, Hasta;
  }

  /// <summary>
  /// CU-G-18 Auditar operaciones del sistema (CU-B-20, CU-A-22): consulta
  /// en sólo lectura. Cada consulta y cada exportación también quedan
  /// registradas. Nada se modifica.
  /// </summary>
  public static class Auditar
  {
    const string Cu = "CU-G-18", Modulo = "Auditoría (CU-G-18)";

    /// <summary>
    /// Acciones registrables (diccionario, REGISTRO DE AUDITORÍA).
    /// </summary>
    public static readonly string[] Acciones = { "alta", "modificación",
      "baja", "consulta", "exportación" };

    // Filtros opcionales: un parámetro nulo no filtra.
    const string SqlBuscar = "SELECT a.id_auditoria AS Id, " +
        "a.fecha_hora AS [Fecha y hora], u.nombre_usuario AS Usuario, " +
        "u.rol AS Perfil, a.circuito AS Circuito, a.modulo AS Pantalla, " +
        "a.accion AS Acción, a.entidad || ' ' || COALESCE(a.id_registro, " +
        "'') AS [Registro afectado] FROM registro_de_auditoria a JOIN " +
        "usuario u ON u.id_usuario = a.id_usuario WHERE substr(a.fecha_hora" +
        ", 1, 10) BETWEEN @p0 AND @p1 AND (@p2 IS NULL OR u.nombre_usuario " +
        "= @p2) AND (@p3 IS NULL OR a.circuito IN (@p3, 'ambos')) AND (@p4 " +
        "IS NULL OR a.modulo LIKE '%' || @p4 || '%') AND (@p5 IS NULL OR " +
        "a.accion = @p5) ORDER BY a.fecha_hora DESC, a.id_auditoria DESC";

    /// <summary>Operaciones que cumplen el filtro.</summary>
    public static DataTable Buscar(FiltroAuditoria f)
    {
      Sesion.Exigir(Cu);
      DataTable t = Consulta.Tabla(SqlBuscar, Formato.Iso(f.Desde),
        Formato.Iso(f.Hasta), f.Usuario, f.Circuito, f.Modulo, f.Accion);
      Auditoria.Registrar("ambos", Modulo, "consulta", "REGISTRO DE " +
        "AUDITORÍA", null, null, "filtro: " + Describir(f));
      return t;
    }

    static string Describir(FiltroAuditoria f)
    {
      return Formato.Iso(f.Desde) + " a " + Formato.Iso(f.Hasta) +
        (f.Usuario == null ? "" : " · " + f.Usuario) +
        (f.Circuito == null ? "" : " · circuito " + f.Circuito) +
        (f.Modulo == null ? "" : " · " + f.Modulo) +
        (f.Accion == null ? "" : " · " + f.Accion);
    }

    /// <summary>Valor anterior y nuevo de una operación.</summary>
    public static string[] Detalle(int idAuditoria)
    {
      Sesion.Exigir(Cu);
      return Consulta.Uno("SELECT valor_anterior AS a, valor_nuevo AS n " +
        "FROM registro_de_auditoria WHERE id_auditoria = @p0",
        r => new[] { r.Texto("a") ?? "—", r.Texto("n") ?? "—" }, idAuditoria);
    }

    /// <summary>
    /// Últimas operaciones (tabla del Inicio del Administrador).
    /// </summary>
    public static DataTable Ultimas(int cantidad)
    {
      Sesion.Exigir(Cu);
      return Consulta.Tabla("SELECT substr(a.fecha_hora, 9, 2) || '/' || " +
        "substr(a.fecha_hora, 6, 2) || ' ' || substr(a.fecha_hora, 12) AS " +
        "Fecha, " +
        "u.nombre_usuario AS Usuario, a.modulo AS Pantalla, a.accion AS " +
        "Acción, a.entidad || ' ' || COALESCE(a.id_registro, '') AS " +
        "Registro FROM registro_de_auditoria a JOIN usuario u ON " +
        "u.id_usuario = a.id_usuario WHERE a.accion <> 'consulta' ORDER BY" +
        " a.fecha_hora DESC, a.id_auditoria DESC LIMIT @p0", cantidad);
    }

    /// <summary>Cambios registrados hoy (indicador del Inicio).</summary>
    public static int CambiosDeHoy()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM registro_de_auditoria " +
        "WHERE fecha_hora LIKE @p0 AND accion <> 'consulta'",
        Formato.Iso(Reloj.Hoy) + "%");
    }

    /// <summary>Registra que se abrió un módulo en sólo lectura.</summary>
    public static void AbrirModulo(string cu, string pantalla)
    {
      Sesion.ExigirLectura(cu);
      Auditoria.Registrar("ambos", Modulo, "consulta", "MÓDULO", cu,
        null, "abrió en sólo lectura: " + pantalla);
    }

    /// <summary>Exporta el resultado del filtro (CSV) y lo registra.</summary>
    public static void Exportar(DataTable t, string ruta)
    {
      Sesion.Exigir(Cu);
      StringBuilder s = new StringBuilder();
      foreach (DataColumn c in t.Columns) s.Append(c.ColumnName + ";");
      s.AppendLine();
      foreach (DataRow r in t.Rows)
        s.AppendLine(string.Join(";", r.ItemArray));
      File.WriteAllText(ruta, s.ToString(), Encoding.UTF8);
      Auditoria.Registrar("ambos", Modulo, "exportación", "REGISTRO DE " +
        "AUDITORÍA", null, null, t.Rows.Count + " operaciones exportadas");
    }

    /// <summary>Nombres de usuario para el filtro.</summary>
    public static System.Collections.Generic.List<string> Usuarios()
    {
      return Consulta.Lista("SELECT nombre_usuario AS n FROM usuario ORDER " +
        "BY 1", r => r.Texto("n"));
    }
  }
}
