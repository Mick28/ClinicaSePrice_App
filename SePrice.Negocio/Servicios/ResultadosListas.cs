using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  public static partial class Resultados
  {
    // Estudios realizados y todavía no entregados, con su situación.
    const string SqlPendientes = "SELECT COALESCE(r.nro_comprobante_retiro" +
      ", '') AS Comprobante, p.apellido_nombre AS Paciente, p.dni AS DNI, " +
      "e.nombre AS Estudio, r.fecha_realizacion AS Realizado, " +
      "r.fecha_estimada AS Estimada, CASE WHEN t.sector_solicitante IS " +
      "NOT NULL THEN 'Para el sector' WHEN r.fecha_estimada <= @p0 THEN " +
      "'Listo para retirar' ELSE 'En proceso' END AS Estado, " +
      "COALESCE(t.sector_solicitante || ' · ' || t.nro_episodio, '') AS " +
      "[Sector y episodio] FROM resultado_de_estudio r JOIN " +
      "detalle_turno_estudio x ON x.id_detalle = r.id_detalle JOIN turno t" +
      " ON t.id_turno = x.id_turno JOIN paciente p ON p.id_paciente = " +
      "t.id_paciente JOIN estudio e ON e.id_estudio = x.id_estudio WHERE " +
      "t.estado = 'realizado' ORDER BY Estado DESC, r.fecha_estimada";

    /// <summary>
    /// Resultados pendientes de entrega: listos para retirar, en proceso
    /// (con su fecha estimada) y los de internación, que van al sector.
    /// </summary>
    public static DataTable Pendientes()
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla(SqlPendientes, Formato.Iso(Reloj.Hoy));
    }

    /// <summary>Últimas entregas registradas, con quién retiró.</summary>
    public static DataTable EntregasRecientes()
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT r.fecha_entrega AS Entregado, " +
        "r.nro_comprobante_retiro AS Comprobante, p.apellido_nombre AS " +
        "Paciente, e.nombre AS Estudio, r.receptor AS [Retiró], CASE WHEN " +
        "r.fecha_entrega <= r.fecha_estimada THEN 'a término' ELSE " +
        "'con demora' END AS Plazo FROM resultado_de_estudio r JOIN " +
        "detalle_turno_estudio x ON x.id_detalle = r.id_detalle JOIN turno" +
        " t ON t.id_turno = x.id_turno JOIN paciente p ON p.id_paciente = " +
        "t.id_paciente JOIN estudio e ON e.id_estudio = x.id_estudio WHERE " +
        "r.fecha_entrega IS NOT NULL ORDER BY r.fecha_entrega DESC, " +
        "r.id_resultado DESC LIMIT 12");
    }
  }
}
