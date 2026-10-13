using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  public static partial class Insumos
  {
    // Pedido al proveedor «propuesto» (CU-B-14): uno abierto a la vez.
    static void Proponer(int idInsumo, Fila i)
    {
      int id = Consulta.Entero("SELECT id_pedido FROM pedido_de_reposicion" +
        " WHERE destino = 'proveedor' AND estado = 'propuesto'");
      if (id == 0) id = Consulta.Insertar("INSERT INTO " +
        "pedido_de_reposicion (fecha, destino, estado) VALUES (@p0, " +
        "'proveedor', 'propuesto')", Formato.Iso(Reloj.Hoy));
      int cantidad = i.Entero("stock_minimo") * 2 - i.Entero("stock_actual");
      Consulta.Ejecutar("INSERT OR REPLACE INTO detalle_de_pedido " +
        "(id_pedido, id_insumo, cantidad) VALUES (@p0, @p1, @p2)", id,
        idInsumo, cantidad);
    }

    // Pedido interno al depósito con la dotación del consultorio (CU-A-15).
    static void ProponerInterno(int idConsultorio, int idInsumo)
    {
      int cantidad = Consulta.Entero("SELECT cantidad_minima FROM " +
        "dotacion_de_insumos WHERE id_consultorio = @p0 AND id_insumo = " +
        "@p1", idConsultorio, idInsumo);
      int id = Consulta.Insertar("INSERT INTO pedido_de_reposicion (fecha," +
        " destino, id_empleado, estado) VALUES (@p0, 'interno', @p1, " +
        "'propuesto')", Formato.Iso(Reloj.Hoy), Sesion.Actual.IdEmpleado);
      Consulta.Ejecutar("INSERT INTO detalle_de_pedido (id_pedido, " +
        "id_insumo, cantidad) VALUES (@p0, @p1, @p2)", id, idInsumo,
        cantidad > 0 ? cantidad : 1);
    }

    /// <summary>
    /// Pedido interno al depósito con lo que falta para completar la
    /// dotación del consultorio (CU-A-15, «Generar pedido interno»).
    /// </summary>
    public static int PedidoInterno(int idConsultorio)
    {
      Sesion.Exigir(Cu);
      List<int[]> faltan = Consulta.Lista("SELECT id_insumo AS i, " +
        "cantidad_minima AS c FROM dotacion_de_insumos WHERE " +
        "id_consultorio = @p0", f => new[] { f.Entero("i"), f.Entero("c") },
        idConsultorio);
      ReglaException.Exigir(faltan.Count > 0, "El consultorio no tiene " +
        "dotación definida: la define la coordinación médica.");
      int id = 0;
      Consulta.EnTransaccion(() => id = Interno(faltan));
      Auditoria.Alta("A", Modulo, "PEDIDO DE REPOSICIÓN", id,
        "interno · consultorio " + idConsultorio);
      return id;
    }

    static int Interno(List<int[]> items)
    {
      int id = Consulta.Insertar("INSERT INTO pedido_de_reposicion (fecha," +
        " destino, id_empleado, estado) VALUES (@p0, 'interno', @p1, " +
        "'enviado')", Formato.Iso(Reloj.Hoy), Sesion.Actual.IdEmpleado);
      foreach (int[] i in items)
        Consulta.Ejecutar("INSERT INTO detalle_de_pedido (id_pedido, " +
          "id_insumo, cantidad) VALUES (@p0, @p1, @p2)", id, i[0], i[1]);
      return id;
    }

    /// <summary>Pedidos (al proveedor o internos) con su estado.</summary>
    public static DataTable Pedidos(string destino)
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT p.id_pedido AS [N.°], p.fecha AS Fecha," +
        " COALESCE(pr.razon_social, 'Depósito') AS Destino, p.estado AS " +
        "Estado, (SELECT group_concat(i.nombre || ' x' || d.cantidad, ', ')" +
        " FROM detalle_de_pedido d JOIN insumo i ON i.id_insumo = " +
        "d.id_insumo WHERE d.id_pedido = p.id_pedido) AS Detalle FROM " +
        "pedido_de_reposicion p LEFT JOIN proveedor pr ON pr.id_proveedor =" +
        " p.id_proveedor WHERE p.destino = @p0 ORDER BY p.id_pedido DESC",
        destino);
    }

    /// <summary>Detalle editable del pedido propuesto al proveedor.</summary>
    public static List<int[]> Propuesto(out int idPedido)
    {
      Sesion.ExigirLectura(Cu);
      idPedido = Consulta.Entero("SELECT id_pedido FROM " +
        "pedido_de_reposicion WHERE destino = 'proveedor' AND estado = " +
        "'propuesto'");
      return Consulta.Lista("SELECT id_insumo AS i, cantidad AS c FROM " +
        "detalle_de_pedido WHERE id_pedido = @p0", f => new[] {
        f.Entero("i"), f.Entero("c") }, idPedido);
    }

    /// <summary>
    /// Arma el pedido propuesto con todos los insumos bajo el mínimo
    /// (si no había uno abierto) y devuelve su número.
    /// </summary>
    public static int ArmarPropuesto()
    {
      Sesion.Exigir(Cu);
      foreach (int id in Consulta.Lista("SELECT id_insumo AS i FROM insumo" +
        " WHERE stock_actual < stock_minimo", f => f.Entero("i")))
        Proponer(id, Insumo(id));
      int pedido;
      Propuesto(out pedido);
      return pedido;
    }

    /// <summary>El responsable confirma y envía el pedido (CU-B-14).</summary>
    public static void Enviar(int idPedido, int idProveedor,
      Dictionary<int, int> cantidades)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(idProveedor > 0, "Elegí el proveedor.",
        "proveedor");
      ReglaException.Exigir(cantidades.Count > 0, "El pedido no tiene " +
        "insumos.");
      Consulta.EnTransaccion(() => GuardarEnvio(idPedido, idProveedor,
        cantidades));
      Auditoria.Cambio("B", Modulo, "PEDIDO DE REPOSICIÓN", idPedido,
        "estado: propuesto", "estado: enviado");
    }

    static void GuardarEnvio(int idPedido, int idProveedor,
      Dictionary<int, int> cantidades)
    {
      foreach (KeyValuePair<int, int> c in cantidades)
        Consulta.Ejecutar("UPDATE detalle_de_pedido SET cantidad = @p0 " +
          "WHERE id_pedido = @p1 AND id_insumo = @p2", c.Value, idPedido,
          c.Key);
      Consulta.Ejecutar("DELETE FROM detalle_de_pedido WHERE id_pedido = " +
        "@p0 AND cantidad <= 0", idPedido);
      Consulta.Ejecutar("UPDATE pedido_de_reposicion SET estado = " +
        "'enviado', id_proveedor = @p0, id_empleado = @p1, fecha = @p2 " +
        "WHERE id_pedido = @p3", idProveedor, Sesion.Actual.IdEmpleado,
        Formato.Iso(Reloj.Hoy), idPedido);
    }

    /// <summary>Registra la recepción: suma las cantidades al stock.</summary>
    public static void RegistrarRecepcion(int idPedido)
    {
      Sesion.Exigir(Cu);
      int n = Consulta.Ejecutar("UPDATE pedido_de_reposicion SET estado = " +
        "'recibido' WHERE id_pedido = @p0 AND estado = 'enviado'", idPedido);
      ReglaException.Exigir(n == 1, "Sólo se reciben pedidos enviados.");
      Consulta.Ejecutar("UPDATE insumo SET stock_actual = stock_actual + " +
        "(SELECT d.cantidad FROM detalle_de_pedido d WHERE d.id_pedido = " +
        "@p0 AND d.id_insumo = insumo.id_insumo) WHERE id_insumo IN " +
        "(SELECT id_insumo FROM detalle_de_pedido WHERE id_pedido = @p0)",
        idPedido);
      Auditoria.Cambio("B", Modulo, "PEDIDO DE REPOSICIÓN", idPedido,
        "estado: enviado", "estado: recibido");
    }

    /// <summary>Pedidos enviados al proveedor sin recibir (Inicio).</summary>
    public static int SinRecibir()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM pedido_de_reposicion " +
        "WHERE destino = 'proveedor' AND estado = 'enviado'");
    }

    /// <summary>Proveedores para el combo.</summary>
    public static List<Opcion> Proveedores()
    {
      return Consulta.Lista("SELECT id_proveedor AS id, razon_social AS " +
        "texto FROM proveedor ORDER BY razon_social", Opcion.Leer);
    }
  }
}
