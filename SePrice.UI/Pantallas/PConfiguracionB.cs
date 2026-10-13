using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-24 Gestionar catálogo de estudios (CU-B-03, CU-G-03): duración,
  /// ayuno, turno, sala e insumos estándar. Laboratorio: sin turno.
  /// </summary>
  class PCatalogo : Pantalla
  {
    readonly TextBox txtNombre = Ui.Caja("Nombre");
    readonly NumericUpDown numDuracion = Ui.Numero("Duracion", 5, 240, 20);
    readonly ComboBox cboSala = Ui.Combo("Sala", DatosBase.ListaLugares(
      "sala").Select(o => o.Texto.Split('·')[0].Trim()));
    readonly CheckBox chkAyuno = Ui.Casilla("Ayuno", "Requiere ayuno");
    readonly CheckBox chkLab = Ui.Casilla("Lab", "De laboratorio (sin " +
      "turno)");
    readonly ComboBox cboInsumo = Ui.Combo("Insumo", Insumos.Lista());
    readonly NumericUpDown numCantidad = Ui.Numero("Cantidad", 1, 99, 1);
    DataGridView tblEstudios;

    protected override void ConstruirNormal()
    {
      tblEstudios = Ui.Tabla("Estudios", Catalogo.Tabla(), 12);
      Tarjeta t = new Tarjeta();
      t.Controls.Add(tblEstudios);
      t.Controls.Add(Ui.Nota("Los estudios de laboratorio no requieren " +
        "turno: se atienden por orden de llegada."));
      Contenido.Controls.Add(Ui.EnFila(t, Formulario()));
      chkLab.CheckedChanged += (s, e) => numDuracion.Enabled = !chkLab.Checked;
    }

    Control Formulario()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(380) };
      t.Controls.Add(Ui.Subtitulo("Nuevo estudio"));
      t.Controls.Add(NuevoCampo("nombre", "Nombre", txtNombre, true));
      t.Controls.Add(NuevoCampo("duracion", "Duración (min)", numDuracion));
      t.Controls.Add(NuevoCampo("sala", "Sala o equipo", cboSala, true));
      t.Controls.Add(chkAyuno);
      t.Controls.Add(chkLab);
      t.Controls.Add(Ui.EnFila(new Campo("Insumo estándar", cboInsumo,
        false), new Campo("Cantidad", numCantidad, false)));
      t.Controls.Add(Ui.Botones(Ui.Boton("Guardar", TipoBoton.Primario,
        (s, e) => Alta()), BotonBlanquear()));
      return t;
    }

    void Alta()
    {
      DatosEstudio d = new DatosEstudio { Nombre = txtNombre.Text,
        Duracion = (int)numDuracion.Value, SalaEquipo = cboSala.Text,
        RequiereAyuno = chkAyuno.Checked, Laboratorio = chkLab.Checked };
      Opcion i = cboInsumo.SelectedItem as Opcion;
      if (i != null) d.Insumos[i.Id] = (int)numCantidad.Value;
      if (Guardar(() => Catalogo.Alta(d), "Estudio agregado al catálogo."))
        Ui.Cargar(tblEstudios, Catalogo.Tabla(), 12);
    }
  }

  /// <summary>
  /// P-25 Configurar franjas horarias de laboratorio (CU-B-04): con ayuno
  /// 07:00 a 11:00 y sin ayuno 07:00 a 17:00, y días de atención.
  /// </summary>
  class PFranjas : Pantalla
  {
    readonly TextBox txtAyuno = Ui.Caja("Ayuno"), txtSin = Ui.Caja("Sin");
    readonly TextBox txtDias = Ui.Caja("Dias");

    protected override void ConstruirNormal()
    {
      txtAyuno.Text = Parametros.Valor("lab_con_ayuno");
      txtSin.Text = Parametros.Valor("lab_sin_ayuno");
      txtDias.Text = Parametros.Valor("lab_dias");
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(NuevoCampo("ayuno", "Estudios con ayuno " +
        "(desde-hasta)", txtAyuno, true), NuevoCampo("sin", "Estudios sin " +
        "ayuno (desde-hasta)", txtSin, true)));
      t.Controls.Add(NuevoCampo("dias", "Días de atención", txtDias, true));
      t.Controls.Add(new LineaDeTiempo(txtAyuno, txtSin));
      t.Controls.Add(Ui.Botones(Ui.Boton("Guardar franjas",
        TipoBoton.Primario, (s, e) => GuardarFranjas())));
      Contenido.Controls.Add(t);
    }

    void GuardarFranjas()
    {
      Guardar(() =>
      {
        Parametros.Cambiar("lab_con_ayuno", txtAyuno.Text.Trim());
        Parametros.Cambiar("lab_sin_ayuno", txtSin.Text.Trim());
        Parametros.Cambiar("lab_dias", txtDias.Text.Trim());
      }, "Franjas guardadas. La recepción de laboratorio ya las aplica.");
    }
  }
}
