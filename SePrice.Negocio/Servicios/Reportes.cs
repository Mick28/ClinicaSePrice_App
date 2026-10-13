using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Indicadores de un período (P-18).</summary>
  public class Reporte
  {
    public int Dados, Atendidos, Ausentes, Sobreturnos, Cancelados;
    public int Realizados, Entregados, EntregadosATermino;
    public double Ausentismo, EsperaPromedio, OcupacionSalas;
    public List<KeyValuePair<string, int>> PorRubro, PorDia;
    public DataTable Insumos;
  }

  /// <summary>
  /// CU-G-11 Generar reportes de gestión (CU-A-14 y CU-B-16): indicadores
  /// del período, por circuito. Muestra totales, no listados de pacientes.
  /// </summary>
  public static class Reportes
  {
    const string Cu = "CU-G-11", Modulo = "Reportes (CU-G-11)";

    // Filtro común: turnos del período, del circuito elegido (A, B o null).
    const string Base = "FROM turno t LEFT JOIN detalle_turno_estudio x ON " +
      "x.id_turno = t.id_turno LEFT JOIN estudio e ON e.id_estudio = " +
      "x.id_estudio LEFT JOIN agenda a ON a.id_agenda = t.id_agenda LEFT " +
      "JOIN profesional p ON p.id_profesional = a.id_profesional LEFT JOIN" +
      " especialidad es ON es.id_especialidad = p.id_especialidad WHERE " +
      "substr(t.fecha_hora, 1, 10) BETWEEN @p0 AND @p1 AND (@p2 IS NULL OR" +
      " (@p2 = 'A' AND x.id_estudio IS NULL) OR (@p2 = 'B' AND x.id_estudio" +
      " IS NOT NULL)) ";

    /// <summary>Calcula el reporte del período.</summary>
    public static Reporte Generar(DateTime desde, DateTime hasta,
      string circuito)
    {
      Sesion.ExigirLectura(Cu);
      ReglaException.Exigir(hasta >= desde, "La fecha hasta no puede ser " +
        "anterior a la fecha desde.", "periodo");
      object[] p = { Formato.Iso(desde), Formato.Iso(hasta), circuito };
      Reporte r = Contar(p);
      ReglaException.Exigir(r.Dados > 0, "No hay movimientos entre el " +
        Formato.Mostrar(Formato.Iso(desde)) + " y el " +
        Formato.Mostrar(Formato.Iso(hasta)) + ". Probá con otro período.");
      Completar(r, p);
      return r;
    }

    static Reporte Contar(object[] p)
    {
      Func<string, int> n = cond => Consulta.Entero("SELECT COUNT(DISTINCT " +
        "t.id_turno) " + Base + cond, p);
      Reporte r = new Reporte
      {
        Dados = n("AND t.estado <> 'cancelado'"),
        Atendidos = n("AND t.estado IN ('atendido', 'realizado', " +
          "'entregado')"),
        Ausentes = n("AND t.estado = 'ausente'"),
        Sobreturnos = n("AND t.procedencia = 'sobreturno'"),
        Cancelados = n("AND t.estado = 'cancelado'"),
        Realizados = n("AND t.estado IN ('realizado', 'entregado')"),
        Entregados = n("AND t.estado = 'entregado'")
      };
      r.Ausentismo = r.Dados == 0 ? 0 : 100.0 * r.Ausentes / r.Dados;
      return r;
    }

    static void Completar(Reporte r, object[] p)
    {
      r.EsperaPromedio = Espera(p);
      r.PorRubro = Serie("SELECT COALESCE(e.nombre, es.nombre) AS k, " +
        "COUNT(*) AS v " + Base + "AND t.estado <> 'cancelado' GROUP BY 1 " +
        "ORDER BY 2 DESC", p);
      r.PorDia = Serie("SELECT substr(t.fecha_hora, 6, 5) AS k, COUNT(*) AS" +
        " v " + Base + "AND t.estado <> 'cancelado' GROUP BY 1 ORDER BY 1",
        p);
      r.OcupacionSalas = Ocupacion(p);
      r.EntregadosATermino = Consulta.Entero("SELECT COUNT(*) FROM " +
        "resultado_de_estudio WHERE fecha_entrega <= fecha_estimada AND " +
        "fecha_realizacion BETWEEN @p0 AND @p1", p[0], p[1]);
      r.Insumos = Consumo(p);
    }

    // Minutos promedio entre la hora del turno y el inicio de la atención.
    static int Espera(object[] p)
    {
      return Consulta.Entero("SELECT CAST(AVG((strftime('%s', " +
        "substr(t.fecha_hora, 1, 10) || ' ' || at.hora_inicio) - " +
        "strftime('%s', t.fecha_hora)) / 60.0) AS INTEGER) FROM atencion at" +
        " JOIN turno t ON t.id_turno = at.id_turno WHERE substr(" +
        "t.fecha_hora, 1, 10) BETWEEN @p0 AND @p1", p[0], p[1]);
    }

    static List<KeyValuePair<string, int>> Serie(string sql, object[] p)
    {
      return Consulta.Lista(sql, f => new KeyValuePair<string, int>(
        f.Texto("k") ?? "Laboratorio", f.Entero("v")), p);
    }

    // Ocupación de salas: minutos con estudio / minutos de agenda (a
    // confirmar con el cliente).
    static double Ocupacion(object[] p)
    {
      double agenda = Convert.ToDouble(Consulta.Escalar("SELECT SUM((substr" +
        "(hora_hasta,1,2)*60+substr(hora_hasta,4,2)) - (substr(hora_desde," +
        "1,2)*60+substr(hora_desde,4,2))) FROM agenda g JOIN " +
        "consultorio_sala c ON c.id_consultorio = g.id_consultorio WHERE " +
        "c.tipo = 'sala' AND g.dia BETWEEN @p0 AND @p1", p[0], p[1]) ?? 0);
      double usado = Convert.ToDouble(Consulta.Escalar("SELECT SUM(" +
        "e.duracion) " + Base + "AND x.id_estudio IS NOT NULL AND t.estado " +
        "IN ('realizado', 'entregado')", p) ?? 0);
      return agenda == 0 ? 0 : Math.Min(100, 100 * usado / agenda);
    }

    // Consumo de insumos del período; destaca los que llegaron al punto de
    // reposición (CU-B-16, alternativo).
    static DataTable Consumo(object[] p)
    {
      return Consulta.Tabla("SELECT i.nombre AS Insumo, SUM(c.cantidad) AS" +
        " Consumido, i.stock_actual AS [Stock actual], i.stock_minimo AS " +
        "[Stock mínimo], CASE WHEN i.stock_actual < i.stock_minimo THEN " +
        "'Alcanzó el punto de reposición' ELSE '' END AS Observación FROM " +
        "consumo_de_insumo c JOIN insumo i ON i.id_insumo = c.id_insumo " +
        "WHERE c.fecha BETWEEN @p0 AND @p1 GROUP BY i.id_insumo ORDER BY 2 " +
        "DESC", p[0], p[1]);
    }

    /// <summary>Atenciones y estudios realizados en el mes (Inicio).</summary>
    public static int AtencionesDelMes()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno WHERE fecha_hora " +
        "LIKE @p0 AND estado IN ('atendido', 'realizado', 'entregado')",
        Reloj.Hoy.ToString("yyyy-MM") + "%");
    }

    /// <summary>Exporta el reporte como planilla (CSV) y lo audita.</summary>
    public static void Exportar(Reporte r, string ruta, string periodo)
    {
      Sesion.ExigirLectura(Cu);
      StringBuilder s = new StringBuilder("Indicador;Valor\n");
      s.Append("Período;" + periodo + "\nTurnos dados;" + r.Dados +
        "\nAtendidos;" + r.Atendidos + "\nAusentismo (%);" +
        r.Ausentismo.ToString("0.0") + "\nSobreturnos;" + r.Sobreturnos +
        "\nCancelaciones;" + r.Cancelados + "\nEspera promedio (min);" +
        r.EsperaPromedio + "\nOcupación de salas (%, a confirmar);" +
        r.OcupacionSalas.ToString("0") + "\nEstudios entregados a término;" +
        r.EntregadosATermino + "\n");
      foreach (KeyValuePair<string, int> k in r.PorRubro)
        s.Append(k.Key + ";" + k.Value + "\n");
      File.WriteAllText(ruta, s.ToString(), Encoding.UTF8);
      Auditoria.Registrar("ambos", Modulo, "exportación", "TURNO", null,
        null, "reporte " + periodo);
    }
  }
}
