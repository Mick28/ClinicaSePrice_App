using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PUrgente
  {
    protected override void ConstruirNormal()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(FilaPaciente());
      t.Controls.Add(Ui.EnFila(NuevoCampo("sector", "Sector", cboSector,
        true), NuevoCampo("episodio", "Habitación o box", txtEpisodio),
        NuevoCampo("prioridad", "Prioridad", cboPrioridad, true)));
      t.Controls.Add(Ui.EnFila(NuevoCampo("estudio", "Estudio", cboEstudio,
        true), NuevoCampo("solicitante", "Médico/a solicitante",
        txtSolicitante, true), NuevoCampo("obs", "Observaciones",
        txtObservaciones)));
      t.Controls.Add(Ui.Botones(Ui.Boton("Enviar solicitud",
        TipoBoton.Primario, (s, e) => Enviar()), BotonBlanquear()));
      Contenido.Controls.Add(t);
      TablaDeLectura("Mis solicitudes de hoy", Demanda.MisSolicitudes());
    }

    // DNI, botón Buscar (alineado con la caja) y nombre del paciente.
    Control FilaPaciente()
    {
      return Ui.EnFila(NuevoCampo("dni", "DNI del paciente", txtDni, true),
        Ui.BotonesIzq(Ui.Boton("Buscar", TipoBoton.Secundario, (s, e) =>
        Buscar())).Tap(f => { f.Padding = new Padding(0, 22, 0, 0);
        f.Height = 66; f.MaximumSize = Ui.Tope(110); }), lblPaciente);
    }

    // La guardia ve nombre y edad; no crea ni edita legajos (5.3).
    void Buscar()
    {
      int dni;
      paciente = int.TryParse(txtDni.Text.Replace(".", "").Trim(), out dni)
        ? Pacientes.PorDni(dni) : null;
      lblPaciente.Text = paciente == null ? "No hay legajo con ese DNI: " +
        "pedí a admisión que lo cree para enviar la solicitud."
        : paciente.Nombre + " · " + paciente.Edad + " años";
      lblPaciente.ForeColor = paciente == null ? Tema.Advertencia
        : Tema.Texto;
    }

    void Enviar()
    {
      if (paciente == null) { Dialogo.Error("Buscá un paciente con legajo " +
        "por DNI."); return; }
      SolicitudUrgente s = new SolicitudUrgente { IdPaciente = paciente.Id,
        Sector = Ui.Elegido(cboSector), Prioridad = Ui.Elegido(cboPrioridad),
        IdEstudio = (cboEstudio.SelectedItem as Estudio)?.Id ?? 0,
        Episodio = txtEpisodio.Text, Solicitante = txtSolicitante.Text,
        Observaciones = txtObservaciones.Text };
      if (Guardar(() => Demanda.SolicitarUrgente(s), "Solicitud enviada. " +
        "El estudio quedó primero en la cola de " + ((Estudio)cboEstudio
        .SelectedItem).SalaEquipo + ".")) Ir("P-12");
    }
  }
}
