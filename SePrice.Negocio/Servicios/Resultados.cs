using System;
using System.Collections.Generic;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Estudio realizado, pendiente de entrega (P-16).</summary>
  public class ResultadoPendiente
  {
    public int Id, IdTurno;
    public string Comprobante, Paciente, Estudio, Realizado, Estimada;
    public string Sector, Episodio;

    /// <summary>
    /// Pedido de guardia o internación: se entrega al sector.
    /// </summary>
    public bool EsInternacion { get { return Sector != null; } }

    public override string ToString()
    {
      return (EsInternacion ? Sector + " " + Episodio : Comprobante) + " · " +
        Paciente + " · " + Estudio;
    }
  }

  /// <summary>
  /// CU-G-09 Entregar resultados de estudios (CU-B-15): contra el
  /// comprobante de retiro; en internación se carga el informe y queda en
  /// el episodio, sin comprobante.
  /// </summary>
  public static partial class Resultados
  {
    const string Cu = "CU-G-09", Modulo = "Entrega de resultados (CU-G-09)";

    /// <summary>Busca por N.° de comprobante (R-3381) o por DNI.</summary>
    public static List<ResultadoPendiente> Buscar(string texto)
    {
      Sesion.ExigirLectura(Cu);
      string t = (texto ?? "").Trim().ToUpperInvariant().Replace(".", "");
      ReglaException.Exigir(t.Length > 0, "Ingresá el N.° de comprobante " +
        "de retiro o el DNI.", "busqueda");
      return Consulta.Lista("SELECT r.*, t.id_turno, p.apellido_nombre AS " +
        "pac, e.nombre AS est, t.sector_solicitante, t.nro_episodio FROM " +
        "resultado_de_estudio r JOIN detalle_turno_estudio x ON " +
        "x.id_detalle = r.id_detalle JOIN turno t ON t.id_turno = " +
        "x.id_turno JOIN paciente p ON p.id_paciente = t.id_paciente JOIN " +
        "estudio e ON e.id_estudio = x.id_estudio WHERE t.estado = " +
        "'realizado' AND (r.nro_comprobante_retiro = @p0 OR " +
        "CAST(p.dni AS TEXT) = @p0) ORDER BY r.fecha_realizacion", Leer, t);
    }

    static ResultadoPendiente Leer(Fila f)
    {
      return new ResultadoPendiente
      {
        Id = f.Entero("id_resultado"), IdTurno = f.Entero("id_turno"),
        Comprobante = f.Texto("nro_comprobante_retiro"),
        Paciente = f.Texto("pac"), Estudio = f.Texto("est"),
        Realizado = f.Texto("fecha_realizacion"),
        Estimada = f.Texto("fecha_estimada"),
        Sector = f.Texto("sector_solicitante"),
        Episodio = f.Texto("nro_episodio")
      };
    }

    /// <summary>
    /// Registra la entrega con fecha y receptor; el estudio pasa a
    /// «entregado». En internación, el receptor es el sector y se carga el
    /// informe.
    /// </summary>
    public static void Entregar(ResultadoPendiente r, string receptor,
      string informe)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(r.EsInternacion || string.CompareOrdinal(
        r.Estimada, Formato.Iso(Reloj.Hoy)) <= 0, "El resultado estará " +
        "listo el " + Formato.Corta(Formato.Leer(r.Estimada)) + ".");
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(receptor),
        "Completá quién retira (nombre y DNI).", "receptor");
      ReglaException.Exigir(!r.EsInternacion || !string.IsNullOrWhiteSpace(
        informe), "Cargá el informe para el episodio.", "informe");
      Consulta.EnTransaccion(() => Guardar(r, receptor, informe));
      Auditoria.Cambio("B", Modulo, "RESULTADO DE ESTUDIO", r.Id,
        "estado: realizado", "estado: entregado · " + receptor);
    }

    static void Guardar(ResultadoPendiente r, string receptor, string inf)
    {
      Consulta.Ejecutar("UPDATE resultado_de_estudio SET fecha_entrega = " +
        "@p0, receptor = @p1, informe = COALESCE(@p2, informe) WHERE " +
        "id_resultado = @p3", Formato.Iso(Reloj.Hoy), receptor.Trim(),
        string.IsNullOrWhiteSpace(inf) ? null : inf.Trim(), r.Id);
      Consulta.Ejecutar("UPDATE turno SET estado = 'entregado' WHERE " +
        "id_turno = @p0", r.IdTurno);
    }
  }
}
