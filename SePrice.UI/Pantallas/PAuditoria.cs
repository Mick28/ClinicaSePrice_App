using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-27 Auditar operaciones del sistema (CU-G-18): sólo el
  /// Administrador/a. Filtra, ve el detalle y exporta; nada se modifica.
  /// Abre los módulos operativos en sólo lectura.
  /// </summary>
  partial class PAuditoria : Pantalla
  {
    readonly ComboBox cboUsuario = Ui.Combo("Usuario", null, false);
    readonly DateTimePicker dtpDesde = Ui.Fecha("Desde",
      Reloj.Hoy.AddDays(-14));
    readonly DateTimePicker dtpHasta = Ui.Fecha("Hasta", Reloj.Hoy);
    readonly ComboBox cboCircuito = Ui.Combo("Circuito", new[] { "Ambos",
      "Consultorios externos (A)", "Estudios clínicos (B)" }, false);
    readonly TextBox txtModulo = Ui.Caja("Modulo");
    readonly ComboBox cboAccion = Ui.Combo("Accion", null, false);
    readonly Parrafo lblAnterior = Ui.Texto("—"), lblNuevo = Ui.Texto("—");
    DataGridView tblOperaciones;
    DataTable resultado;

    protected override void ConstruirNormal()
    {
      Contenido.Controls.Add(Ui.Aviso(TipoAviso.Info, "Modo auditoría · " +
        "sólo lectura. Tus consultas quedan registradas."));
      Pestanas p = new Pestanas { AlElegir = Elegir };
      p.Agregar("Registro de operaciones");
      p.Agregar("Módulos en sólo lectura");
      Contenido.Controls.Add(p);
      p.Iniciar();
    }

    void Elegir(string nombre, Columna area)
    {
      if (nombre.StartsWith("Registro")) Registro(area);
      else Modulos(area);
    }

    void Registro(Columna area)
    {
      cboUsuario.Items.Clear();
      cboUsuario.Items.Add("Todos");
      foreach (string u in Auditar.Usuarios()) cboUsuario.Items.Add(u);
      cboAccion.Items.Clear();
      cboAccion.Items.Add("Todas");
      cboAccion.Items.AddRange(Auditar.Acciones);
      cboUsuario.SelectedIndex = cboAccion.SelectedIndex = 0;
      cboCircuito.SelectedIndex = 0;
      area.Controls.Add(Filtros());
      tblOperaciones = Ui.Tabla("Operaciones", null, 12);
      tblOperaciones.SelectionChanged += (s, e) => Detalle();
      area.Controls.Add(Ui.EnFila(Lista(), PanelDetalle()));
      Aplicar();
    }

    Control Filtros()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(new Campo("Usuario", cboUsuario, false),
        new Campo("Desde *", dtpDesde, false), new Campo("Hasta *",
        dtpHasta, false), new Campo("Circuito", cboCircuito, false)));
      t.Controls.Add(Ui.EnFila(new Campo("Módulo (texto)", txtModulo, false),
        new Campo("Acción", cboAccion, false), Ui.Botones(Ui.Boton(
        "Aplicar filtros", TipoBoton.Primario, (s, e) => Aplicar()))));
      return t;
    }

    Control Lista()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(tblOperaciones);
      t.Controls.Add(Ui.BotonesIzq(Ui.Boton("Exportar resultado",
        TipoBoton.Secundario, (s, e) => Exportar())));
      return t;
    }

    Control PanelDetalle()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(340) };
      t.Controls.Add(Ui.Subtitulo("Detalle de la operación"));
      t.Controls.Add(Ui.Nota("VALOR ANTERIOR"));
      t.Controls.Add(lblAnterior);
      t.Controls.Add(Ui.Nota("VALOR NUEVO"));
      t.Controls.Add(lblNuevo);
      return t;
    }
  }
}
