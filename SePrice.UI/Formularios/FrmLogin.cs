using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-00 Iniciar sesión en el sistema (CU-G-01): la «puerta» del sistema.
  /// Lleva a cada usuario al Inicio de su perfil. Única pantalla sin sesión.
  /// </summary>
  [DesignerCategory("Code")]
  partial class FrmLogin : Form
  {
    readonly Tarjeta tarjeta = new Tarjeta { MaximumSize = Ui.Tope(440),
      Padding = new Padding(32, 28, 32, 24), Espacio = 14 };
    readonly TextBox txtUsuario = Ui.Caja("Usuario");
    readonly TextBox txtContrasena = Ui.Caja("Contrasena", true);
    readonly Aviso aviso = Ui.Aviso(TipoAviso.Error, "");
    Campo campoUsuario, campoContrasena;

    public FrmLogin()
    {
      Text = "Clínica SePrice — Sistema de Gestión Administrativa";
      BackColor = Tema.Fondo;
      ClientSize = new Size(1100, 760);
      StartPosition = FormStartPosition.CenterScreen;
      Font = Tema.Fuente(10f);
      Armar();
      Controls.Add(tarjeta);
      Resize += (s, e) => Centrar();
      Shown += (s, e) => { Centrar(); txtUsuario.Focus(); };
    }

    void Armar()
    {
      tarjeta.Controls.Add(Logo());
      tarjeta.Controls.Add(Centrado(Ui.Nota("Sistema de Gestión " +
        "Administrativa"), 11f));
      aviso.Visible = false;
      tarjeta.Controls.Add(aviso);
      campoUsuario = new Campo("Usuario", txtUsuario, true);
      tarjeta.Controls.Add(campoUsuario);
      tarjeta.Controls.Add(CampoContrasena());
      tarjeta.Controls.Add(BotonIngresar());
      if (Configuracion.ModoDemo) tarjeta.Controls.Add(new PanelDemo(this));
      tarjeta.Controls.Add(Centrado(Ui.Nota("¿No podés ingresar? Contactá " +
        "a la administración del sistema."), 9f));
    }

    // Botón principal; Enter también ingresa.
    Button BotonIngresar()
    {
      Button b = Ui.Boton("Ingresar  →", TipoBoton.Primario, (s, e) =>
        Ingresar());
      b.MaximumSize = Size.Empty;
      b.Height = 44;
      AcceptButton = b;
      return b;
    }

    static Control Logo()
    {
      PictureBox p = new PictureBox { Height = 60,
        SizeMode = PictureBoxSizeMode.CenterImage };
      string ruta = Path.Combine(Application.StartupPath, "logo.png");
      if (File.Exists(ruta)) p.Image = Image.FromFile(ruta);
      return p;
    }

    static Parrafo Centrado(Parrafo p, float puntos)
    {
      p.TextAlign = ContentAlignment.MiddleCenter;
      p.Font = Tema.Fuente(puntos);
      return p;
    }

    Control CampoContrasena()
    {
      Button mostrar = Ui.Boton("Mostrar", TipoBoton.Enlace, (s, e) =>
        txtContrasena.UseSystemPasswordChar =
          !txtContrasena.UseSystemPasswordChar);
      mostrar.Height = txtContrasena.Height;
      mostrar.Font = Tema.Fuente(9f, System.Drawing.FontStyle.Bold);
      Fila f = Ui.EnFila(txtContrasena, mostrar);
      f.Espacio = 4;
      campoContrasena = new Campo("Contraseña", f, true);
      return campoContrasena;
    }

    void Centrar()
    {
      tarjeta.Width = 440;
      tarjeta.Left = (ClientSize.Width - tarjeta.Width) / 2;
      tarjeta.Top = Math.Max(16, (ClientSize.Height - tarjeta.Height) / 2);
    }

    /// <summary>Completa usuario y contraseña (accesos de prueba).</summary>
    public void Autocompletar(string usuario)
    {
      txtUsuario.Text = usuario;
      txtContrasena.Text = "SePrice2026";
      txtContrasena.Focus();
    }
  }
}
