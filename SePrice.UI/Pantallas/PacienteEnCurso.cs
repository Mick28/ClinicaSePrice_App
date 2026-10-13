using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Paciente en curso (especificación 4.3 y material: «validación en
  /// búsquedas»): el DNI se valida una sola vez y las pantallas siguientes
  /// lo usan sin volver a pedirlo.
  /// </summary>
  static class PacienteEnCurso
  {
    /// <summary>Legajo elegido (null si todavía no hay).</summary>
    public static Paciente Actual;

    /// <summary>
    /// Barra «Paciente: Pérez, Ana · DNI 30.123.456 · OS Sanitas Federal»
    /// con el enlace «Cambiar paciente», o null si no hay paciente.
    /// </summary>
    public static Control Barra(Action cambiar)
    {
      if (Actual == null) return null;
      Paciente p = PorPerfil(Actual);
      Fila f = Ui.EnFila(new Parrafo { Text = p.Barra,
        Font = Tema.Fuente(10.5f, FontStyle.Bold), ForeColor = Tema.Primario,
        Padding = new Padding(12, 8, 0, 6) });
      Button b = Ui.Boton("Cambiar paciente", TipoBoton.Enlace, (s, e) =>
        cambiar());
      f.Controls.Add(b);
      f.BackColor = Tema.PrimarioSuave;
      return f;
    }

    static Paciente PorPerfil(Paciente p)
    {
      return Pacientes.PorId(p.Id) ?? p;
    }
  }
}
