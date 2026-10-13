using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PAcreditar
  {
    TextBox txtSolicitante, txtMatricula, txtEstudios;
    DateTimePicker dtpEmision, dtpVencimiento;

    // Panel de cobertura: sólo con obra social. En estudios, la orden.
    void Panel()
    {
      panel.Controls.Clear();
      validacion = null;
      Paciente p = PacienteEnCurso.Actual;
      if (!optObra.Checked) { Arancel(); return; }
      panel.Controls.Add(Dato("Obra social", p.ObraSocial ?? "—"));
      panel.Controls.Add(Ui.EnFila(Dato("Plan", p.Plan), Dato("Afiliado",
        p.Afiliado)));
      if (Turno != null && Turno.EsEstudio) Orden();
      panel.Controls.Add(Ui.BotonesIzq(Ui.Boton("Validar cobertura",
        TipoBoton.Secundario, (s, e) => Validar())));
    }

    // Particular: se informa el arancel (parámetro, a confirmar).
    void Arancel()
    {
      panel.Controls.Add(Dato("Arancel particular", Formato.Pesos(
        Acreditacion.Arancel(Turno != null && Turno.EsEstudio))));
      panel.Controls.Add(Ui.Nota("Valor de ejemplo, a confirmar con el " +
        "cliente."));
    }

    static Campo Dato(string e, string v)
    {
      return new Campo(e, Ui.Fijo(e, v), false);
    }

    // Orden médica (CU-B-11): sin orden, la acreditación queda bloqueada.
    void Orden()
    {
      txtSolicitante = Ui.Caja("Solicitante");
      txtMatricula = Ui.Caja("Matricula");
      txtEstudios = Ui.Caja("Estudios");
      txtEstudios.Text = Turno.Detalle;
      dtpEmision = Ui.Fecha("Emision", Reloj.Hoy);
      dtpVencimiento = Ui.Fecha("Vencimiento", Reloj.Hoy.AddDays(30));
      panel.Controls.Add(Ui.Nota("ORDEN MÉDICA *"));
      panel.Controls.Add(NuevoCampo("orden", "Médico/a solicitante",
        txtSolicitante));
      panel.Controls.Add(Ui.EnFila(new Campo("Matrícula", txtMatricula,
        false), new Campo("Emisión", dtpEmision, false), new Campo(
        "Vencimiento", dtpVencimiento, false)));
    }

    void Validar()
    {
      if (Turno == null) { Dialogo.Error("Elegí el turno a acreditar.");
        return; }
      int? orden = null;
      if (Turno.EsEstudio && !Intentar(() => orden = Acreditacion
        .RegistrarOrden(PacienteEnCurso.Actual.Id, Orden(true)))) return;
      Validacion v = null;
      if (!Intentar(() => v = Acreditacion.Validar(Turno.IdTurno, orden,
        Turno.EsEstudio))) return;
      validacion = v;
      Resultado(v);
    }

    DatosOrden Orden(bool leer)
    {
      if (string.IsNullOrWhiteSpace(txtSolicitante.Text)) return null;
      return new DatosOrden { Solicitante = txtSolicitante.Text,
        Matricula = txtMatricula.Text, Estudios = txtEstudios.Text,
        Emision = dtpEmision.Value, Vencimiento = dtpVencimiento.Value };
    }

    // Válida (verde), rechazada (rojo, con «Continuar como particular») o
    // pendiente por validación diferida (ámbar).
    void Resultado(Validacion v)
    {
      TipoAviso tipo = v.Resultado == "válida" ? TipoAviso.Exito
        : v.Resultado == "rechazada" ? TipoAviso.Error : TipoAviso.Advertencia;
      panel.Controls.Add(Ui.Aviso(tipo, v.Mensaje));
      if (v.Resultado == "rechazada") panel.Controls.Add(Ui.BotonesIzq(
        Ui.Boton("Continuar como particular", TipoBoton.Secundario, (s, e) =>
        optParticular.Checked = true)));
    }

    // Validaciones diferidas pendientes de regularizar (CU-A-11).
    void Pendientes()
    {
      System.Data.DataTable t = Acreditacion.Pendientes();
      if (t.Rows.Count == 0) return;
      Tarjeta c = new Tarjeta();
      c.Controls.Add(Ui.Subtitulo("Validaciones pendientes de regularizar"));
      DataGridView g = Ui.Tabla("Pendientes", t, 5);
      c.Controls.Add(g);
      c.Controls.Add(Ui.BotonesIzq(Ui.Boton("Registrar respuesta: válida",
        TipoBoton.Secundario, (s, e) => Regularizar(g, true)), Ui.Boton(
        "Registrar respuesta: rechazada", TipoBoton.Secundario, (s, e) =>
        Regularizar(g, false))));
      Contenido.Controls.Add(c);
    }

    void Regularizar(DataGridView g, bool valida)
    {
      int? id = Ui.Elegido(g);
      if (id == null) { Dialogo.Error("Elegí una validación."); return; }
      if (Guardar(() => Acreditacion.Regularizar(id.Value, valida), valida
        ? "Validación regularizada." : "Rechazada: la atención se " +
        "reclasificó como particular.")) Ir("P-10");
    }
  }
}
