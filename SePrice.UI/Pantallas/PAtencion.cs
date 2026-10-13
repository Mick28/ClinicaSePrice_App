using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-14 Registrar atención y turno de control (CU-A-13): hora de inicio
  /// (al llamar) y de fin (al registrar); si hace falta, el pedido de
  /// control con su plazo. No es historia clínica.
  /// </summary>
  class PAtencion : Pantalla
  {
    readonly CheckBox chkControl = Ui.Casilla("Control", "Requiere turno de " +
      "control");
    readonly RadioButton opt7 = Ui.Opcion("7", "7 días"),
      opt15 = Ui.Opcion("15", "15 días"), opt30 = Ui.Opcion("30", "30 días");
    EnEspera turno;

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Atenciones de hoy", Consultas.Atenciones(Reloj.Hoy));
    }

    protected override void ConstruirNormal()
    {
      if (!ExigirTurno("P-13", "Ir a la sala de espera", "Llamá a un " +
        "paciente desde la sala de espera o la cola de atención.")) return;
      turno = Atencion.Detalle((int)Parametro);
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo(turno.Paciente + " · " + turno.Edad +
        " años · turno " + turno.Hora));
      t.Controls.Add(Ui.Texto("Inicio de atención: " + Atencion.Inicio(
        turno.IdTurno) + " · el fin se registra al confirmar."));
      chkControl.Font = Tema.Fuente(12f);
      chkControl.Height = 40;
      opt15.Checked = true;
      t.Controls.Add(chkControl);
      t.Controls.Add(Ui.Grupo(opt7, opt15, opt30));
      t.Controls.Add(Acciones());
      Contenido.Controls.Add(t);
    }

    // Botones grandes: el médico los usa entre paciente y paciente.
    Control Acciones()
    {
      return Ui.Botones(Ui.Boton("Registrar atención", TipoBoton.Primario,
        (s, e) => Registrar()).Tap(b => b.Height = 48), Ui.Boton("Volver " +
        "a la sala de espera", TipoBoton.Secundario, (s, e) => Ir("P-13"))
        .Tap(b => b.Height = 48));
    }

    int Plazo
    {
      get { return opt7.Checked ? 7 : opt30.Checked ? 30 : 15; }
    }

    void Registrar()
    {
      bool control = chkControl.Checked;
      if (!Guardar(() => Atencion.RegistrarAtencion(turno.IdTurno, control,
        Plazo), null)) return;
      Dialogo.Informar(control ? "Atención registrada. Se pidió un turno " +
        "de control para dentro de " + Plazo + " días: administración lo " +
        "va a coordinar." : "Atención registrada.");
      Ir("P-13");
    }
  }

  /// <summary>
  /// P-15 Registrar realización del estudio y consumo de insumos (CU-B-13,
  /// incluye CU-G-10): descuenta stock y emite el comprobante de retiro.
  /// </summary>
  class PEstudio : Pantalla
  {
    readonly Dictionary<int, NumericUpDown> cantidades =
      new Dictionary<int, NumericUpDown>();
    readonly DateTimePicker dtpEstimada = Ui.Fecha("Estimada",
      Reloj.Hoy.AddDays(2));
    EnEspera turno;

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Estudios de hoy", Consultas.Atenciones(Reloj.Hoy));
    }

    protected override void ConstruirNormal()
    {
      if (!ExigirTurno("P-13", "Ir a la sala de espera", "Llamá a un " +
        "paciente desde la sala de espera o la cola de atención.")) return;
      turno = Atencion.Detalle((int)Parametro);
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo(turno.Paciente + " · " + turno.Edad +
        " años · " + turno.Detalle + " · " + turno.Lugar));
      t.Controls.Add(Ui.Nota("INSUMOS USADOS (cantidades estándar del " +
        "estudio, editables)"));
      foreach (string[] i in Catalogo.InsumosDe(turno.IdEstudio.Value))
        t.Controls.Add(Insumo(i));
      t.Controls.Add(new Campo("Fecha estimada del resultado", dtpEstimada,
        true).Tap(c => c.MaximumSize = Ui.Tope(260)));
      t.Controls.Add(Ui.Botones(Ui.Boton("Registrar estudio",
        TipoBoton.Primario, (s, e) => Registrar()).Tap(b => b.Height = 48)));
      Contenido.Controls.Add(t);
    }

    Control Insumo(string[] i)
    {
      NumericUpDown n = Ui.Numero("Cant" + i[0], 0, 999, int.Parse(i[2]));
      n.Font = Tema.Fuente(13f);
      cantidades[int.Parse(i[0])] = n;
      return Ui.EnFila(Ui.Texto(i[1] + " (" + i[3] + ")", 12f), n.Tap(x =>
        x.MaximumSize = Ui.Tope(120)));
    }

    void Registrar()
    {
      Dictionary<int, int> usados = cantidades.Where(c => c.Value.Value > 0)
        .ToDictionary(c => c.Key, c => (int)c.Value.Value);
      EstudioRegistrado r = null;
      if (!Guardar(() => r = Atencion.RegistrarEstudio(turno.IdTurno, usados,
        dtpEstimada.Value), null)) return;
      foreach (string a in r.Alertas) Dialogo.Informar(a, "Stock bajo mínimo");
      Dialogo.Informar(r.Internacion ? "Estudio realizado. El resultado se " +
        "envía al sector: no se emite comprobante de retiro." : "Estudio " +
        "realizado. Comprobante de retiro " + r.Comprobante + " · resultado " +
        "estimado para el " + Formato.Corta(r.FechaEstimada) + ".");
      Ir("P-13");
    }
  }
}
