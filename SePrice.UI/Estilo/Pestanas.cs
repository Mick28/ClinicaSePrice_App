using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SePrice.UI
{
  /// <summary>
  /// Pestañas: una fila de botones y un área que se rearma al elegir. Cada
  /// perfil recibe sólo sus pestañas (las ajenas no se dibujan, 3.5.6).
  /// </summary>
  [DesignerCategory("Code")]
  class Pestanas : Columna
  {
    readonly FlowLayoutPanel tira = Ui.BotonesIzq();
    readonly Columna area = new Columna { Espacio = 16 };
    Button activa;

    /// <summary>Se llama con el nombre de la pestaña y el área vacía.</summary>
    public Action<string, Columna> AlElegir;

    public Pestanas()
    {
      Espacio = 12;
      tira.Height = 40;
      Controls.Add(tira);
      Controls.Add(area);
    }

    /// <summary>Agrega una pestaña.</summary>
    public void Agregar(string nombre)
    {
      Button b = Ui.Boton(nombre, TipoBoton.Secundario, null);
      b.Click += (s, e) => Elegir(b);
      tira.Controls.Add(b);
      if (activa == null) activa = b;
    }

    /// <summary>
    /// Muestra la pestaña indicada (por defecto, la primera).
    /// </summary>
    public void Iniciar(int indice = 0)
    {
      if (indice > 0 && indice < tira.Controls.Count)
        activa = (Button)tira.Controls[indice];
      if (activa != null) Elegir(activa);
    }

    void Elegir(Button b)
    {
      foreach (Control c in tira.Controls)
      {
        c.BackColor = c == b ? Tema.Primario : Tema.Superficie;
        c.ForeColor = c == b ? Color.White : Tema.Primario;
      }
      activa = b;
      area.SuspendLayout();
      // Se quitan sin desechar: los controles de la pantalla se reutilizan.
      area.Controls.Clear();
      AlElegir(b.Text, area);
      area.ResumeLayout();
      PerformLayout();
    }
  }
}
