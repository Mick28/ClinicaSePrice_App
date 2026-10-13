using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-22 Registrar ingreso y egreso del profesional (CU-G-13, sólo
  /// consultorios externos): el ingreso habilita la sala de espera.
  /// </summary>
  class PPresentismo : Pantalla
  {
    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Presentismo de los profesionales",
        Consultas.Presentismo());
    }

    protected override void ConstruirNormal()
    {
      Jornada j = Presentismo.Hoy();
      Tarjeta t = new Tarjeta { Espacio = 14 };
      t.Controls.Add(new Parrafo { Text = Formato.Hora(Reloj.Ahora),
        Font = Tema.Fuente(36f, FontStyle.Bold), ForeColor = Tema.Primario });
      t.Controls.Add(Ui.Texto(Formato.Larga(Reloj.Hoy), 12f));
      t.Controls.Add(Ui.Texto(j.TieneAgenda ? "Tu agenda de hoy: " + j.Lugar +
        " · " + j.Desde + " a " + j.Hasta : "No tenés agenda hoy.", 12f));
      t.Controls.Add(Ui.Pildora(Estado(j), Tema.DeEstado(j.Estado)));
      if (j.TieneAgenda && j.Egreso == null) t.Controls.Add(Boton(j));
      Contenido.Controls.Add(t);
      TablaDeLectura("Esta semana", Presentismo.Semana());
    }

    static string Estado(Jornada j)
    {
      if (j.Ingreso == null) return "Sin registrar";
      return "Presente desde " + j.Ingreso + (j.Egreso == null ? ""
        : " · egreso " + j.Egreso);
    }

    Control Boton(Jornada j)
    {
      bool ingreso = j.Ingreso == null;
      Button b = Ui.Boton(ingreso ? "Registrar ingreso" : "Registrar egreso",
        TipoBoton.Primario, (s, e) => { if (ingreso) Ingresar();
          else Egresar(); });
      b.MaximumSize = Size.Empty;
      b.Height = 64;
      b.Font = Tema.Fuente(14f, FontStyle.Bold);
      return b;
    }

    void Ingresar()
    {
      string aviso = null;
      if (!Intentar(() => aviso = Presentismo.RegistrarIngreso())) return;
      if (aviso != null) Dialogo.Informar(aviso, "Ingreso fuera de tolerancia");
      Dialogo.Informar("Ingreso registrado a las " + Formato.Hora(
        Reloj.Ahora) + ". Tu sala de espera ya está habilitada.");
      Ir("P-13");
    }

    // Egreso con pacientes pendientes: se advierte antes (CU-A-17).
    void Egresar()
    {
      int n = Presentismo.PacientesEsperando();
      string texto = n > 0 ? "¿Registrar el egreso de la jornada? Todavía " +
        "hay " + n + " paciente(s) acreditado(s) en espera." : "¿Registrar " +
        "el egreso de la jornada?";
      if (!Dialogo.Confirmar(texto, "Registrar egreso", "Registrar egreso",
        n > 0)) return;
      if (Intentar(() => Presentismo.RegistrarEgreso())) Ir("P-22");
    }
  }
}
