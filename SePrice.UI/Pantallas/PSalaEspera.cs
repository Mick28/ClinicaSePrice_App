using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-13 Atender al paciente (CU-G-08): la misma pantalla para el médico
  /// (sala de espera, CU-A-12) y el técnico (cola de estudios, CU-B-12).
  /// Ninguno ve obra social, teléfono ni DNI. Pensada para tablet.
  /// </summary>
  class PSalaEspera : Pantalla
  {
    bool Medico
    {
      get { return Sesion.Actual.Perfil == Perfil.Medico; }
    }

    protected override string TituloVisible
    {
      get
      {
        if (Auditoria) return "Atender al paciente";
        return Medico ? "Sala de espera" : "Cola de atención · " +
          (Atencion.MisSalas() ?? "sin agenda hoy");
      }
    }

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Atenciones y estudios de hoy", Consultas.Atenciones(
        Reloj.Hoy));
    }

    protected override void ConstruirNormal()
    {
      if (Medico && !Presentismo.Presente(Sesion.Actual.IdProfesional ?? 0))
      {
        Bloqueada();
        return;
      }
      List<EnEspera> l = Medico ? Atencion.SalaDeEspera()
        : Atencion.ColaDeEstudios();
      Contenido.Controls.Add(Ui.Pildora(l.Count(x => x.Estado ==
        "acreditado") + " en espera", Tema.Primario));
      foreach (EnEspera e in l.Where(x => x.Estado == "en atención"))
        Contenido.Controls.Add(EnAtencion(e));
      foreach (EnEspera e in l.Where(x => x.Estado == "acreditado"))
        Contenido.Controls.Add(Fila(e));
      if (l.Count == 0) Contenido.Controls.Add(Ui.Aviso(TipoAviso.Info,
        "No hay pacientes en espera."));
    }

    // Variante bloqueada: sin ingreso registrado no hay sala (CU-A-17).
    void Bloqueada()
    {
      Contenido.Controls.Add(Ui.Aviso(TipoAviso.Advertencia, "Para ver tu " +
        "sala de espera, registrá tu ingreso."));
      Contenido.Controls.Add(Ui.BotonesIzq(Ui.Boton("Registrar ingreso",
        TipoBoton.Primario, (s, e) => Ir("P-22"))));
    }

    Control EnAtencion(EnEspera e)
    {
      Tarjeta t = new Tarjeta { ColorBorde = Tema.Advertencia };
      t.Controls.Add(Ui.Pildora("En atención", Tema.Advertencia));
      t.Controls.Add(Ui.Subtitulo(Texto(e)));
      t.Controls.Add(Ui.BotonesIzq(Ui.Boton(Medico ? "Registrar atención"
        : "Registrar estudio", TipoBoton.Primario, (s, a) => Ir(Medico
        ? "P-14" : "P-15", e.IdTurno)).Tap(b => b.Height = 48)));
      return t;
    }

    static string Texto(EnEspera e)
    {
      return e.Hora + "  ·  " + e.Paciente + "  ·  " + e.Edad + " años" +
        (e.Detalle == null ? "" : "  ·  " + e.Detalle);
    }

    Control Fila(EnEspera e)
    {
      Tarjeta t = new Tarjeta { Padding = new Padding(16, 10, 16, 10) };
      Parrafo p = Ui.Texto(Texto(e) + "\nEspera: " + e.Espera + " min", 12f);
      Fila f = Ui.EnFila(p);
      if (e.Etiqueta != null) f.Controls.Add(Ui.Pildora(e.Etiqueta,
        e.Etiqueta == "Urgencia" ? Tema.Error : Tema.Advertencia));
      Button b = Ui.Boton("Llamar", TipoBoton.Primario, (s, a) => Llamar(e));
      b.Height = 48;
      b.MaximumSize = Ui.Tope(120);
      f.Controls.Add(b);
      t.Controls.Add(f);
      return t;
    }

    void Llamar(EnEspera e)
    {
      if (Intentar(() => Atencion.Llamar(e.IdTurno))) Ir("P-13");
    }
  }
}
