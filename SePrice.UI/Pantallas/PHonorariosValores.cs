using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PHonorarios
  {
    ComboBox cboPrestacion, cboModalidad;
    TextBox txtImporte;
    DataGridView tblValores;

    // Valores de honorarios por prestación y modalidad (a cargo de la
    // jefatura: dato financiero, no configuración del sistema).
    void Valores(Columna area)
    {
      CrearControles();
      Tarjeta f = new Tarjeta { MaximumSize = Ui.Tope(340) };
      f.Controls.Add(Ui.Subtitulo("Cargar valor"));
      f.Controls.Add(NuevoCampo("prestacion", "Prestación", cboPrestacion,
        true));
      f.Controls.Add(NuevoCampo("modalidad", "Modalidad de pago",
        cboModalidad, true));
      f.Controls.Add(NuevoCampo("importe", "Valor ($)", txtImporte, true));
      f.Controls.Add(Ui.Botones(Ui.Boton("Guardar valor", TipoBoton.Primario,
        (s, e) => GuardarValor())));
      Tarjeta t = new Tarjeta();
      t.Controls.Add(tblValores);
      area.Controls.Add(Ui.EnFila(t, f));
    }

    void CrearControles()
    {
      cboPrestacion = Ui.Combo("Prestacion", DatosBase.ListaEspecialidades());
      cboModalidad = Ui.Combo("Modalidad", new[] { "obra social",
        "particular" });
      txtImporte = Ui.Caja("Importe");
      tblValores = Ui.Tabla("Valores", Honorarios.Valores(), 12);
    }

    void GuardarValor()
    {
      decimal importe;
      if (!decimal.TryParse(txtImporte.Text.Replace(".", ""), out importe))
      {
        Dialogo.Error("Ingresá el valor en pesos, por ejemplo 24000.");
        return;
      }
      Opcion p = cboPrestacion.SelectedItem as Opcion;
      if (Guardar(() => Honorarios.FijarValor(p?.Id ?? 0, Ui.Elegido(
        cboModalidad), importe), "Valor de honorario guardado."))
        Ui.Cargar(tblValores, Honorarios.Valores(), 12);
    }
  }
}
