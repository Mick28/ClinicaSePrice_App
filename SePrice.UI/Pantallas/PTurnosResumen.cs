using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PTurnos
  {
    Control Resumen()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(340) };
      t.Controls.Add(Ui.Subtitulo("Resumen"));
      t.Controls.Add(resumen);
      MostrarResumen();
      return t;
    }

    // Panel resumen: paciente, circuito, profesional o estudio, fecha y
    // hora, duración, lugar y modalidad; botón «Asignar turno».
    void MostrarResumen()
    {
      resumen.Controls.Clear();
      if (elegida == null)
      {
        resumen.Controls.Add(Ui.Nota("Elegí una franja libre del " +
          "calendario."));
        return;
      }
      Lineas();
      resumen.Controls.Add(Ui.Botones(Ui.Boton("Asignar turno",
        TipoBoton.Primario, (s, e) => Asignar())));
    }

    void Lineas()
    {
      Linea("Paciente", PacienteEnCurso.Actual.Nombre);
      Linea(circuito == "A" ? "Profesional" : "Estudio", circuito == "A"
        ? elegida.Profesional : (cboEstudio.SelectedItem as Estudio).Nombre);
      Linea("Fecha y hora", elegida.ToString());
      Linea("Duración", elegida.Duracion + " min");
      Linea(circuito == "A" ? "Consultorio" : "Sala", elegida.Lugar + " · " +
        elegida.Ubicacion);
      Linea("Modalidad", Modalidad);
    }

    void Linea(string etiqueta, string valor)
    {
      resumen.Controls.Add(Ui.Nota(etiqueta.ToUpperInvariant()));
      resumen.Controls.Add(Ui.Texto(valor));
    }

    void Asignar()
    {
      int id = 0;
      if (Guardar(() => id = Turnos.Asignar(PacienteEnCurso.Actual.Id,
        elegida, Modalidad), null)) Ir("P-08", id);
    }

    // Turnos de control pedidos por los profesionales (CU-A-13).
    void Controles()
    {
      System.Data.DataTable c = Turnos.ControlesPendientes();
      if (c.Rows.Count == 0) return;
      TablaDeLectura("Turnos de control pedidos en la atención", c,
        "El profesional indicó un control: coordinalo con el paciente.");
    }
  }
}
