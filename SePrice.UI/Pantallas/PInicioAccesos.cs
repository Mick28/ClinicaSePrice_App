using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PInicio
  {
    static readonly Dictionary<string, string> descripciones =
      new Dictionary<string, string>
    {
      { "P-03", "Alta, baja y perfil de usuarios" },
      { "P-27", "Registro de operaciones, sólo lectura" },
      { "P-04", "Especialidades, profesionales, consultorios y salas" },
      { "P-19", "Duración, sobreturnos y tolerancia" },
      { "P-20", "Consultorio, franja y dotación" },
      { "P-21", "Franjas de turnos por profesional" },
      { "P-24", "Estudios, ayuno e insumos" },
      { "P-25", "Horarios con y sin ayuno" },
      { "P-26", "Salas y equipos" },
      { "P-05", "Buscar por DNI o crear legajo" },
      { "P-06", "Disponibilidad y reserva" },
      { "P-10", "Identidad, pago y cobertura" },
      { "P-17", "Consumo, stock y pedidos" },
      { "P-07", "Un sobreturno por hora" },
      { "P-11", "Por orden de llegada" },
      { "P-16", "Contra comprobante de retiro" },
      { "P-13", "Pacientes acreditados, por orden" },
      { "P-22", "Ingreso y egreso de la jornada" },
      { "P-12", "Guardia e internación" },
      { "P-18", "Indicadores del período" },
      { "P-23", "Atenciones del período" },
    };

    // Accesos agrupados por sección, con la etiqueta de circuito (P-01).
    Control Accesos()
    {
      Fila f = new Fila();
      foreach (var g in Navegacion.Menu(perfil).GroupBy(i => i.Seccion))
        f.Controls.Add(Grupo(g.Key, g.ToList()));
      return f;
    }

    Control Grupo(string seccion, List<ItemMenu> items)
    {
      Tarjeta t = new Tarjeta { Espacio = 8 };
      string c = seccion == Navegacion.A ? "A" : seccion == Navegacion.B
        ? "B" : null;
      Parrafo titulo = Ui.Subtitulo("●  " + (c == null ? "Común" : c == "A"
        ? "Consultorios externos" : "Estudios clínicos"));
      titulo.ForeColor = c == null ? Tema.Texto : Tema.DeCircuito(c);
      titulo.Font = Tema.Fuente(11f, FontStyle.Bold);
      t.Controls.Add(titulo);
      foreach (ItemMenu i in items) t.Controls.Add(Acceso(i));
      return t;
    }

    Button Acceso(ItemMenu i)
    {
      string cu = Navegacion.Pantallas[i.Codigo].Cu;
      string codigo = Configuracion.MostrarCodigos ? cu + " · " : "";
      Button b = new Button { Height = 58, FlatStyle = FlatStyle.Flat,
        BackColor = Tema.Fondo, ForeColor = Tema.Texto, Cursor =
        Cursors.Hand, TextAlign = ContentAlignment.MiddleLeft,
        Font = Tema.Fuente(9.5f), Text = i.Texto + "\n" + codigo + 
        descripciones[i.Codigo] };
      b.FlatAppearance.BorderColor = Tema.Borde;
      b.Click += (s, e) => Ir(i.Codigo);
      return b;
    }

    // Admin: últimos cambios del registro de auditoría (P-27).
    void UltimosCambios()
    {
      Tarjeta t = new Tarjeta();
      t.Controls.Add(Ui.Subtitulo("Últimos cambios registrados en " +
        "Auditoría"));
      t.Controls.Add(Ui.Nota("Extracto del registro único de auditoría " +
        "(CU-G-18). Nada de esto se puede modificar."));
      t.Controls.Add(Ui.Tabla("Cambios", Auditar.Ultimas(6), 6));
      t.Controls.Add(Ui.Botones(Ui.Boton("Ver registro de auditoría " +
        "completo  →", TipoBoton.Enlace, (s, e) => Ir("P-27"))));
      Contenido.Controls.Add(t);
    }
  }
}
