using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-11 Registrar llegada de paciente de laboratorio sin turno (CU-B-08,
  /// CU-G-05): orden de llegada dentro de la franja (con ayuno 07 a 11 h;
  /// sin ayuno 07 a 17 h). Rige la franja más restrictiva.
  /// </summary>
  class PLaboratorio : Pantalla
  {
    readonly List<CheckBox> estudios = new List<CheckBox>();
    readonly List<Estudio> lab = Catalogo.Estudios(false);

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Demanda sin turno de hoy", Consultas.DemandaDelDia(
        Reloj.Hoy));
    }

    protected override void ConstruirNormal()
    {
      if (!ExigirPaciente()) return;
      AgregarBarraPaciente();
      Contenido.Controls.Add(Ui.EnFila(Franjas(), Cola()));
    }

    Control Franjas()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Hora actual " + Formato.Hora(
        Reloj.Ahora)));
      t.Controls.Add(Ui.EnFila(Franja("Con ayuno", "lab_con_ayuno"),
        Franja("Sin ayuno", "lab_sin_ayuno")));
      t.Controls.Add(Ui.Nota("ESTUDIOS PEDIDOS *"));
      Casillas(t);
      t.Controls.Add(Ui.Botones(Ui.Boton("Registrar llegada",
        TipoBoton.Primario, (s, e) => Registrar())));
      return t;
    }

    // Una casilla por estudio de laboratorio (se indica si pide ayuno).
    void Casillas(Tarjeta t)
    {
      foreach (Estudio e in lab)
      {
        CheckBox c = Ui.Casilla("Estudio", e.Nombre + (e.RequiereAyuno
          ? "   (requiere ayuno)" : ""));
        c.Tag = e;
        estudios.Add(c);
        t.Controls.Add(c);
      }
    }

    static Control Franja(string nombre, string clave)
    {
      int[] f = Parametros.Franja(Parametros.Valor(clave));
      int m = Reloj.Ahora.Hour * 60 + Reloj.Ahora.Minute;
      bool vigente = m >= f[0] && m < f[1];
      return Ui.Indicador(nombre, Parametros.Valor(clave).Replace("-",
        " a "), vigente ? "Vigente" : "Cerrada", vigente ? Tema.Exito
        : Tema.Texto2);
    }

    Control Cola()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(360) };
      t.Controls.Add(Ui.Subtitulo("Cola de laboratorio"));
      t.Controls.Add(Ui.Tabla("Cola", Demanda.ColaLaboratorio(), 10));
      return t;
    }

    void Registrar()
    {
      List<Estudio> pedidos = estudios.Where(c => c.Checked).Select(c =>
        (Estudio)c.Tag).ToList();
      string nro = null;
      if (!Guardar(() => nro = Demanda.RegistrarLlegada(PacienteEnCurso
        .Actual.Id, pedidos), null)) return;
      Dialogo.Informar("Número de orden " + nro + ". Hay " +
        Math.Max(0, Demanda.AntesEnCola()) + " personas antes.");
      Ir("P-11");
    }
  }

  /// <summary>
  /// P-12 Solicitar estudio urgente (CU-B-09, CU-G-05): guardia o
  /// internación. Entra primero en la cola del técnico, vinculado al
  /// episodio. La guardia sólo busca por DNI (no crea ni edita legajos).
  /// </summary>
  partial class PUrgente : Pantalla
  {
    readonly TextBox txtDni = Ui.Caja("Dni");
    readonly Parrafo lblPaciente = Ui.Texto("—");
    readonly ComboBox cboSector = Ui.Combo("Sector", new[] { "guardia",
      "internación" });
    readonly TextBox txtEpisodio = Ui.Caja("Episodio");
    readonly ComboBox cboPrioridad = Ui.Combo("Prioridad", new[] {
      "urgente", "alta" });
    readonly ComboBox cboEstudio = Ui.Combo("Estudio",
      Catalogo.Estudios(true));
    readonly TextBox txtSolicitante = Ui.Caja("Solicitante");
    readonly TextBox txtObservaciones = Ui.Caja("Observaciones");
    Paciente paciente;

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Demanda sin turno de hoy", Consultas.DemandaDelDia(
        Reloj.Hoy));
    }
  }
}
