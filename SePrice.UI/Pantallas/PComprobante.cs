using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-08 Emitir comprobante de turno (CU-A-09): se abre siempre al
  /// asignar o reprogramar. Se imprime o se guarda como PDF desde la
  /// vista previa de impresión.
  /// </summary>
  class PComprobante : Pantalla
  {
    Comprobante c;

    protected override void ConstruirNormal()
    {
      if (!ExigirTurno("P-06", "Asignar turno", "El comprobante se " +
        "emite al asignar un turno.")) return;
      c = Turnos.Comprobante((int)Parametro);
      Contenido.Controls.Add(Ui.Aviso(TipoAviso.Exito, "El turno se " +
        "registró correctamente."));
      Contenido.Controls.Add(Tarjeta());
      Contenido.Controls.Add(Ui.BotonesIzq(Ui.Boton("Volver al inicio",
        TipoBoton.Primario, (s, e) => Ir("P-01")), Ui.Boton("Dar otro turno" +
        " al mismo paciente", TipoBoton.Secundario, (s, e) => Ir("P-06")),
        Ui.Boton("Imprimir o guardar PDF", TipoBoton.Secundario, (s, e) =>
        Imprimir())));
    }

    string Encabezado
    {
      get { return "Turno N.° " + Formato.Dni(c.Numero) + " · Emitido " +
        Formato.Corta(c.Emision) + " " + Formato.Hora(c.Emision); }
    }

    Control Tarjeta()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(720) };
      t.ColorBorde = Tema.Primario;
      t.Controls.Add(Ui.Subtitulo(Encabezado));
      t.Controls.Add(Ui.EnFila(Dato("Paciente", c.Paciente), Dato("Circuito",
        c.Circuito)));
      t.Controls.Add(Ui.EnFila(Dato(c.Circuito.StartsWith("Estudios")
        ? "Estudio" : "Profesional", c.Detalle), Dato("Fecha y hora",
        Formato.Corta(c.FechaHora) + " · " + Formato.Hora(c.FechaHora))));
      t.Controls.Add(Ui.EnFila(Dato("Lugar", c.Lugar), Dato("Modalidad",
        c.Modalidad)));
      if (c.Sobreturno) t.Controls.Add(Ui.Pildora("Sobreturno",
        Tema.Advertencia));
      t.Controls.Add(Ui.Aviso(TipoAviso.Info, c.Indicaciones));
      return t;
    }

    static Campo Dato(string e, string v)
    {
      return new Campo(e, Ui.Fijo(e, v), false);
    }

    void Imprimir()
    {
      PrintDocument d = new PrintDocument { DocumentName = "Turno " +
        c.Numero };
      d.PrintPage += (s, e) => Dibujar(e.Graphics);
      using (PrintPreviewDialog p = new PrintPreviewDialog { Document = d })
        p.ShowDialog();
    }

    void Dibujar(Graphics g)
    {
      string logo = Path.Combine(Application.StartupPath, "logo.png");
      if (File.Exists(logo)) g.DrawImage(Image.FromFile(logo), 60, 50);
      string texto = "Turno N.° " + c.Numero + "\n" + c.Paciente + "\n" +
        c.Circuito + "\n" + c.Detalle + "\n" + Formato.Corta(c.FechaHora) +
        " · " + Formato.Hora(c.FechaHora) + "\n" + c.Lugar + "\n" +
        (c.Sobreturno ? "SOBRETURNO\n" : "") + "\n" + c.Indicaciones;
      g.DrawString(texto, Tema.Fuente(12f), Brushes.Black, new RectangleF(60,
        130, 650, 600));
    }
  }
}
