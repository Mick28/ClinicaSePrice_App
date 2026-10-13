using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SePrice.UI
{
  /// <summary>
  /// Campo de formulario: etiqueta arriba (nunca sólo un texto de ayuda),
  /// el control y, si hace falta, el error junto al campo (2.4 y 2.8).
  /// </summary>
  [DesignerCategory("Code")]
  class Campo : Panel
  {
    readonly Label etiqueta;
    readonly Parrafo error;

    /// <summary>Control de ingreso (TextBox, ComboBox, etc.).</summary>
    public readonly Control Control;

    /// <summary>Obligatorio: lleva asterisco y se controla vacío.</summary>
    public readonly bool Obligatorio;

    public Campo(string texto, Control control, bool obligatorio)
    {
      Control = control;
      Obligatorio = obligatorio;
      texto = texto.EndsWith(" *") ? texto.Substring(0, texto.Length - 2)
        : texto;
      etiqueta = new Label { Text = texto + (obligatorio ? " *" : ""),
        Font = Tema.Fuente(9.5f, FontStyle.Bold), ForeColor = Tema.Texto,
        AutoSize = false, Height = 22 };
      error = new Parrafo { ForeColor = Tema.Error, Visible = false,
        Font = Tema.Fuente(9f) };
      Controls.AddRange(new Control[] { etiqueta, control, error });
      Height = 26 + control.Height + 4;
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
      base.OnLayout(e);
      etiqueta.SetBounds(0, 0, Width, 22);
      Control.SetBounds(0, 24, Width, 0, Columna.PosicionYAncho);
      int y = 24 + Control.Height + 4;
      if (error.Visible)
      {
        error.SetBounds(0, y, Width, error.Height);
        y += error.Height;
      }
      if (Height != y) Height = y;
    }

    /// <summary>Muestra el error debajo del campo.</summary>
    public void MostrarError(string mensaje)
    {
      error.Text = mensaje;
      error.Visible = true;
      PerformLayout();
    }

    /// <summary>Oculta el error.</summary>
    public void LimpiarError()
    {
      if (!error.Visible) return;
      error.Visible = false;
      PerformLayout();
    }

    /// <summary>¿El campo obligatorio quedó vacío?</summary>
    public bool Vacio
    {
      get
      {
        ComboBox c = Control as ComboBox;
        if (c != null) return Ui.Elegido(c) == null;
        return Control is TextBox && string.IsNullOrWhiteSpace(Control.Text);
      }
    }

    /// <summary>Vacía el control (botón «Blanquear»).</summary>
    public void Blanquear()
    {
      LimpiarError();
      if (Control is TextBox) Control.Text = "";
      ComboBox c = Control as ComboBox;
      if (c != null) c.SelectedIndex = c.Items.Count > 0 &&
        c.Items[0] == Ui.Seleccionar ? 0 : -1;
      CheckBox k = Control as CheckBox;
      if (k != null) k.Checked = false;
    }
  }
}
