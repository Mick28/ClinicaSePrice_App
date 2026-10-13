using System.ComponentModel;
using System.Windows.Forms;

namespace SePrice.UI
{
  /// <summary>
  /// Ubica botones de igual tamaño en filas que se cortan al ancho
  /// disponible (franjas del calendario de turnos) y ajusta su altura.
  /// </summary>
  [DesignerCategory("Code")]
  class Botonera : Panel
  {
    public int Espacio = 8;

    protected override void OnLayout(LayoutEventArgs e)
    {
      base.OnLayout(e);
      int x = 0, y = 0, alto = 0;
      foreach (Control c in Controls)
      {
        if (x > 0 && x + c.Width > ClientSize.Width)
        {
          x = 0;
          y += alto + Espacio;
        }
        c.Location = new System.Drawing.Point(x, y);
        x += c.Width + Espacio;
        alto = c.Height;
      }
      int total = Controls.Count == 0 ? 0 : y + alto;
      if (Height != total) Height = total;
    }
  }
}
