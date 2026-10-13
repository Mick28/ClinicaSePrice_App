using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-03 Gestionar usuarios del sistema (CU-G-16): sólo el Administrador/a.
  /// Alta con perfil, baja lógica, reactivación y contraseña provisoria.
  /// </summary>
  partial class PUsuarios : Pantalla
  {
    readonly TextBox txtBuscar = Ui.Caja("Buscar");
    readonly ComboBox cboEstado = Ui.Combo("Estado", new[] { "Todos",
      "habilitado", "deshabilitado" }, false);
    readonly TextBox txtNombre = Ui.Caja("Nombre");
    readonly TextBox txtUsuario = Ui.Caja("Usuario");
    readonly TextBox txtClave = Ui.Caja("Clave", true);
    readonly TextBox txtRepetir = Ui.Caja("Repetir", true);
    readonly ComboBox cboPerfil = Ui.Combo("Perfil",
      Permisos.Nombres.Values);
    readonly ComboBox cboPersona = Ui.Combo("Persona");
    Campo campoNombre, campoPersona;
    DataGridView tblUsuarios;

    protected override void ConstruirNormal()
    {
      cboEstado.SelectedIndex = 0;
      Contenido.Controls.Add(Filtros());
      Contenido.Controls.Add(Ui.EnFila(Lista(), Formulario()));
      Buscar();
    }

    Control Filtros()
    {
      Campo b = new Campo("Buscar por nombre o usuario", txtBuscar, false);
      Campo e = new Campo("Estado", cboEstado, false);
      e.MaximumSize = Ui.Tope(220);
      Button buscar = Ui.Boton("Buscar", TipoBoton.Secundario, (s, a) =>
        Buscar());
      return Ui.EnFila(b, e, Abajo(buscar));
    }

    static Control Abajo(Button b)
    {
      FlowLayoutPanel f = Ui.BotonesIzq(b);
      f.Padding = new Padding(0, 24, 0, 0);
      f.Height = 68;
      f.MaximumSize = Ui.Tope(b.Width + 8);
      return f;
    }

    Control Lista()
    {
      Tarjeta t = new Tarjeta();
      tblUsuarios = Ui.Tabla("Usuarios", null, 10);
      t.Controls.Add(tblUsuarios);
      t.Controls.Add(Ui.BotonesIzq(
        Ui.Boton("Cambiar perfil", TipoBoton.Secundario, (s, e) =>
          CambiarPerfil()),
        Ui.Boton("Restablecer contraseña", TipoBoton.Secundario, (s, e) =>
          Restablecer()),
        Ui.Boton("Reactivar", TipoBoton.Secundario, (s, e) => Reactivar()),
        Ui.Boton("Dar de baja", TipoBoton.Peligro, (s, e) => Baja())));
      t.Controls.Add(Ui.Nota("Un usuario no se borra: se da de baja y " +
        "conserva su historial de sesiones. Nadie puede darse de baja a sí " +
        "mismo."));
      return t;
    }

    void Buscar()
    {
      string estado = cboEstado.SelectedIndex <= 0 ? null
        : cboEstado.Text;
      var filas = Usuarios.Listar(txtBuscar.Text, estado).Select(u => new
      { u.Id, Usuario = u.NombreUsuario, Nombre = u.Nombre, Perfil = u.Rol,
        Estado = u.Estado, Último = Formato.Mostrar(u.UltimoIngreso) });
      Ui.Cargar(tblUsuarios, Tablas.De(filas), 10);
    }
  }
}
