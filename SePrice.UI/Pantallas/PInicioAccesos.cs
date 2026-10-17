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
      { "P-06", "Consultar disponibilidad y reserva" },
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

    // Título de cada tarjeta de acceso (si falta, se usa el del menú).
    static readonly Dictionary<string, string> titulos =
        new Dictionary<string, string>
        {
      { "P-03", "Gestionar usuarios" },
      { "P-27", "Auditar operaciones" },
      { "P-04", "Registrar datos de base" },
      { "P-19", "Configurar parámetros de agenda" },
      { "P-20", "Asignar consultorio e insumos" },
      { "P-21", "Publicar agenda" },
      { "P-24", "Gestionar catálogo de estudios" },
      { "P-25", "Configurar franjas de laboratorio" },
      { "P-26", "Publicar agenda por recurso" },
      { "P-05", "Gestionar pacientes" },
      { "P-06", "Asignar turnos" },
      { "P-10", "Acreditar paciente" },
      { "P-17", "Controlar insumos" },
      { "P-07", "Asignar sobreturno" },
      { "P-11", "Registrar laboratorio sin turno" },
      { "P-16", "Entregar resultados" },
      { "P-13", "Atender pacientes" },
      { "P-22", "Registrar ingreso y egreso" },
      { "P-12", "Solicitar estudio urgente" },
      { "P-18", "Generar reportes" },
      { "P-23", "Liquidar honorarios" },
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
      Parrafo titulo = Ui.Subtitulo("●  " + (c == null ? "General" : c == "A"
        ? "Consultorios externos" : "Estudios clínicos"));
      titulo.ForeColor = c == null ? Tema.Texto : Tema.DeCircuito(c);
      titulo.Font = Tema.Fuente(11f, FontStyle.Bold);
      t.Controls.Add(titulo);
      foreach (ItemMenu i in items) t.Controls.Add(Acceso(i));
      return t;
    }

    // Tarjeta de acceso: título en negrita y descripción en gris.
    Control Acceso(ItemMenu i)
    {
        string cu = Navegacion.Pantallas[i.Codigo].Cu;
        string codigo = Configuracion.MostrarCodigos ? cu + " · " : "";
        Tarjeta t = new Tarjeta
        {
            Espacio = 2,
            Cursor = Cursors.Hand,
            Padding = new Padding(16, 12, 16, 12)
        };
        t.Controls.Add(Linea(Titulo(i), 10.5f, FontStyle.Bold, Tema.Texto));
        t.Controls.Add(Linea(codigo + descripciones[i.Codigo], 9.5f,
            FontStyle.Regular, Tema.Texto2));
        Clic(t, i.Codigo);
        return t;
    }

    static Parrafo Linea(string texto, float puntos, FontStyle estilo,
        Color color)
    {
        return new Parrafo
        {
            Text = texto,
            Font = Tema.Fuente(puntos, estilo),
            ForeColor = color,
            Cursor = Cursors.Hand
        };
    }

    static string Titulo(ItemMenu i)
    {
        string t;
        return titulos.TryGetValue(i.Codigo, out t) ? t : i.Texto;
    }

    // Toda la tarjeta responde al clic, también sus dos textos.
    void Clic(Control t, string codigo)
    {
        t.Click += (s, e) => Ir(codigo);
        foreach (Control c in t.Controls) c.Click += (s, e) => Ir(codigo);
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
