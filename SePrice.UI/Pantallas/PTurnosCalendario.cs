using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PTurnos
  {
    void Buscar()
    {
      calendario.Controls.Clear();
      List<Franja> l = null;
      if (circuito == "A") BuscarA(ref l);
      else BuscarB(ref l);
      if (l == null) return;
      if (l.Count == 0) SinDisponibilidad();
      foreach (var dia in l.GroupBy(f => f.Inicio.Date.ToString("yyyyMMdd") +
        f.Profesional + f.Lugar)) calendario.Controls.Add(Dia(dia.ToList()));
      Contenido.PerformLayout();
    }

    void BuscarA(ref List<Franja> l)
    {
      Opcion esp = cboEspecialidad.SelectedItem as Opcion;
      if (esp == null) { Dialogo.Error("Elegí la especialidad."); return; }
      Opcion prof = cboProfesional.SelectedItem as Opcion;
      l = Turnos.Disponibles(esp.Id, prof?.Id, dtpDesde.Value);
    }

    void BuscarB(ref List<Franja> l)
    {
      Estudio e = cboEstudio.SelectedItem as Estudio;
      if (e == null) { Dialogo.Error("Elegí el estudio."); return; }
      if (e.EsLaboratorio) { Laboratorio(); return; }
      if (e.RequiereAyuno) calendario.Controls.Add(Ui.Aviso(TipoAviso.Info,
        "Este estudio requiere ayuno: sólo se ofrecen turnos de " +
        Parametros.Valor("lab_con_ayuno").Replace("-", " a ") + "."));
      l = Turnos.DisponiblesEstudio(e.Id, dtpDesde.Value);
    }

    // CU-B-06, alternativo: el laboratorio no usa turnos.
    void Laboratorio()
    {
      calendario.Controls.Add(Ui.Aviso(TipoAviso.Info, "Los estudios de " +
        "laboratorio no requieren turno: se atienden por orden de llegada."));
      calendario.Controls.Add(Ui.BotonesIzq(Ui.Boton("Ir a Laboratorio sin " +
        "turno", TipoBoton.Secundario, (s, e) => Ir("P-11"))));
    }

    // CU-A-05, alternativo: primera fecha libre o sobreturno.
    void SinDisponibilidad()
    {
      string mensaje = "No hay franjas libres en esa semana.";
      Opcion esp = cboEspecialidad.SelectedItem as Opcion;
      if (circuito == "A" && esp != null)
      {
        DateTime? f = Turnos.PrimeraLibre(esp.Id, (cboProfesional
          .SelectedItem as Opcion)?.Id, dtpDesde.Value.AddDays(7));
        mensaje += f == null ? " No hay agenda publicada en los próximos " +
          "dos meses." : " Primera fecha libre: " + Formato.Corta(f.Value) +
          ".";
      }
      calendario.Controls.Add(Ui.Aviso(TipoAviso.Advertencia, mensaje));
      if (circuito == "A") calendario.Controls.Add(Ui.BotonesIzq(Ui.Boton(
        "Asignar sobreturno", TipoBoton.Secundario, (s, e) => Ir("P-07"))));
    }

    Control Dia(List<Franja> franjas)
    {
      Franja f0 = franjas[0];
      Tarjeta t = new Tarjeta { Espacio = 8 };
      t.Controls.Add(Ui.Texto(Formato.Corta(f0.Inicio) + " · " +
        f0.Profesional + " · " + f0.Lugar, 10.5f).Tap(p => p.Font =
        Tema.Fuente(10.5f, FontStyle.Bold)));
      Botonera b = new Botonera();
      foreach (Franja f in franjas) b.Controls.Add(BotonFranja(f));
      t.Controls.Add(b);
      return t;
    }

    Button BotonFranja(Franja f)
    {
      Button b = Ui.Boton(Formato.Hora(f.Inicio), TipoBoton.Secundario, null);
      b.Width = 76;
      b.Click += (s, e) => { elegida = f; MostrarResumen(); };
      return b;
    }
  }
}
