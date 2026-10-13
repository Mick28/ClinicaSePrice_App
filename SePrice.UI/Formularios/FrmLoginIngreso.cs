using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class FrmLogin
  {
    // Variantes de P-00: faltan datos, error, usuario deshabilitado.
    void Ingresar()
    {
      Avisar(null);
      if (Falta(campoUsuario, txtUsuario) | Falta(campoContrasena,
        txtContrasena))
      {
        Avisar("Completá usuario y contraseña.");
        return;
      }
      ResultadoIngreso r = Sesion.Iniciar(txtUsuario.Text,
        txtContrasena.Text);
      if (r == ResultadoIngreso.Incorrecto) Incorrecto();
      else if (r == ResultadoIngreso.Deshabilitado) Avisar("Tu usuario no " +
        "está habilitado. Contactá a la administración del sistema.");
      else if (r == ResultadoIngreso.CambiarClave && !CambiarClave())
        Sesion.Cerrar();
      else Entrar();
    }

    static bool Falta(Campo c, TextBox t)
    {
      c.LimpiarError();
      if (!string.IsNullOrWhiteSpace(t.Text)) return false;
      c.MostrarError("Campo obligatorio.");
      return true;
    }

    void Incorrecto()
    {
      Avisar("Usuario o contraseña incorrectos. Revisá los datos e " +
        "intentá de nuevo.");
      txtContrasena.Text = "";
      txtContrasena.Focus();
    }

    void Avisar(string texto)
    {
      aviso.Visible = texto != null;
      if (texto != null) aviso.Mensaje = "⚠   " + texto;
    }

    // Contraseña provisoria (CU-B-18): se reemplaza en el primer ingreso.
    static bool CambiarClave()
    {
      string nueva = Dialogo.Ingresar("Tenés una contraseña provisoria. " +
        "Ingresá una nueva (al menos 8 caracteres).", "Cambiar contraseña",
        null, true);
      if (nueva == null) return false;
      string repetir = Dialogo.Ingresar("Repetí la nueva contraseña.",
        "Cambiar contraseña", null, true);
      try { Usuarios.CambiarMiClave(nueva, repetir); return true; }
      catch (ReglaException e) { Dialogo.Error(e.Message); return false; }
    }

    // Abre el Inicio del perfil; al cerrar la sesión vuelve acá.
    void Entrar()
    {
      txtContrasena.Text = "";
      FrmPrincipal p = new FrmPrincipal();
      p.FormClosed += (s, e) => Volver(p.CerroSesion);
      Hide();
      p.Show();
    }

    void Volver(bool cerroSesion)
    {
      if (!cerroSesion) { Sesion.Cerrar(); Close(); return; }
      // Ya se confirmó en «¿Querés cerrar la sesión?»: vuelve directo al
      // inicio de sesión, sin otro mensaje.
      aviso.Visible = false;
      txtUsuario.Text = "";
      Show();
      Activate();
      txtUsuario.Focus();
    }
  }
}
