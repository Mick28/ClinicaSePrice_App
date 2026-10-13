using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Una atención del período a liquidar (P-23).</summary>
  public class AtencionLiquidable
  {
    public int IdTurno;
    public string Fecha, Paciente, Prestacion, Modalidad;
    public decimal? Valor;
    public bool Observada;
  }

  /// <summary>Resultado del cálculo de una liquidación.</summary>
  public class Liquidacion
  {
    public List<AtencionLiquidable> Atenciones;
    public decimal Total;
    public List<string> SinValor = new List<string>();
  }

  /// <summary>
  /// CU-G-14 Liquidar honorarios por profesional (CU-A-18): atenciones
  /// del período sin liquidar, con el valor por prestación y modalidad;
  /// las «observadas» (validación pendiente) no suman; nunca se liquida dos
  /// veces la misma atención.
  /// </summary>
  public static class Honorarios
  {
    const string Cu = "CU-G-14", Modulo = "Liquidar honorarios (CU-G-14)";

    /// <summary>
    /// Valores de honorarios (pestaña «Valores de honorarios»).
    /// </summary>
    public static DataTable Valores()
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT v.id_valor AS Id, e.nombre AS " +
        "Prestación, v.modalidad_pago AS Modalidad, v.importe AS Valor, " +
        "v.vigencia_desde AS [Vigencia desde], CASE v.a_confirmar WHEN 1 " +
        "THEN 'A confirmar con el cliente' ELSE '' END AS Nota FROM " +
        "valor_de_honorario v JOIN especialidad e ON e.id_especialidad = " +
        "v.id_especialidad ORDER BY e.nombre, v.modalidad_pago");
    }

    /// <summary>
    /// Carga o reemplaza el valor de una prestación y modalidad.
    /// </summary>
    public static void FijarValor(int idEspecialidad, string modalidad,
      decimal importe)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(idEspecialidad > 0, "Elegí la prestación.",
        "prestacion");
      ReglaException.Exigir(importe > 0, "El valor tiene que ser mayor " +
        "que cero.", "importe");
      Consulta.Ejecutar("DELETE FROM valor_de_honorario WHERE " +
        "id_especialidad = @p0 AND modalidad_pago = @p1", idEspecialidad,
        modalidad);
      int id = Consulta.Insertar("INSERT INTO valor_de_honorario (" +
        "id_especialidad, modalidad_pago, importe, vigencia_desde, " +
        "a_confirmar) VALUES (@p0, @p1, @p2, @p3, 1)", idEspecialidad,
        modalidad, importe, Formato.Iso(Reloj.Hoy));
      Auditoria.Alta("A", Modulo, "VALOR DE HONORARIO", id, modalidad +
        " · " + Formato.Pesos(importe));
    }

    /// <summary>Calcula la liquidación del período (sin guardarla).</summary>
    public static Liquidacion Calcular(int idProfesional, DateTime desde,
      DateTime hasta)
    {
      Sesion.ExigirLectura(Cu);
      List<AtencionLiquidable> l = Atenciones(idProfesional, desde, hasta);
      ReglaException.Exigir(l.Count > 0, "No hay atenciones sin liquidar " +
        "para " + Nombre(idProfesional) + " en ese período.", "periodo");
      Liquidacion r = new Liquidacion { Atenciones = l };
      r.Total = l.Where(a => !a.Observada).Sum(a => a.Valor ?? 0);
      r.SinValor = l.Where(a => !a.Observada && a.Valor == null).Select(a =>
        a.Prestacion + " · " + a.Modalidad).Distinct().ToList();
      return r;
    }

    static List<AtencionLiquidable> Atenciones(int idProf, DateTime desde,
      DateTime hasta)
    {
      return Consulta.Lista("SELECT t.id_turno, substr(t.fecha_hora, 1, 10)" +
        " AS f, p.apellido_nombre AS pac, e.nombre AS pres, " +
        "t.modalidad_pago AS m, v.importe, EXISTS (SELECT 1 FROM " +
        "validacion_de_cobertura c WHERE c.id_turno = t.id_turno AND " +
        "c.resultado = 'pendiente') AS obs FROM turno t JOIN agenda a ON " +
        "a.id_agenda = t.id_agenda JOIN paciente p ON p.id_paciente = " +
        "t.id_paciente JOIN profesional pr ON pr.id_profesional = " +
        "a.id_profesional JOIN especialidad e ON e.id_especialidad = " +
        "pr.id_especialidad LEFT JOIN valor_de_honorario v ON " +
        "v.id_especialidad = e.id_especialidad AND v.modalidad_pago = " +
        "t.modalidad_pago WHERE a.id_profesional = @p0 AND t.estado = " +
        "'atendido' AND t.id_liquidacion IS NULL AND a.dia BETWEEN @p1 AND " +
        "@p2 ORDER BY t.fecha_hora", Leer, idProf, Formato.Iso(desde),
        Formato.Iso(hasta));
    }

    static AtencionLiquidable Leer(Fila f)
    {
      return new AtencionLiquidable
      {
        IdTurno = f.Entero("id_turno"), Fecha = f.Texto("f"),
        Paciente = f.Texto("pac"), Prestacion = f.Texto("pres"),
        Modalidad = f.Texto("m"), Observada = f.Logico("obs"),
        Valor = f.Vacio("importe") ? (decimal?)null : f.Importe("importe")
      };
    }

    /// <summary>
    /// Cierra la liquidación: marca las atenciones (salvo las observadas)
    /// para que no se liquiden dos veces.
    /// </summary>
    public static int Cerrar(int idProfesional, DateTime desde,
      DateTime hasta)
    {
      Sesion.Exigir(Cu);
      Liquidacion r = Calcular(idProfesional, desde, hasta);
      ReglaException.Exigir(r.SinValor.Count == 0, "La prestación " +
        r.SinValor.FirstOrDefault() + " no tiene valor de honorario. " +
        "Cargalo en Valores de honorarios.", "valores");
      int id = 0;
      Consulta.EnTransaccion(() => id = Guardar(idProfesional, desde, hasta,
        r));
      Auditoria.Alta("A", Modulo, "LIQUIDACIÓN DE HONORARIOS", id,
        Nombre(idProfesional) + " · " + Formato.Pesos(r.Total));
      return id;
    }

    static int Guardar(int idProf, DateTime desde, DateTime hasta,
      Liquidacion r)
    {
      int id = Consulta.Insertar("INSERT INTO liquidacion_de_honorarios (" +
        "id_profesional, periodo_desde, periodo_hasta, importe_total, " +
        "estado, fecha_cierre) VALUES (@p0, @p1, @p2, @p3, 'cerrada', @p4)",
        idProf, Formato.Iso(desde), Formato.Iso(hasta), r.Total,
        Formato.Iso(Reloj.Hoy));
      foreach (AtencionLiquidable a in r.Atenciones.Where(x => !x.Observada))
        Consulta.Ejecutar("UPDATE turno SET id_liquidacion = @p0 WHERE " +
          "id_turno = @p1", id, a.IdTurno);
      return id;
    }

    /// <summary>Liquidaciones emitidas.</summary>
    public static DataTable Liquidaciones()
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT l.id_liquidacion AS [N.°], " +
        "p.apellido_nombre AS Profesional, l.periodo_desde AS Desde, " +
        "l.periodo_hasta AS Hasta, l.importe_total AS Total, l.estado AS " +
        "Estado, l.fecha_cierre AS Cierre FROM liquidacion_de_honorarios l " +
        "JOIN profesional p ON p.id_profesional = l.id_profesional ORDER " +
        "BY l.id_liquidacion DESC");
    }

    /// <summary>
    /// Atenciones observadas (validación pendiente), para el Inicio.
    /// </summary>
    public static int Observadas()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno t WHERE " +
        "t.estado = 'atendido' AND t.id_liquidacion IS NULL AND EXISTS " +
        "(SELECT 1 FROM validacion_de_cobertura c WHERE c.id_turno = " +
        "t.id_turno AND c.resultado = 'pendiente')");
    }

    static string Nombre(int idProfesional)
    {
      return Consulta.Texto("SELECT apellido_nombre FROM profesional WHERE " +
        "id_profesional = @p0", idProfesional);
    }
  }
}
