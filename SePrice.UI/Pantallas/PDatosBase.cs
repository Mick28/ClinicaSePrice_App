using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-04 Registrar especialidades, profesionales, consultorios y salas
  /// (CU-G-17). Administrador/a: las cuatro pestañas; Coordinación:
  /// Especialidades; Jefatura: Profesionales, Consultorios y Salas.
  /// </summary>
  partial class PDatosBase : Pantalla
  {
    static readonly Dictionary<PestanaBase, string> nombres =
      new Dictionary<PestanaBase, string>
    {
      { PestanaBase.Especialidades, "Especialidades" },
      { PestanaBase.Profesionales, "Profesionales y técnicos" },
      { PestanaBase.Consultorios, "Consultorios" },
      { PestanaBase.Salas, "Salas de estudio" },
    };

    PestanaBase actual;
    DataGridView tabla;

    protected override void ConstruirNormal()
    {
      Pestanas p = new Pestanas { AlElegir = Elegir };
      foreach (PestanaBase b in DatosBase.Pestanas(Sesion.Actual.Perfil))
        p.Agregar(nombres[b]);
      Contenido.Controls.Add(p);
      p.Iniciar();
    }

    void Elegir(string nombre, Columna area)
    {
      foreach (var n in nombres) if (n.Value == nombre) actual = n.Key;
      OlvidarCampos();
      if (actual == PestanaBase.Salas)
        area.Controls.Add(Ui.Circuito("B"));
      tabla = Ui.Tabla("Datos", Datos(), 10);
      area.Controls.Add(Ui.EnFila(Lista(), Formulario()));
    }

    System.Data.DataTable Datos()
    {
      switch (actual)
      {
        case PestanaBase.Especialidades: return DatosBase.Especialidades();
        case PestanaBase.Profesionales: return DatosBase.Profesionales();
        case PestanaBase.Consultorios: return DatosBase.Lugares("consultorio");
        default: return DatosBase.Lugares("sala");
      }
    }

    Control Lista()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(tabla);
      t.Controls.Add(Ui.BotonesIzq(Ui.Boton("Dar de baja",
        TipoBoton.Peligro, (s, e) => Baja())));
      t.Controls.Add(Ui.Nota("La baja no borra: el registro pasa a " +
        "«inactivo» y se avisan las agendas vigentes afectadas."));
      return t;
    }

    void Baja()
    {
      int? id = Ui.Elegido(tabla);
      if (id == null) { Dialogo.Error("Elegí un registro de la tabla.");
        return; }
      if (!Dialogo.Confirmar("El registro pasa a «inactivo». ¿Dar de " +
        "baja?", "Dar de baja", "Dar de baja", true)) return;
      List<string> afectado = null;
      if (!Intentar(() => afectado = DatosBase.DarDeBaja(actual, id.Value)))
        return;
      if (afectado.Count > 0) Dialogo.Informar("Quedan afectadas:\n" +
        string.Join("\n", afectado), "Dado de baja");
      Ui.Cargar(tabla, Datos(), 10);
    }
  }
}
