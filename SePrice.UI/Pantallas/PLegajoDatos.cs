using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PLegajo
  {
    TextBox txtNombre, txtTelefono, txtCorreo, txtAfiliado;
    DateTimePicker dtpNacimiento;
    ComboBox cboObraSocial;

    // Tarjeta del legajo encontrado, con sus acciones y su historial.
    void Mostrar(Paciente p)
    {
      Limpiar();
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Legajo N.° " + Formato.Dni(p.Legajo) +
        " · " + p.Nombre));
      t.Controls.Add(Ui.EnFila(Dato("DNI", Formato.Dni(p.Dni)), Dato(
        "Nacimiento y edad", Formato.Mostrar(p.Nacimiento) + " · " + p.Edad +
        " años"), Dato("Teléfono", p.Telefono), Dato("Correo", p.Correo)));
      t.Controls.Add(Ui.EnFila(Dato("Obra social", p.ObraSocial ??
        "Sin cobertura (particular)"), Dato("Plan", p.Plan), Dato(
        "Afiliado", p.Afiliado)));
      t.Controls.Add(Acciones(p));
      resultado.Controls.Add(t);
      Historial(p);
    }

    Control Acciones(Paciente p)
    {
      return Ui.Botones(Ui.Boton("Dar turno", TipoBoton.Primario,
        (s, e) => Ir("P-06")), Ui.Boton("Acreditar", TipoBoton.Secundario,
        (s, e) => Ir("P-10")), Ui.Boton("Editar datos", TipoBoton.Secundario,
        (s, e) => Formulario(p)));
    }

    static Campo Dato(string etiqueta, string valor)
    {
      return new Campo(etiqueta, Ui.Fijo(etiqueta, valor), false);
    }

    void Historial(Paciente p)
    {
      Tarjeta h = new Tarjeta();
      h.Controls.Add(Ui.Subtitulo("Historial de turnos"));
      h.Controls.Add(Ui.Tabla("Historial", Pacientes.Historial(p.Id), 8));
      resultado.Controls.Add(h);
    }

    // Alta o edición (el DNI viene de la búsqueda y no se cambia).
    void Formulario(Paciente p)
    {
      Limpiar();
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo(p.Id == 0 ? "Nuevo legajo" : "Editar " +
        "legajo N.° " + Formato.Dni(p.Legajo)));
      Campos(p);
      Filas(t, p);
      t.Controls.Add(Ui.Botones(Ui.Boton("Guardar", TipoBoton.Primario,
        (s, e) => GuardarLegajo(p)), BotonBlanquear()));
      resultado.Controls.Add(t);
    }

    void Filas(Tarjeta t, Paciente p)
    {
      t.Controls.Add(Ui.EnFila(Dato("DNI", Formato.Dni(p.Dni)),
        NuevoCampo("nombre", "Apellido y nombre", txtNombre, true),
        NuevoCampo("nacimiento", "Fecha de nacimiento", dtpNacimiento,
        true)));
      t.Controls.Add(Ui.EnFila(NuevoCampo("telefono", "Teléfono",
        txtTelefono, true), NuevoCampo("correo", "Correo electrónico",
        txtCorreo)));
      t.Controls.Add(Ui.EnFila(NuevoCampo("obra", "Obra social",
        cboObraSocial, true), NuevoCampo("afiliado", "Número de afiliado",
        txtAfiliado)));
    }

    void Campos(Paciente p)
    {
      txtNombre = Ui.Caja("Nombre");
      txtTelefono = Ui.Caja("Telefono");
      txtCorreo = Ui.Caja("Correo");
      txtAfiliado = Ui.Caja("Afiliado");
      dtpNacimiento = Ui.Fecha("Nacimiento", p.Nacimiento == null
        ? new DateTime(1980, 1, 1) : Formato.Leer(p.Nacimiento));
      cboObraSocial = Ui.Combo("ObraSocial", Pacientes.ObrasSociales());
      txtNombre.Text = p.Nombre;
      txtTelefono.Text = p.Telefono;
      txtCorreo.Text = p.Correo;
      txtAfiliado.Text = p.Afiliado;
      Opcion os = cboObraSocial.Items.OfType<Opcion>().FirstOrDefault(o =>
        o.Id == (p.Id == 0 ? -1 : p.IdObraSocial ?? 0));
      if (os != null) cboObraSocial.SelectedItem = os;
    }

    void GuardarLegajo(Paciente p)
    {
      Opcion os = cboObraSocial.SelectedItem as Opcion;
      Paciente d = new Paciente { Id = p.Id, Dni = p.Dni, Legajo = p.Legajo,
        Nombre = txtNombre.Text.Trim(), Telefono = txtTelefono.Text.Trim(),
        Nacimiento = Formato.Iso(dtpNacimiento.Value),
        Correo = txtCorreo.Text, Afiliado = txtAfiliado.Text,
        IdObraSocial = os == null || os.Id == 0 ? (int?)null : os.Id };
      Paciente nuevo = null;
      bool ok = Guardar(() => { if (p.Id == 0) nuevo = Pacientes.Alta(d);
        else Pacientes.Modificar(d); }, null);
      if (!ok) return;
      PacienteEnCurso.Actual = nuevo ?? Pacientes.PorId(p.Id);
      Dialogo.Informar(nuevo != null ? "Legajo N.° " + Formato.Dni(
        nuevo.Legajo) + " creado." : "Los datos se registraron " +
        "correctamente.");
      Ir("P-05");
    }
  }
}
