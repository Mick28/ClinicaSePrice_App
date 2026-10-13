using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-23 Liquidar honorarios por profesional (CU-G-14, CU-A-18): calcula
  /// las atenciones del período sin liquidar; las observadas no suman; la
  /// jefatura carga los valores de honorarios (dato financiero).
  /// </summary>
  partial class PHonorarios : Pantalla
  {
    readonly ComboBox cboProfesional = Ui.Combo("Profesional",
      DatosBase.ListaProfesionales("médico"));
    readonly DateTimePicker dtpDesde = Ui.Fecha("Desde",
      new DateTime(2026, 9, 1));
    readonly DateTimePicker dtpHasta = Ui.Fecha("Hasta",
      new DateTime(2026, 9, 30));
    readonly Columna detalle = new Columna { Espacio = 12 };

    protected override void ConstruirAuditoria()
    {
      TablaDeLectura("Liquidaciones emitidas", Honorarios.Liquidaciones());
      TablaDeLectura("Valores de honorarios", Honorarios.Valores(),
        "Valores de ejemplo, a confirmar con el cliente.");
    }

    protected override void ConstruirNormal()
    {
      Pestanas p = new Pestanas { AlElegir = Elegir };
      p.Agregar("Liquidar");
      p.Agregar("Valores de honorarios");
      p.Agregar("Liquidaciones emitidas");
      Contenido.Controls.Add(p);
      p.Iniciar();
    }

    void Elegir(string nombre, Columna area)
    {
      OlvidarCampos();
      if (nombre == "Liquidar") Liquidar(area);
      else if (nombre.StartsWith("Valores")) Valores(area);
      else area.Controls.Add(Ui.Tabla("Liquidaciones",
        Honorarios.Liquidaciones(), 12));
    }

    void Liquidar(Columna area)
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.EnFila(NuevoCampo("profesional", "Profesional",
        cboProfesional, true), NuevoCampo("periodo", "Desde", dtpDesde,
        true), new Campo("Hasta *", dtpHasta, true), Ui.Botones(Ui.Boton(
        "Calcular", TipoBoton.Secundario, (s, e) => Calcular()))));
      area.Controls.Add(t);
      detalle.Controls.Clear();
      area.Controls.Add(detalle);
    }

    int Profesional
    {
      get { return (cboProfesional.SelectedItem as Opcion)?.Id ?? 0; }
    }

    void Calcular()
    {
      detalle.Controls.Clear();
      Liquidacion l = null;
      if (Profesional == 0) { Dialogo.Error("Elegí el profesional."); return; }
      if (!Intentar(() => l = Honorarios.Calcular(Profesional,
        dtpDesde.Value, dtpHasta.Value))) return;
      detalle.Controls.Add(Ui.Tabla("Atenciones", Tablas.De(l.Atenciones
        .Select(a => new { Fecha = Formato.Mostrar(a.Fecha), a.Paciente,
        Prestación = a.Prestacion, Modalidad = a.Modalidad, Valor =
        a.Valor.HasValue ? Formato.Pesos(a.Valor.Value) : "sin valor",
        Estado = a.Observada ? "Validación pendiente: se liquida cuando " +
        "se regularice" : "" })), 14));
      Pie(l);
    }

    void Pie(Liquidacion l)
    {
      foreach (string s in l.SinValor) detalle.Controls.Add(Ui.Aviso(
        TipoAviso.Error, "La prestación " + s + " no tiene valor de " +
        "honorario. Cargalo en Valores de honorarios."));
      detalle.Controls.Add(Ui.Subtitulo("Total a liquidar " +
        Formato.Pesos(l.Total)));
      detalle.Controls.Add(Ui.Pildora("Valores de ejemplo, a confirmar con " +
        "el cliente", Tema.Advertencia));
      detalle.Controls.Add(Ui.Botones(Ui.Boton("Cerrar liquidación",
        TipoBoton.Primario, (s, e) => Cerrar())));
      Contenido.PerformLayout();
    }

    void Cerrar()
    {
      int id = 0;
      if (Guardar(() => id = Honorarios.Cerrar(Profesional, dtpDesde.Value,
        dtpHasta.Value), null))
      {
        Dialogo.Informar("Liquidación N.° " + id + " cerrada. Esas " +
          "atenciones ya no se vuelven a liquidar.");
        detalle.Controls.Clear();
      }
    }
  }
}
