using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PUsuarios
  {
    // Panel lateral «Nuevo usuario» (frm_usuario).
    Control Formulario()
    {
      Tarjeta t = new Tarjeta { MaximumSize = Ui.Tope(380) };
      t.Controls.Add(Ui.Subtitulo("Nuevo usuario"));
      t.Controls.Add(NuevoCampo("perfil", "Perfil", cboPerfil, true));
      campoNombre = NuevoCampo("nombre", "Nombre y apellido", txtNombre,
        true);
      campoPersona = NuevoCampo("persona", "Profesional", cboPersona, true);
      campoPersona.Visible = false;
      t.Controls.Add(campoNombre);
      t.Controls.Add(campoPersona);
      Acceso(t);
      t.Controls.Add(Ui.Botones(Ui.Boton("Guardar", TipoBoton.Primario,
        (s, e) => Alta()), BotonBlanquear()));
      cboPerfil.SelectedIndexChanged += (s, e) => CambioDePerfil();
      return t;
    }

    // Usuario y contraseña inicial (se repite para confirmarla).
    void Acceso(Tarjeta t)
    {
      t.Controls.Add(NuevoCampo("usuario", "Nombre de usuario", txtUsuario,
        true));
      t.Controls.Add(NuevoCampo("clave", "Contraseña (mínimo 8)", txtClave,
        true));
      t.Controls.Add(NuevoCampo("repetir", "Repetir contraseña", txtRepetir,
        true));
    }

    Perfil PerfilElegido
    {
      get { return Permisos.DeRol(cboPerfil.Text); }
    }

    // Médicos y técnicos se eligen del padrón de profesionales (CU-B-18).
    void CambioDePerfil()
    {
      if (Ui.Elegido(cboPerfil) == null) return;
      bool prof = Usuarios.EsProfesional(PerfilElegido);
      campoNombre.Visible = !prof;
      campoPersona.Visible = prof;
      cboPersona.Items.Clear();
      if (prof) foreach (Opcion o in Usuarios.ProfesionalesSinUsuario(
        PerfilElegido)) cboPersona.Items.Add(o);
      Contenido.PerformLayout();
    }

    void Alta()
    {
      if (!Guardar(() => Usuarios.Alta(DelFormulario()), "Usuario " +
        txtUsuario.Text + " creado. Ya puede iniciar sesión.")) return;
      Blanquear();
      Buscar();
    }

    DatosUsuario DelFormulario()
    {
      Opcion persona = cboPersona.SelectedItem as Opcion;
      return new DatosUsuario { Nombre = txtNombre.Text,
        NombreUsuario = txtUsuario.Text, Clave = txtClave.Text,
        Repetir = txtRepetir.Text, Perfil = PerfilElegido,
        IdProfesional = persona == null ? (int?)null : persona.Id };
    }

    // ---------- acciones sobre la fila elegida ----------

    Usuario Elegido()
    {
      int? id = Ui.Elegido(tblUsuarios);
      if (id == null) Dialogo.Error("Elegí un usuario de la tabla.");
      return id == null ? null : Usuarios.PorId(id.Value);
    }

    void Baja()
    {
      Usuario u = Elegido();
      if (u == null || !Dialogo.Confirmar(u.Nombre + " no va a poder " +
        "ingresar al sistema. ¿Dar de baja?", "Dar de baja", "Dar de baja",
        true)) return;
      if (Intentar(() => Usuarios.DarDeBaja(u.Id))) Buscar();
    }

    void Reactivar()
    {
      Usuario u = Elegido();
      if (u != null && Intentar(() => Usuarios.Reactivar(u.Id))) Buscar();
    }

    void CambiarPerfil()
    {
      Usuario u = Elegido();
      if (u == null) return;
      string[] mismos = Permisos.Nombres.Where(p => Usuarios.EsProfesional(
        p.Key) == Usuarios.EsProfesional(u.Perfil)).Select(p => p.Value)
        .ToArray();
      string nuevo = Dialogo.Ingresar("Perfil nuevo para " + u.Nombre +
        ".", "Cambiar perfil", mismos);
      if (nuevo != null && Intentar(() => Usuarios.CambiarPerfil(u.Id,
        Permisos.DeRol(nuevo)))) Buscar();
    }

    void Restablecer()
    {
      Usuario u = Elegido();
      string c = u == null ? null : Dialogo.Ingresar("Contraseña " +
        "provisoria para " + u.NombreUsuario + " (la cambia en su próximo " +
        "ingreso).", "Restablecer contraseña", null, true);
      if (c != null && Intentar(() => Usuarios.Restablecer(u.Id, c, c)))
        Dialogo.Informar("Contraseña provisoria asignada.");
    }
  }
}
