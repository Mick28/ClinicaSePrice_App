using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Esqueleto de toda pantalla interna (especificación 3.1): encabezado
  /// con la sesión y «Cerrar sesión», menú lateral del perfil (sólo sus
  /// funciones) y el contenido, con «Volver al inicio».
  /// </summary>
  [DesignerCategory("Code")]
  partial class FrmPrincipal : Form
  {
    readonly Panel contenido = new Panel { Dock = DockStyle.Fill };
    readonly Panel pie = new Panel { Dock = DockStyle.Bottom, Height = 56,
      BackColor = Tema.Fondo, Padding = new Padding(32, 8, 32, 8) };
    readonly Dictionary<string, Button> items =
      new Dictionary<string, Button>();
    Pantalla actual;

    /// <summary>
    /// true si se salió con «Cerrar sesión» (vuelve al login).
    /// </summary>
    public bool CerroSesion { get; private set; }

    public FrmPrincipal()
    {
      Text = "Clínica SePrice — " + Permisos.Nombres[Sesion.Actual.Perfil];
      BackColor = Tema.Fondo;
      ClientSize = new Size(1280, 800);
      MinimumSize = new Size(1024, 700);
      StartPosition = FormStartPosition.CenterScreen;
      Font = Tema.Fuente(10f);
      pie.Controls.Add(Ui.BotonesIzq(Ui.Boton("←  Volver al inicio",
        TipoBoton.Secundario, (s, e) => Abrir("P-01"))));
      Controls.Add(contenido);
      Controls.Add(pie);
      Controls.Add(MenuLateral());
      Controls.Add(Encabezado());
      FormClosing += (s, e) => { if (!CerroSesion) Sesion.Cerrar(); };
      Abrir("P-01");
    }

    /// <summary>
    /// Abre una pantalla. Si el perfil no puede verla, muestra «Acceso no
    /// permitido» y vuelve a su Inicio (3.5.6, regla 5).
    /// </summary>
    public void Abrir(string codigo, object parametro = null,
      bool auditoria = false)
    {
      Perfil p = Sesion.Actual.Perfil;
      if (!Navegacion.Permitida(p, codigo, auditoria))
      {
        codigo = "P-01";
        parametro = PInicio.AccesoDenegado;
        auditoria = false;
      }
      Mostrar(Crear(codigo, parametro, auditoria));
      if (auditoria) Auditar.AbrirModulo(actual.Def.Cu, actual.Def.Nombre);
    }

    Pantalla Crear(string codigo, object parametro, bool auditoria)
    {
      Pantalla nueva = codigo == "P-01" ? new PInicio()
        : Navegacion.Pantallas[codigo].Crear();
      if (codigo != "P-01") nueva.Def = Navegacion.Pantallas[codigo];
      nueva.Parametro = parametro;
      nueva.Auditoria = auditoria;
      nueva.Dock = DockStyle.Fill;
      return nueva;
    }

    void Mostrar(Pantalla nueva)
    {
      SuspendLayout();
      if (actual != null) { contenido.Controls.Remove(actual);
        actual.Dispose(); }
      actual = nueva;
      contenido.Controls.Add(nueva);
      nueva.Construir();
      pie.Visible = nueva.Def != null;
      Activar(nueva.Def == null ? "P-01" : nueva.Def.Codigo,
        nueva.Auditoria);
      ResumeLayout();
    }

    /// <summary>
    /// «Cerrar sesión» (botón del encabezado, en todas las pantallas): la
    /// misma ventana para todos los perfiles; si hay datos sin guardar, lo
    /// avisa. Llama al procedimiento único <see cref="Sesion.Cerrar"/>
    /// (CU-G-15) y vuelve al inicio de sesión.
    /// </summary>
    void CerrarSesion()
    {
      string conCambios = actual != null && actual.TieneCambios
        ? (actual.Def != null ? actual.Def.Nombre : "Inicio") : null;
      if (!DlgCerrarSesion.Confirmar(conCambios)) return;
      Sesion.Cerrar();
      CerroSesion = true;
      Close();
    }
  }
}
