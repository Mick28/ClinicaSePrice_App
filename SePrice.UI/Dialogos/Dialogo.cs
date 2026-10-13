using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SePrice.UI
{
  /// <summary>
  /// Cuadros de diálogo (especificación 2.6 y material de la Etapa 3): un
  /// solo componente con cuatro usos. Bloquean la aplicación hasta que se
  /// responden. Esc equivale a No / Cancelar.
  /// </summary>
  static class Dialogo
  {
    /// <summary>
    /// Cómo se muestra la ventana (modal). Sólo las pruebas en Linux lo
    /// reemplazan, para responder solas y guardar la captura.
    /// </summary>
    public static Func<Form, DialogResult> Mostrar = v => v.ShowDialog();

    /// <summary>Informar: «Los datos se registraron correctamente.»</summary>
    public static void Informar(string texto, string titulo = "Listo")
    {
      using (Ventana v = new Ventana(titulo, texto, "✔", Tema.Exito))
      {
        v.AgregarBoton("Aceptar", TipoBoton.Primario, DialogResult.OK);
        Mostrar(v);
      }
    }

    /// <summary>Error: qué pasó y qué hacer.</summary>
    public static void Error(string texto, string titulo = "No se pudo " +
      "completar")
    {
      using (Ventana v = new Ventana(titulo, texto, "!", Tema.Error))
      {
        v.AgregarBoton("Aceptar", TipoBoton.Primario, DialogResult.OK);
        Mostrar(v);
      }
    }

    /// <summary>
    /// Confirmar (No / Sí). Las acciones destructivas van en rojo.
    /// </summary>
    public static bool Confirmar(string texto, string titulo = "Confirmar",
      string si = "Sí", bool peligro = false, string no = "No")
    {
      using (Ventana v = new Ventana(titulo, texto, peligro ? "!" : "?",
        peligro ? Tema.Advertencia : Tema.Primario))
      {
        v.AgregarBoton(si, peligro ? TipoBoton.Peligro : TipoBoton.Primario,
          DialogResult.Yes);
        v.AgregarBoton(no, TipoBoton.Secundario, DialogResult.No);
        return Mostrar(v) == DialogResult.Yes;
      }
    }

    /// <summary>
    /// Ingresar un dato (Cancelar / Aceptar). Con opciones, muestra una
    /// lista; si no, una caja de texto. Devuelve null si se cancela.
    /// </summary>
    public static string Ingresar(string texto, string titulo,
      string[] opciones = null, bool clave = false)
    {
      using (Ventana v = new Ventana(titulo, texto, "✎", Tema.Primario))
      {
        Control c = v.AgregarEntrada(opciones, clave);
        v.AgregarBoton("Aceptar", TipoBoton.Primario, DialogResult.OK);
        v.AgregarBoton("Cancelar", TipoBoton.Secundario, DialogResult.Cancel);
        if (Mostrar(v) != DialogResult.OK) return null;
        return string.IsNullOrWhiteSpace(c.Text) ? null : c.Text.Trim();
      }
    }
  }

  /// <summary>
  /// Ventana modal del diálogo. Se ubica todo con medidas explícitas (no
  /// depende del acomodo automático), así se ve igual en cualquier equipo.
  /// </summary>
  [DesignerCategory("Code")]
  class Ventana : Form
  {
    const int Ancho = 460, Margen = 24;
    readonly Label lblTexto;
    readonly List<Button> botones = new List<Button>();
    Control entrada;

    public Ventana(string titulo, string texto, string icono, Color color)
    {
      Text = titulo;
      FormBorderStyle = FormBorderStyle.FixedDialog;
      StartPosition = FormStartPosition.CenterParent;
      MaximizeBox = MinimizeBox = false;
      ShowInTaskbar = false;
      BackColor = Tema.Superficie;
      Font = Tema.Fuente(10f);
      Controls.Add(Icono(icono, color));
      Controls.Add(Titulo(titulo));
      lblTexto = Texto(texto);
      Controls.Add(lblTexto);
      Ubicar();
    }

    static Label Icono(string icono, Color color)
    {
      return new Label { Text = icono, Location = new Point(Margen, Margen),
        Size = new Size(34, 34), TextAlign = ContentAlignment.MiddleCenter,
        ForeColor = Color.White, BackColor = color,
        Font = Tema.Fuente(13f, FontStyle.Bold) };
    }

    static Label Titulo(string titulo)
    {
      return new Label { Text = titulo, AutoEllipsis = true,
        Location = new Point(Margen + 46, Margen),
        Size = new Size(Ancho - 2 * Margen - 46, 34),
        TextAlign = ContentAlignment.MiddleLeft, ForeColor = Tema.Texto,
        Font = Tema.Fuente(12.5f, FontStyle.Bold) };
    }

    static Label Texto(string texto)
    {
      Font f = Tema.Fuente(10.5f);
      int ancho = Ancho - 2 * Margen;
      int alto = TextRenderer.MeasureText(texto, f, new Size(ancho, 0),
        TextFormatFlags.WordBreak).Height + 6;
      return new Label { Text = texto, Font = f, ForeColor = Tema.Texto,
        Location = new Point(Margen, Margen + 50), Size = new Size(ancho,
        alto) };
    }

    // Texto, entrada (si hay) y botones a la derecha; ajusta el alto.
    void Ubicar()
    {
      int y = lblTexto.Bottom + 16;
      if (entrada != null)
      {
        entrada.SetBounds(Margen, y, Ancho - 2 * Margen, entrada.Height);
        y = entrada.Bottom + 16;
      }
      int x = Ancho - Margen;
      foreach (Button b in botones)
      {
        x -= b.Width;
        b.Location = new Point(x, y);
        x -= 8;
      }
      ClientSize = new Size(Ancho, y + 38 + Margen);
    }

    /// <summary>
    /// Agrega un botón (el primero agregado queda a la derecha).
    /// </summary>
    public void AgregarBoton(string texto, TipoBoton tipo, DialogResult r)
    {
      Button b = Ui.Boton(texto, tipo, null);
      b.DialogResult = r;
      botones.Add(b);
      Controls.Add(b);
      if (r == DialogResult.OK || r == DialogResult.Yes) AcceptButton = b;
      else CancelButton = b;
      if (botones.Count == 1) CancelButton = b;
      Ubicar();
    }

    /// <summary>
    /// Agrega la caja de texto o la lista para ingresar un dato.
    /// </summary>
    public Control AgregarEntrada(string[] opciones, bool clave)
    {
      entrada = opciones == null ? (Control)Ui.Caja("Dato", clave)
        : Ui.Combo("Dato", opciones, false);
      Controls.Add(entrada);
      Shown += (s, e) => entrada.Focus();
      Ubicar();
      return entrada;
    }
  }
}
