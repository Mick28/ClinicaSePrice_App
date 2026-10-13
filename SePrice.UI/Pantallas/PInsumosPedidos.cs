using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PInsumos
  {
    readonly ComboBox cboInsumo = Ui.Combo("Insumo", Insumos.Lista());
    readonly NumericUpDown numCantidad = Ui.Numero("Cantidad", 1, 999, 1);
    readonly ComboBox cboProveedor = Ui.Combo("Proveedor",
      Insumos.Proveedores());
    readonly Dictionary<int, NumericUpDown> pedido =
      new Dictionary<int, NumericUpDown>();
    int idPedido;

    // Variante consultorios (CU-A-15): consumo y pedido interno.
    Control Consumo()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(340) };
      t.Controls.Add(Ui.Subtitulo("Registrar consumo"));
      t.Controls.Add(NuevoCampo("insumo", "Insumo", cboInsumo, true));
      t.Controls.Add(NuevoCampo("cantidad", "Cantidad", numCantidad, true));
      t.Controls.Add(Ui.Botones(Ui.Boton("Registrar consumo",
        TipoBoton.Primario, (s, e) => Consumir())));
      t.Controls.Add(Ui.Botones(Ui.Boton("Generar pedido interno",
        TipoBoton.Secundario, (s, e) => Interno())));
      return t;
    }

    void Consumir()
    {
      string alerta = null;
      if (!Guardar(() => alerta = Insumos.RegistrarConsumo(Consultorio ?? 0,
        (cboInsumo.SelectedItem as Opcion)?.Id ?? 0, (int)numCantidad.Value),
        null, false)) return;
      if (alerta != null) Dialogo.Informar(alerta, "Alerta de stock");
      else Dialogo.Informar("Consumo registrado.");
      Cargar();
    }

    void Interno()
    {
      int id = 0;
      if (Guardar(() => id = Insumos.PedidoInterno(Consultorio ?? 0), null))
        Dialogo.Informar("Pedido interno N.° " + id + " enviado al " +
          "depósito con la dotación del consultorio.");
    }

    void SinPedido(Tarjeta t)
    {
      t.Controls.Add(Ui.Nota("No hay un pedido propuesto abierto."));
      t.Controls.Add(Ui.Botones(Ui.Boton("Armar pedido con los insumos " +
        "bajo mínimo", TipoBoton.Secundario, (s, e) => Armar())));
    }

    // Variante estudios (CU-B-14): pedido propuesto al proveedor.
    Control Pedido()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(380) };
      t.Controls.Add(Ui.Subtitulo("Pedido propuesto"));
      List<int[]> det = Insumos.Propuesto(out idPedido);
      if (idPedido == 0) { SinPedido(t); return t; }
      foreach (int[] d in det) t.Controls.Add(Linea(d));
      t.Controls.Add(NuevoCampo("proveedor", "Proveedor", cboProveedor,
        true));
      t.Controls.Add(Ui.Botones(Ui.Boton("Enviar pedido al proveedor",
        TipoBoton.Primario, (s, e) => Enviar())));
      return t;
    }

    Control Linea(int[] d)
    {
      Opcion i = Insumos.Lista().First(o => o.Id == d[0]);
      NumericUpDown n = Ui.Numero("Ped" + d[0], 0, 99999, d[1]);
      pedido[d[0]] = n;
      return Ui.EnFila(Ui.Texto(i.Texto), n.Tap(x => x.MaximumSize =
        Ui.Tope(110)));
    }

    void Armar()
    {
      if (Intentar(() => Insumos.ArmarPropuesto())) Ir("P-17");
    }

    void Enviar()
    {
      Dictionary<int, int> c = pedido.ToDictionary(p => p.Key, p =>
        (int)p.Value.Value);
      if (Guardar(() => Insumos.Enviar(idPedido, (cboProveedor.SelectedItem
        as Opcion)?.Id ?? 0, c), "Pedido N.° " + idPedido + " enviado al " +
        "proveedor.")) Ir("P-17");
    }

    void Recepcion()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Pedidos al proveedor"));
      DataGridView g = Ui.Tabla("Pedidos", Insumos.Pedidos("proveedor"), 6);
      t.Controls.Add(g);
      t.Controls.Add(Ui.BotonesIzq(Ui.Boton("Registrar recepción",
        TipoBoton.Secundario, (s, e) => Recibir(g))));
      Contenido.Controls.Add(t);
    }

    void Recibir(DataGridView g)
    {
      int? id = Ui.Elegido(g, "N.°");
      if (id == null) { Dialogo.Error("Elegí un pedido."); return; }
      if (Guardar(() => Insumos.RegistrarRecepcion(id.Value), "Recepción " +
        "registrada: el stock quedó actualizado.")) Ir("P-17");
    }
  }
}
