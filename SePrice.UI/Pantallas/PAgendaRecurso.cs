using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-26 Publicar agenda de estudios por recurso (CU-B-05): franjas por
  /// sala según la duración del estudio; impide la doble reserva.
  /// </summary>
  class PAgendaRecurso : Pantalla
  {
    readonly ComboBox cboSala = Ui.Combo("Sala",
      DatosBase.ListaLugares("sala"));
    readonly ComboBox cboTecnico = Ui.Combo("Tecnico",
      DatosBase.ListaProfesionales("técnico"));
    readonly ComboBox cboEstudio = Ui.Combo("Estudio",
      Catalogo.Estudios(true));
    readonly DateTimePicker dtpDesde = Ui.Fecha("Desde", Reloj.Hoy.AddDays(4));
    readonly DateTimePicker dtpHasta = Ui.Fecha("Hasta", Reloj.Hoy.AddDays(15));
    readonly ComboBox cboDesde = Ui.Hora("HDesde"), cboHasta = Ui.Hora("HHasta");
    readonly List<CheckBox> dias = new List<CheckBox>();
    readonly Columna vista = new Columna();

    protected override void ConstruirNormal()
    {
      cboDesde.SelectedItem = "08:00";
      cboHasta.SelectedItem = "12:00";
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(NuevoCampo("sala", "Sala o equipo", cboSala,
        true), NuevoCampo("tecnico", "Técnico/a", cboTecnico, true),
        NuevoCampo("estudio", "Estudio (define la duración)", cboEstudio,
        true)));
      t.Controls.Add(Ui.EnFila(NuevoCampo("periodo", "Desde", dtpDesde),
        new Campo("Hasta", dtpHasta, false), NuevoCampo("horario",
        "Hora desde", cboDesde, true), new Campo("Hora hasta", cboHasta,
        true)));
      t.Controls.Add(Dias());
      t.Controls.Add(Acciones());
      Contenido.Controls.Add(t);
      Contenido.Controls.Add(vista);
    }

    Control Acciones()
    {
      return Ui.Botones(Ui.Boton("Publicar agenda", TipoBoton.Primario,
        (s, e) => Publicar()), Ui.Boton("Ver vista previa",
        TipoBoton.Secundario, (s, e) => Previa()));
    }

    Control Dias()
    {
      FlowLayoutPanel f = Ui.BotonesIzq();
      foreach (string d in Asignaciones.Dias)
      {
        CheckBox c = Ui.Casilla(d, d);
        c.Width = 110;
        c.Checked = d != "sábado";
        dias.Add(c);
        f.Controls.Add(c);
      }
      return f;
    }

    AgendaRecurso Datos()
    {
      Opcion sala = cboSala.SelectedItem as Opcion;
      Estudio e = cboEstudio.SelectedItem as Estudio;
      AgendaRecurso r = new AgendaRecurso { IdSala = sala?.Id ?? 0,
        Sala = sala?.Texto.Split('·')[0].Trim(),
        IdTecnico = (cboTecnico.SelectedItem as Opcion)?.Id ?? 0,
        DuracionEstudio = e?.Duracion ?? 15, Desde = dtpDesde.Value,
        Hasta = dtpHasta.Value,
        HoraDesde = (cboDesde.SelectedItem as string) ?? "",
        HoraHasta = (cboHasta.SelectedItem as string) ?? ""
      };
      r.Dias.AddRange(dias.Where(c => c.Checked).Select(c => c.Text));
      return r;
    }

    void Previa()
    {
      vista.Controls.Clear();
      List<DiaAgenda> l = null;
      if (!Intentar(() => l = Agendas.PrevisualizarRecurso(Datos())))
        return;
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Vista previa: " + l.Sum(d => d.Turnos) +
        " turnos en " + l.Count + " días"));
      t.Controls.Add(Ui.Tabla("Vista", Tablas.De(l.Select(d => new {
        Día = Formato.Corta(d.Dia), Horario = d.Desde + " a " + d.Hasta,
        Sala = d.Lugar, Turnos = d.Turnos })), 10));
      vista.Controls.Add(t);
    }

    void Publicar()
    {
      int n = 0;
      if (Guardar(() => n = Agendas.PublicarRecurso(Datos()), null))
        Dialogo.Informar("Agenda publicada: " + n + " turnos.");
    }
  }

  /// <summary>
  /// Línea de tiempo de 06:00 a 18:00 con las franjas (P-25).
  /// </summary>
  [DesignerCategory("Code")]
  class LineaDeTiempo : Control
  {
    readonly TextBox ayuno, sin;

    public LineaDeTiempo(TextBox ayuno, TextBox sin)
    {
      this.ayuno = ayuno;
      this.sin = sin;
      Height = 90;
      DoubleBuffered = true;
      ResizeRedraw = true;
      ayuno.TextChanged += (s, e) => Invalidate();
      sin.TextChanged += (s, e) => Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      float x0 = 40, ancho = Width - 60, hora = ancho / 12f;
      for (int h = 0; h <= 12; h += 2)
        e.Graphics.DrawString((6 + h).ToString("00") + ":00",
          Tema.Fuente(7.5f), Brushes.Gray, x0 + h * hora - 14, 70);
      Barra(e.Graphics, ayuno.Text, "Con ayuno", 8, x0, hora, Tema.Advertencia);
      Barra(e.Graphics, sin.Text, "Sin ayuno", 38, x0, hora, Tema.Exito);
    }

    static void Barra(Graphics g, string franja, string texto, float y,
      float x0, float hora, Color c)
    {
      int[] f;
      try { f = Parametros.Franja(franja); } catch (ReglaException) { return; }
      float a = x0 + (f[0] / 60f - 6) * hora, b = x0 + (f[1] / 60f - 6) * hora;
      using (SolidBrush r = new SolidBrush(Ui.Suave(c)))
        g.FillRectangle(r, a, y, b - a, 24);
      g.DrawString(texto + " · " + franja, Tema.Fuente(8.5f),
        new SolidBrush(c), a + 6, y + 4);
    }
  }
}
