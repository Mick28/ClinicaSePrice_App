using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-17 Controlar insumos y reponer stock (CU-G-10): variante consultorios
  /// para el personal administrativo (consumo y pedido interno; stock sólo
  /// lectura) y variante estudios para el responsable de insumos (pedidos
  /// al proveedor y recepción).
  /// </summary>
  partial class PInsumos : Pantalla
  {
    readonly CheckBox chkBajo = Ui.Casilla("Bajo", "Sólo bajo mínimo");
    readonly ComboBox cboConsultorio = Ui.Combo("Consultorio",
      DatosBase.ListaLugares("consultorio"), false);
    DataGridView tblInsumos;

    bool Consultorios
    {
      get { return Sesion.Actual.Perfil == Perfil.Administrativo; }
    }

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Insumos y stock", Insumos.Listar(false, null));
      TablaDeLectura("Pedidos al proveedor", Insumos.Pedidos("proveedor"));
      TablaDeLectura("Pedidos internos (consultorios)",
        Insumos.Pedidos("interno"));
    }

    protected override void ConstruirNormal()
    {
      Contenido.Controls.Add(Ui.Circuito(Consultorios ? "A" : "B"));
      tblInsumos = Ui.Tabla("Insumos", null, 10);
      Tarjeta t = new Tarjeta();
      Control filtro = Consultorios ? (Control)Ui.EnFila(new Campo(
        "Consultorio", cboConsultorio, false), chkBajo) : chkBajo;
      t.Controls.Add(filtro);
      t.Controls.Add(tblInsumos);
      Contenido.Controls.Add(Consultorios ? Ui.EnFila(t, Consumo())
        : Ui.EnFila(t, Pedido()));
      if (Consultorios) TablaDeLectura("Pedidos internos al depósito",
        Insumos.Pedidos("interno"));
      else Recepcion();
      Eventos();
    }

    // Los filtros recargan la tabla al cambiar.
    void Eventos()
    {
      chkBajo.CheckedChanged += (s, e) => Cargar();
      cboConsultorio.SelectedIndexChanged += (s, e) => Cargar();
      if (cboConsultorio.Items.Count > 0) cboConsultorio.SelectedIndex = 0;
      Cargar();
    }

    int? Consultorio
    {
      get { return Consultorios ? (cboConsultorio.SelectedItem as Opcion)?.Id
        : null; }
    }

    void Cargar()
    {
      System.Data.DataTable d = Insumos.Listar(chkBajo.Checked, Consultorio);
      if (!Consultorios) d.Columns.Remove("Dotación del consultorio");
      Ui.Cargar(tblInsumos, d, 10);
      Contenido.PerformLayout();
    }
  }
}
