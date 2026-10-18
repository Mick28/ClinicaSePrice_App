using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Calendario semanal de P-06: navegación por semanas, un botón por día
  /// con sus turnos libres y, del día elegido, todas las franjas (libres
  /// para elegir; ocupadas en gris).
  /// </summary>
  partial class PTurnos
  {
    DateTime semana;
    DateTime? diaElegido;
    List<Franja> franjasSemana = new List<Franja>();

    static readonly string[] Meses = { "enero", "febrero", "marzo", "abril",
      "mayo", "junio", "julio", "agosto", "septiembre", "octubre",
      "noviembre", "diciembre" };

    // «Consultar disponibilidad»: abre la semana de la fecha elegida.
    void Buscar()
    {
      semana = Lunes(dtpDesde.Value);
      diaElegido = null;
      Cargar();
    }

    static DateTime Lunes(DateTime d)
    {
      return d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));
    }

    // Consulta la base para la semana (de lunes a sábado) y la dibuja.
    void Cargar()
    {
      calendario.Controls.Clear();
      elegida = null;
      MostrarResumen();
      List<Franja> l = null;
      if (circuito == "A") BuscarA(ref l);
      else BuscarB(ref l);
      if (l == null) return;
      franjasSemana = l.Where(f => f.Inicio.Date >= semana &&
        f.Inicio.Date <= semana.AddDays(5)).ToList();
      if (diaElegido == null) diaElegido = franjasSemana.Where(f =>
        !f.Ocupada).Select(f => (DateTime?)f.Inicio.Date).FirstOrDefault();
      Dibujar();
    }

    void BuscarA(ref List<Franja> l)
    {
      Opcion esp = cboEspecialidad.SelectedItem as Opcion;
      if (esp == null) { Dialogo.Error("Elegí la especialidad."); return; }
      Opcion prof = cboProfesional.SelectedItem as Opcion;
      l = Turnos.Disponibles(esp.Id, prof?.Id, semana, 5, true);
    }

    void BuscarB(ref List<Franja> l)
    {
      Estudio e = cboEstudio.SelectedItem as Estudio;
      if (e == null) { Dialogo.Error("Elegí el estudio."); return; }
      if (e.EsLaboratorio) { Laboratorio(); return; }
      l = Turnos.DisponiblesEstudio(e.Id, semana, 5, true);
    }

    // Redibuja con lo ya consultado (al cambiar de día o elegir horario).
    void Dibujar()
    {
      calendario.Controls.Clear();
      Estudio e = cboEstudio.SelectedItem as Estudio;
      if (circuito == "B" && e != null && e.RequiereAyuno)
        calendario.Controls.Add(Ui.Aviso(TipoAviso.Info, "Este estudio " +
          "requiere ayuno: sólo se ofrecen turnos de " + Parametros.Valor(
          "lab_con_ayuno").Replace("-", " a ") + "."));
      calendario.Controls.Add(Semana());
      if (!franjasSemana.Any(f => !f.Ocupada)) SinDisponibilidad();
      if (diaElegido != null) MostrarDia(diaElegido.Value);
      Contenido.PerformLayout();
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
      string mensaje = "No hay turnos disponibles en esa semana.";
      Opcion esp = cboEspecialidad.SelectedItem as Opcion;
      if (circuito == "A" && esp != null)
      {
        DateTime? f = Turnos.PrimeraLibre(esp.Id, (cboProfesional
          .SelectedItem as Opcion)?.Id, semana.AddDays(7));
        mensaje += f == null ? " No hay agenda publicada en los próximos " +
          "dos meses." : " Primera fecha libre: " + Formato.Corta(f.Value) +
          ".";
      }
      calendario.Controls.Add(Ui.Aviso(TipoAviso.Advertencia, mensaje));
      if (circuito == "A") calendario.Controls.Add(Ui.BotonesIzq(Ui.Boton(
        "Asignar sobreturno", TipoBoton.Secundario, (s, e) => Ir("P-07"))));
    }

    // Encabezado: semana, navegación y un botón por día con su estado.
    Control Semana()
    {
      Tarjeta t = new Tarjeta { Espacio = 10 };
      t.Controls.Add(Ui.Subtitulo(TituloSemana()));
      Botonera nav = new Botonera();
      nav.Controls.Add(Ui.Boton("‹  Anterior", TipoBoton.Secundario,
        (s, e) => Mover(-7)));
      nav.Controls.Add(Ui.Boton("Hoy", TipoBoton.Secundario,
        (s, e) => Mover(0)));
      nav.Controls.Add(Ui.Boton("Siguiente  ›", TipoBoton.Secundario,
        (s, e) => Mover(7)));
      t.Controls.Add(nav);
      t.Controls.Add(DiasSemana());
      return t;
    }

    // Las seis tarjetas de la semana: tres arriba (lun a mié) y tres abajo
    // (jue a sáb).
    Control DiasSemana()
    {
      Columna c = new Columna { Espacio = 10 };
      for (int fila = 0; fila < 2; fila++)
      {
        Fila f = new Fila { Espacio = 10 };
        for (int i = 0; i < 3; i++)
          f.Controls.Add(TarjetaDia(semana.AddDays(fila * 3 + i)));
        c.Controls.Add(f);
      }
      return c;
    }

    // «Semana del 12 al 17 de octubre de 2026».
    string TituloSemana()
    {
      DateTime fin = semana.AddDays(5);
      return "Semana del " + semana.Day + (semana.Month == fin.Month ? ""
        : " de " + Meses[semana.Month - 1]) + " al " + fin.Day + " de " +
        Meses[fin.Month - 1] + " de " + fin.Year;
    }

    // Cambia de semana; 0 vuelve a la semana de hoy.
    void Mover(int dias)
    {
      semana = dias == 0 ? Lunes(Reloj.Hoy) : semana.AddDays(dias);
      diaElegido = null;
      Cargar();
    }

    // Tarjeta del día: «MAR», «20» y el estado («29 libres», «completo»,
    // «sin agenda» o «pasado»). La del día abierto se resalta.
    Control TarjetaDia(DateTime d)
    {
      int total = franjasSemana.Count(f => f.Inicio.Date == d);
      int libres = franjasSemana.Count(f => f.Inicio.Date == d &&
        !f.Ocupada);
      string estado = d < Reloj.Hoy ? "pasado" : total == 0 ? "sin agenda"
        : libres == 0 ? "completo" : libres + (libres == 1 ? " libre"
        : " libres");
      bool activo = diaElegido == d;
      Tarjeta t = new Tarjeta { Espacio = 2, Padding = new Padding(8, 10,
        8, 10), BackColor = activo ? Tema.PrimarioSuave : Tema.FilaAlterna,
        ColorBorde = activo ? Tema.Primario : Tema.Borde };
      t.Controls.Add(LineaDia(Formato.Corta(d).Substring(0, 3)
        .ToUpperInvariant(), 9f, FontStyle.Regular, Tema.Texto2));
      t.Controls.Add(LineaDia(d.Day.ToString(), 15f, FontStyle.Bold,
        activo ? Tema.Primario : Tema.Texto));
      t.Controls.Add(LineaDia(estado, 9f, FontStyle.Bold, libres > 0
        ? Tema.Exito : Tema.Texto2));
      if (total > 0) ClicDia(t, d);
      return t;
    }

    static Parrafo LineaDia(string texto, float puntos, FontStyle estilo,
      Color color)
    {
      return new Parrafo { Text = texto, Font = Tema.Fuente(puntos, estilo),
        ForeColor = color, TextAlign = ContentAlignment.MiddleCenter };
    }

    // Toda la tarjeta (y sus textos) abre el día.
    void ClicDia(Control t, DateTime d)
    {
      t.Cursor = Cursors.Hand;
      EventHandler clic = (s, e) => { diaElegido = d; Dibujar(); };
      t.Click += clic;
      foreach (Control c in t.Controls) c.Click += clic;
    }

    // Franjas del día elegido, por profesional o sala, y la referencia.
    void MostrarDia(DateTime d)
    {
      foreach (var g in franjasSemana.Where(f => f.Inicio.Date == d)
        .GroupBy(f => f.Profesional + f.Lugar))
        calendario.Controls.Add(Dia(g.ToList()));
      calendario.Controls.Add(Ui.Nota("Referencia: con borde, libre · " +
        "relleno, elegido · gris, ocupado."));
    }

    Control Dia(List<Franja> franjas)
    {
      Franja f0 = franjas[0];
      Tarjeta t = new Tarjeta { Espacio = 8 };
      t.Controls.Add(Ui.Texto(Formato.Corta(f0.Inicio) + " · " +
        f0.Profesional + " · " + f0.Lugar + " · intervalos de " +
        f0.Duracion + " min", 10.5f).Tap(p => p.Font =
        Tema.Fuente(10.5f, FontStyle.Bold)));
      Botonera b = new Botonera();
      foreach (Franja f in franjas) b.Controls.Add(BotonFranja(f));
      t.Controls.Add(b);
      return t;
    }

    // Libre: se elige. Ocupada: gris y deshabilitada. Elegida: rellena.
    Button BotonFranja(Franja f)
    {
      bool esta = elegida != null && elegida.IdAgenda == f.IdAgenda &&
        elegida.Inicio == f.Inicio;
      Button b = Ui.Boton(Formato.Hora(f.Inicio) + (f.Ocupada ? " · ocupado"
        : ""), esta ? TipoBoton.Primario : TipoBoton.Secundario, null);
      b.MaximumSize = Size.Empty;
      b.Width = 130;
      if (f.Ocupada)
      {
        b.Enabled = false;
        b.BackColor = Tema.Auditoria;
        b.FlatAppearance.BorderColor = Tema.Auditoria;
      }
      else b.Click += (s, e) => { elegida = f; MostrarResumen(); Dibujar(); };
      return b;
    }
  }
}
