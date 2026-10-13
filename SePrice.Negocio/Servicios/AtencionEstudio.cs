using System;
using System.Collections.Generic;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Resultado de registrar un estudio (P-15).</summary>
  public class EstudioRegistrado
  {
    public string Comprobante;
    public DateTime FechaEstimada;
    public bool Internacion;
    public List<string> Alertas = new List<string>();
  }

  public static partial class Atencion
  {
    /// <summary>
    /// Registra la realización del estudio y los insumos usados (CU-B-13):
    /// descuenta el stock (incluye CU-G-10), pasa a «realizado» y emite el
    /// comprobante de retiro con la fecha estimada.
    /// </summary>
    public static EstudioRegistrado RegistrarEstudio(int idTurno,
      Dictionary<int, int> insumos, DateTime fechaEstimada)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(fechaEstimada.Date >= Reloj.Hoy, "La fecha " +
        "estimada del resultado no puede ser anterior a hoy.", "fecha");
      Fila t = Turno(idTurno);
      EstudioRegistrado r = new EstudioRegistrado
      {
        FechaEstimada = fechaEstimada.Date,
        Internacion = t.Texto("procedencia") == "guardia"
      };
      Consulta.EnTransaccion(() => Guardar(t, insumos, r));
      Auditoria.Cambio("B", Modulo, "TURNO", idTurno, "estado: en atención",
        "estado: realizado · " + (r.Comprobante ?? "sin comprobante"));
      return r;
    }

    static Fila Turno(int idTurno)
    {
      Fila t = Consulta.Uno("SELECT t.*, x.id_detalle, a.id_consultorio " +
        "AS sala, (SELECT c.id_consultorio FROM consultorio_sala c WHERE " +
        "c.numero_nombre = e.sala_equipo) AS sala_estudio FROM turno t " +
        "JOIN detalle_turno_estudio x ON x.id_turno = t.id_turno JOIN " +
        "estudio e ON e.id_estudio = x.id_estudio LEFT JOIN agenda a ON " +
        "a.id_agenda = t.id_agenda WHERE t.id_turno = @p0", f => f, idTurno);
      ReglaException.Exigir(t != null && t.Texto("estado") == "en atención",
        "Llamá al paciente antes de registrar el estudio.");
      return t;
    }

    static void Guardar(Fila t, Dictionary<int, int> insumos,
      EstudioRegistrado r)
    {
      int sala = t.EnteroONulo("sala") ?? t.Entero("sala_estudio");
      foreach (KeyValuePair<int, int> i in insumos)
      {
        string alerta = Insumos.Descontar(i.Key, i.Value, sala, null, true);
        if (alerta != null) r.Alertas.Add(alerta);
      }
      r.Comprobante = r.Internacion ? null : NuevoComprobante();
      Consulta.Ejecutar("INSERT INTO resultado_de_estudio (id_detalle, " +
        "id_tecnico, fecha_realizacion, nro_comprobante_retiro, " +
        "fecha_estimada) VALUES (@p0, @p1, @p2, @p3, @p4)",
        t.Entero("id_detalle"), Yo, Formato.Iso(Reloj.Hoy),
        r.Comprobante ?? "INT-" + t.Entero("id_turno"),
        Formato.Iso(r.FechaEstimada));
      Consulta.Ejecutar("UPDATE turno SET estado = 'realizado' WHERE " +
        "id_turno = @p0", t.Entero("id_turno"));
    }

    static string NuevoComprobante()
    {
      int n = Consulta.Entero("SELECT COALESCE(MAX(CAST(substr(" +
        "nro_comprobante_retiro, 3) AS INTEGER)), 3000) + 1 FROM " +
        "resultado_de_estudio WHERE nro_comprobante_retiro LIKE 'R-%'");
      return "R-" + n;
    }
  }
}
