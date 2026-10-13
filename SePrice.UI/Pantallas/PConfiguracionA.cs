using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-19 Configurar parámetros de agenda (CU-A-03, CU-G-03): duración por
  /// especialidad, sobreturnos por hora y tolerancia de ingreso. Cada
  /// tarjeta muestra su última modificación (3.5.6, regla 6).
  /// </summary>
  class PParametros : Pantalla
  {
    protected override void ConstruirNormal()
    {
      Contenido.Controls.Add(Ui.Subtitulo("Duración mínima por especialidad"));
      Fila f = new Fila();
      foreach (DataRow r in DatosBase.Especialidades().Rows)
        if (Convert.ToString(r["Estado"]) == "activo") f.Controls.Add(
          Especialidad(r));
      Contenido.Controls.Add(f);
      Contenido.Controls.Add(Ui.Subtitulo("Reglas de agenda"));
      Fila p = new Fila();
      foreach (Parametro x in Parametros.DelCircuito("A").Where(x =>
        !x.Clave.StartsWith("arancel"))) p.Controls.Add(Tarjeta(x));
      Contenido.Controls.Add(p);
    }

    Control Especialidad(DataRow r)
    {
      int id = Convert.ToInt32(r["Id"]);
      string valor = Convert.ToString(r["Duración (min)"]);
      return Ui.Indicador(Convert.ToString(r["Nombre"]), valor + " min",
        "Editar ✎", Tema.Primario).Tap(t => Clic(t, () => CambiarDuracion(
        id, Convert.ToString(r["Nombre"]), valor)));
    }

    Control Tarjeta(Parametro p)
    {
      Tarjeta t = Ui.Indicador(p.Descripcion, p.Valor + " " + p.Unidad,
        "Últ. modificación: " + (p.UltimaModificacion ?? "—") +
        " · Editar ✎", Tema.Primario);
      if (p.AConfirmar) t.Controls.Add(Ui.Pildora("A confirmar con el " +
        "cliente", Tema.Advertencia));
      Clic(t, () => Cambiar(p));
      return t;
    }

    static void Clic(Control c, Action a)
    {
      c.Cursor = Cursors.Hand;
      c.Click += (s, e) => a();
      foreach (Control h in c.Controls) Clic(h, a);
    }

    void CambiarDuracion(int id, string nombre, string actual)
    {
      string v = Dialogo.Ingresar("Duración mínima del turno de " + nombre +
        " (minutos). Actual: " + actual + ".", "Editar duración");
      int n;
      if (v == null) return;
      if (!int.TryParse(v, out n)) { Dialogo.Error("Ingresá un número.");
        return; }
      if (Intentar(() => Parametros.CambiarDuracion(id, n))) Recargar();
    }

    void Cambiar(Parametro p)
    {
      string v = Dialogo.Ingresar(p.Descripcion + " (" + p.Unidad + "). " +
        "Actual: " + p.Valor + ".", "Editar parámetro");
      if (v != null && Intentar(() => Parametros.Cambiar(p.Clave, v)))
        Recargar();
    }

    void Recargar()
    {
      Dialogo.Informar("Los datos se registraron correctamente. Se " +
        "aplican a las agendas que se publiquen desde ahora.");
      Ir("P-19");
    }
  }

  static class ExtensionesControl
  {
    /// <summary>Aplica una acción al control y lo devuelve.</summary>
    public static T Tap<T>(this T c, Action<T> accion)
    {
      accion(c);
      return c;
    }
  }
}
