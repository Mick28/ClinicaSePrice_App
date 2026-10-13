using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Base de todas las pantallas internas: título (= caso de uso), etiqueta
  /// de circuito, contenido en columna con desplazamiento, campos con su
  /// error, y el patrón de formularios del material: confirmar
  /// («¿Los datos ingresados son correctos?») → informar.
  /// </summary>
  [DesignerCategory("Code")]
  class Pantalla : UserControl
  {
    /// <summary>Definición de la pantalla en el mapa.</summary>
    public DefPantalla Def;

    /// <summary>Abierta desde la auditoría: sólo lectura (5.3).</summary>
    public bool Auditoria;

    /// <summary>
    /// Dato que le pasa la pantalla anterior (por ejemplo, un turno).
    /// </summary>
    public object Parametro;

    /// <summary>Contenido de la pantalla, de arriba abajo.</summary>
    protected readonly Columna Contenido = new Columna
    { Padding = new Padding(32, 24, 32, 24), Espacio = 16 };

    readonly Panel desplazable = new Panel { Dock = DockStyle.Fill,
      AutoScroll = true };
    readonly Dictionary<string, Campo> campos =
      new Dictionary<string, Campo>();
    Aviso resumen;

    public Pantalla()
    {
      BackColor = Tema.Fondo;
      desplazable.Controls.Add(Contenido);
      Controls.Add(desplazable);
      desplazable.Resize += (s, e) => Ajustar();
    }

    void Ajustar()
    {
      // Se reserva el ancho de la barra vertical: así nunca aparece la
      // barra horizontal.
      Contenido.Width = desplazable.Width - SystemInformation
        .VerticalScrollBarWidth - 2;
    }

    /// <summary>Ventana principal que contiene la pantalla.</summary>
    protected FrmPrincipal Principal
    {
      get { return (FrmPrincipal)FindForm(); }
    }

    /// <summary>Arma la pantalla (normal o modo auditoría).</summary>
    public void Construir()
    {
      Contenido.Controls.Add(Encabezado());
      if (Auditoria) ConstruirAuditoria();
      else ConstruirNormal();
      Ajustar();
      TieneCambios = false;
    }

    /// <summary>Contenido de la pantalla operativa.</summary>
    protected virtual void ConstruirNormal() { }

    /// <summary>
    /// Contenido en modo auditoría: datos como etiquetas o tablas, sin
    /// ningún botón que modifique (CU-B-20).
    /// </summary>
    protected virtual void ConstruirAuditoria() { }

    /// <summary>
    /// ¿Hay datos ingresados sin confirmar? (cierre de sesión).
    /// </summary>
    public bool TieneCambios { get; protected set; }

    Control Encabezado()
    {
      Columna c = new Columna { Espacio = 4 };
      if (Auditoria) c.Controls.Add(Ui.Aviso(TipoAviso.Info, "Modo " +
        "auditoría · sólo lectura. Tus consultas quedan registradas."));
      Fila f = Ui.EnFila(Ui.Titulo(TituloVisible));
      if (Def != null && Def.Circuito != null)
      {
        Label l = Ui.Circuito(Def.Circuito);
        l.MaximumSize = Ui.Tope(l.PreferredWidth + 4);
        f.Controls.Add(l);
      }
      c.Controls.Add(f);
      return c;
    }

    /// <summary>
    /// Título que se muestra (por defecto, el de la definición).
    /// </summary>
    protected virtual string TituloVisible
    {
      get { return Def == null ? "" : Def.Nombre; }
    }

    // ---------- Campos y formularios ----------

    /// <summary>Crea un campo y lo registra con su nombre de regla.</summary>
    protected Campo NuevoCampo(string clave, string etiqueta, Control c,
      bool obligatorio = false)
    {
      Campo campo = new Campo(etiqueta, c, obligatorio);
      campos[clave] = campo;
      c.TextChanged += (s, e) => TieneCambios = true;
      return campo;
    }

    /// <summary>
    /// Olvida los campos registrados (al cambiar de pestaña).
    /// </summary>
    protected void OlvidarCampos()
    {
      campos.Clear();
    }

    /// <summary>Vacía todos los campos (botón «Blanquear», 4.2).</summary>
    protected virtual void Blanquear()
    {
      foreach (Campo c in campos.Values) c.Blanquear();
      OcultarResumen();
      TieneCambios = false;
    }

    /// <summary>Botón «Blanquear» estándar.</summary>
    protected Button BotonBlanquear()
    {
      return Ui.Boton("Blanquear", TipoBoton.Secundario, (s, e) =>
        Blanquear());
    }

    /// <summary>
    /// Patrón de ingreso de datos (4.2): controla los obligatorios, pide
    /// confirmación, ejecuta y avisa. Los errores de las reglas se muestran
    /// junto al campo o en un diálogo de error.
    /// </summary>
    protected bool Guardar(Action accion, string exito,
      bool confirmar = true)
    {
      LimpiarErrores();
      if (!Completo()) return false;
      if (confirmar && !Dialogo.Confirmar("¿Los datos ingresados son " +
        "correctos?")) return false;
      if (!Intentar(accion)) return false;
      TieneCambios = false;
      if (exito != null) Dialogo.Informar(exito);
      return true;
    }

    /// <summary>Ejecuta y muestra el error de regla, si lo hay.</summary>
    protected bool Intentar(Action accion)
    {
      try
      {
        accion();
        return true;
      }
      catch (ReglaException e)
      {
        MostrarError(e);
        return false;
      }
    }

    void MostrarError(ReglaException e)
    {
      Campo c;
      if (e.Campo != null && campos.TryGetValue(e.Campo, out c))
      {
        c.MostrarError(e.Message);
        Resumen("No se pudo completar la operación. Revisá los " +
          "campos marcados.");
      }
      else Dialogo.Error(e.Message);
    }

    bool Completo()
    {
      List<Campo> vacios = campos.Values.Where(c => c.Obligatorio &&
        c.Visible && c.Vacio).ToList();
      foreach (Campo c in vacios) c.MostrarError("Campo obligatorio.");
      if (vacios.Count > 0) Resumen("Faltan completar campos obligatorios.");
      return vacios.Count == 0;
    }

    void LimpiarErrores()
    {
      foreach (Campo c in campos.Values) c.LimpiarError();
      OcultarResumen();
    }

    void Resumen(string texto)
    {
      if (resumen == null)
      {
        resumen = Ui.Aviso(TipoAviso.Error, texto);
        Contenido.Controls.Add(resumen);
        Contenido.Controls.SetChildIndex(resumen, 1);
      }
      resumen.Mensaje = "⚠   " + texto;
      resumen.Visible = true;
    }

    void OcultarResumen()
    {
      if (resumen != null) resumen.Visible = false;
    }

    /// <summary>Tabla de sólo lectura con título (modo auditoría).</summary>
    protected void TablaDeLectura(string titulo, System.Data.DataTable t,
      string nota = null)
    {
      Tarjeta c = new Tarjeta();
      c.Controls.Add(Ui.Subtitulo(titulo));
      if (nota != null) c.Controls.Add(Ui.Nota(nota));
      c.Controls.Add(Ui.Tabla("Lectura", t, 14));
      Contenido.Controls.Add(c);
    }

    /// <summary>Barra del paciente en curso, si hay (4.3).</summary>
    protected void AgregarBarraPaciente()
    {
      Control b = PacienteEnCurso.Barra(() => { PacienteEnCurso.Actual =
        null; Ir("P-05"); });
      if (b != null) Contenido.Controls.Add(b);
    }

    /// <summary>
    /// Pantallas que trabajan sobre el paciente en curso: si no hay, se
    /// empieza por el DNI (Gestionar turnos «include» Gestionar legajo).
    /// </summary>
    protected bool ExigirPaciente()
    {
      if (PacienteEnCurso.Actual != null) return true;
      Contenido.Controls.Add(Ui.Aviso(TipoAviso.Info, "Primero buscá al " +
        "paciente por DNI: queda en curso para el turno, el comprobante y " +
        "la acreditación."));
      Contenido.Controls.Add(Ui.BotonesIzq(Ui.Boton("Buscar paciente",
        TipoBoton.Primario, (s, e) => Ir("P-05"))));
      return false;
    }

    /// <summary>
    /// Las pantallas hijas (comprobante, atención) se abren desde otra con
    /// el turno elegido; si se llega sin él, se indica desde dónde ir.
    /// </summary>
    protected bool ExigirTurno(string origen, string boton, string texto)
    {
      if (Parametro is int) return true;
      Contenido.Controls.Add(Ui.Aviso(TipoAviso.Info, texto));
      Contenido.Controls.Add(Ui.BotonesIzq(Ui.Boton(boton,
        TipoBoton.Primario, (s, e) => Ir(origen))));
      return false;
    }

    /// <summary>Abre otra pantalla de la aplicación.</summary>
    protected void Ir(string codigo, object parametro = null)
    {
      Principal.Abrir(codigo, parametro);
    }
  }
}
