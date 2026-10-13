using System;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PDatosBase
  {
    TextBox txtNombre, txtMatricula, txtUbicacion, txtEquipamiento;
    NumericUpDown numDuracion;
    ComboBox cboTipo, cboEspecialidad;

    // Formulario lateral de la pestaña elegida.
    Control Formulario()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(380) };
      t.Controls.Add(Ui.Subtitulo("Nuevo · " + nombres[actual]));
      if (actual == PestanaBase.Especialidades) Especialidad(t);
      else if (actual == PestanaBase.Profesionales) Profesional(t);
      else Lugar(t);
      t.Controls.Add(Ui.Botones(Ui.Boton("Guardar", TipoBoton.Primario,
        (s, e) => Alta()), BotonBlanquear()));
      return t;
    }

    void Especialidad(Columna t)
    {
      txtNombre = Ui.Caja("Nombre");
      numDuracion = Ui.Numero("Duracion", 5, 120, 15);
      t.Controls.Add(NuevoCampo("nombre", "Nombre", txtNombre, true));
      t.Controls.Add(NuevoCampo("duracion", "Duración mínima del turno " +
        "(min)", numDuracion, true));
      t.Controls.Add(Ui.Nota("Mínimos relevados: 15 general, 25 " +
        "fisio-kinesiología, 30 salud mental."));
    }

    void Profesional(Columna t)
    {
      txtNombre = Ui.Caja("Nombre");
      txtMatricula = Ui.Caja("Matricula");
      cboTipo = Ui.Combo("Tipo", new[] { "médico", "técnico" });
      cboEspecialidad = Ui.Combo("Especialidad",
        DatosBase.ListaEspecialidades());
      t.Controls.Add(NuevoCampo("nombre", "Apellido y nombre", txtNombre,
        true));
      t.Controls.Add(NuevoCampo("matricula", "Matrícula", txtMatricula,
        true));
      t.Controls.Add(NuevoCampo("tipo", "Tipo", cboTipo, true));
      Campo esp = NuevoCampo("especialidad", "Especialidad",
        cboEspecialidad);
      t.Controls.Add(esp);
      cboTipo.SelectedIndexChanged += (s, e) => { esp.Visible =
        cboTipo.Text == "médico"; Contenido.PerformLayout(); };
    }

    void Lugar(Columna t)
    {
      bool sala = actual == PestanaBase.Salas;
      txtNombre = Ui.Caja("Nombre");
      txtUbicacion = Ui.Caja("Ubicacion");
      t.Controls.Add(NuevoCampo("nombre", sala ? "Nombre de la sala"
        : "Número de consultorio", txtNombre, true));
      t.Controls.Add(NuevoCampo("ubicacion", "Piso y ubicación",
        txtUbicacion, true));
      if (!sala) return;
      txtEquipamiento = Ui.Caja("Equipamiento");
      t.Controls.Add(NuevoCampo("equipamiento", "Equipamiento",
        txtEquipamiento, true));
    }

    void Alta()
    {
      if (Guardar(Accion(), "Los datos se registraron correctamente."))
        Ui.Cargar(tabla, Datos(), 10);
    }

    Action Accion()
    {
      switch (actual)
      {
        case PestanaBase.Especialidades: return () =>
          DatosBase.AltaEspecialidad(txtNombre.Text, (int)numDuracion.Value);
        case PestanaBase.Profesionales: return () =>
          DatosBase.AltaProfesional(txtNombre.Text, txtMatricula.Text,
          cboTipo.Text, (cboEspecialidad.SelectedItem as Opcion)?.Id);
        case PestanaBase.Consultorios: return () => DatosBase.AltaLugar(
          "consultorio", txtNombre.Text, txtUbicacion.Text, null);
        default: return () => DatosBase.AltaLugar("sala", txtNombre.Text,
          txtUbicacion.Text, txtEquipamiento.Text);
      }
    }
  }
}
