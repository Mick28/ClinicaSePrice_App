using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-05 Gestionar legajo de paciente (CU-G-02): busca por DNI, crea el
  /// legajo si no existe (uno solo por DNI, para los dos circuitos) y lo
  /// deja como paciente en curso.
  /// </summary>
  partial class PLegajo : Pantalla
  {
    readonly TextBox txtDni = Ui.Caja("Dni");
    readonly Columna resultado = new Columna { Espacio = 16 };

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Padrón de pacientes", Consultas.Pacientes());
    }

    protected override void ConstruirNormal()
    {
      AgregarBarraPaciente();
      Tarjeta t = new Tarjeta();
      Button buscar = Ui.Boton("Buscar paciente", TipoBoton.Primario,
        (s, e) => Buscar());
      t.Controls.Add(Ui.EnFila(new Campo("DNI del paciente *", txtDni,
        true), Abajo(buscar)));
      Contenido.Controls.Add(t);
      Contenido.Controls.Add(resultado);
      txtDni.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Buscar(); };
      if (PacienteEnCurso.Actual != null) Mostrar(PacienteEnCurso.Actual);
    }

    static Control Abajo(Button b)
    {
      FlowLayoutPanel f = Ui.BotonesIzq(b);
      f.Padding = new Padding(0, 22, 0, 0);
      f.Height = 66;
      f.MaximumSize = Ui.Tope(b.Width + 10);
      return f;
    }

    /// <summary>Lee el DNI (sin puntos) o avisa si no es válido.</summary>
    int? Dni()
    {
      int dni;
      string t = txtDni.Text.Replace(".", "").Trim();
      if (int.TryParse(t, out dni) && t.Length >= 7 && t.Length <= 8)
        return dni;
      Dialogo.Error("Ingresá un DNI de 7 u 8 dígitos.");
      return null;
    }

    void Buscar()
    {
      int? dni = Dni();
      if (dni == null) return;
      Paciente p = Pacientes.PorDni(dni.Value);
      if (p == null) SinResultado(dni.Value);
      else { PacienteEnCurso.Actual = p; Ir("P-05"); }
    }

    void SinResultado(int dni)
    {
      Limpiar();
      resultado.Controls.Add(Ui.Aviso(TipoAviso.Info, "No hay un legajo " +
        "con el DNI " + Formato.Dni(dni) + "."));
      resultado.Controls.Add(Ui.BotonesIzq(Ui.Boton("Crear legajo",
        TipoBoton.Primario, (s, e) => Formulario(new Paciente { Dni =
        dni }))));
    }

    void Limpiar()
    {
      while (resultado.Controls.Count > 0) resultado.Controls[0].Dispose();
      OlvidarCampos();
    }
  }
}
