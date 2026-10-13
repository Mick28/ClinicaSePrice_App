using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>
  /// CU-G-10 Controlar insumos y reponer stock (CU-A-15, CU-B-14). Un solo
  /// insumo para los dos circuitos; la alerta la dispara siempre el stock
  /// mínimo global; la dotación del consultorio dice qué y cuánto reponer.
  /// </summary>
  public static partial class Insumos
  {
    const string Cu = "CU-G-10", Modulo = "Insumos (CU-G-10)";

    /// <summary>
    /// Insumos con stock, mínimo y estado (y dotación, si hay).
    /// </summary>
    public static DataTable Listar(bool soloBajoMinimo, int? idConsultorio)
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT i.id_insumo AS Id, i.nombre AS Insumo, " +
        "i.unidad_medida AS Unidad, i.stock_actual AS [Stock actual], " +
        "i.stock_minimo AS [Stock mínimo], CASE WHEN i.stock_actual < " +
        "i.stock_minimo THEN 'Bajo mínimo' ELSE 'OK' END AS Estado, " +
        "COALESCE(d.cantidad_minima, '—') AS [Dotación del consultorio], " +
        "(SELECT MAX(p.fecha) FROM pedido_de_reposicion p JOIN " +
        "detalle_de_pedido dp ON dp.id_pedido = p.id_pedido WHERE " +
        "dp.id_insumo = i.id_insumo AND p.estado = 'recibido') AS [Última " +
        "reposición] FROM insumo i LEFT JOIN dotacion_de_insumos d ON " +
        "d.id_insumo = i.id_insumo AND d.id_consultorio = @p0 WHERE @p1 = " +
        "0 OR i.stock_actual < i.stock_minimo ORDER BY i.nombre",
        idConsultorio, soloBajoMinimo);
    }

    /// <summary>Insumos para un combo.</summary>
    public static List<Opcion> Lista()
    {
      return Consulta.Lista("SELECT id_insumo AS id, nombre || ' (' || " +
        "unidad_medida || ')' AS texto FROM insumo ORDER BY nombre",
        Opcion.Leer);
    }

    /// <summary>
    /// Cantidad de insumos bajo el mínimo (indicador del Inicio).
    /// </summary>
    public static int BajoMinimo()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM insumo WHERE " +
        "stock_actual < stock_minimo");
    }

    /// <summary>Consumo de un insumo en un consultorio (CU-A-15).</summary>
    public static string RegistrarConsumo(int idConsultorio, int idInsumo,
      int cantidad)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(idInsumo > 0, "Elegí el insumo.", "insumo");
      ReglaException.Exigir(cantidad > 0, "La cantidad tiene que ser " +
        "mayor que cero.", "cantidad");
      string alerta = null;
      Consulta.EnTransaccion(() => alerta = Descontar(idInsumo, cantidad,
        idConsultorio, Sesion.Actual.IdEmpleado, false));
      return alerta;
    }

    /// <summary>
    /// Descuenta el stock y registra el consumo. Si el stock queda bajo el
    /// mínimo, devuelve la alerta y propone el pedido: interno (dotación
    /// del consultorio) o al proveedor (estudios).
    /// </summary>
    internal static string Descontar(int idInsumo, int cantidad,
      int idLugar, int? idEmpleado, bool deEstudio)
    {
      Fila i = Insumo(idInsumo);
      ReglaException.Exigir(i.Entero("stock_actual") >= cantidad, "No hay " +
        "stock suficiente de " + i.Texto("nombre") + ".", "cantidad");
      Consulta.Ejecutar("UPDATE insumo SET stock_actual = stock_actual - " +
        "@p0 WHERE id_insumo = @p1", cantidad, idInsumo);
      Consulta.Ejecutar("INSERT INTO consumo_de_insumo (id_insumo, " +
        "id_consultorio, id_empleado, fecha, cantidad) VALUES (@p0, @p1, " +
        "@p2, @p3, @p4)", idInsumo, idLugar, idEmpleado,
        Formato.Iso(Reloj.Hoy), cantidad);
      Auditoria.Alta(deEstudio ? "B" : "A", Modulo, "CONSUMO DE INSUMO",
        idInsumo, i.Texto("nombre") + " x" + cantidad);
      return Alerta(idInsumo, deEstudio, idLugar);
    }

    static Fila Insumo(int idInsumo)
    {
      return Consulta.Uno("SELECT * FROM insumo WHERE id_insumo = @p0",
        f => f, idInsumo);
    }

    static string Alerta(int idInsumo, bool deEstudio, int idLugar)
    {
      Fila i = Insumo(idInsumo);
      if (i.Entero("stock_actual") >= i.Entero("stock_minimo")) return null;
      if (deEstudio) Proponer(idInsumo, i);
      else ProponerInterno(idLugar, idInsumo);
      return "El stock de " + i.Texto("nombre") + " quedó por debajo del " +
        "mínimo (" + i.Entero("stock_actual") + " de " +
        i.Entero("stock_minimo") + "). Se " + (deEstudio ? "avisó a " +
        "Insumos." : "propuso un pedido interno.");
    }
  }
}
