using System.Collections.Generic;

namespace SePrice.UI
{
  static partial class Navegacion
  {
    static DefPantalla D(string codigo, string nombre, string menu,
      string cu, string circuito, System.Func<Pantalla> crear)
    {
      return new DefPantalla { Codigo = codigo, Nombre = nombre, Menu = menu,
        Cu = cu, Circuito = circuito, Crear = crear };
    }

    /// <summary>
    /// Mapa de pantallas: código, nombre (= caso de uso), proceso.
    /// </summary>
    public static readonly Dictionary<string, DefPantalla> Pantallas =
      new List<DefPantalla>
    {
      D("P-03", "Gestionar usuarios del sistema", "Usuarios y roles",
        "CU-G-16", null, () => new PUsuarios()),
      D("P-04", "Registrar especialidades, profesionales, consultorios y " +
        "salas", "Datos de base", "CU-G-17", null, () => new PDatosBase()),
      D("P-05", "Gestionar legajo de paciente", "Pacientes", "CU-G-02",
        null, () => new PLegajo()),
      D("P-06", "Asignar turno", "Turnos", "CU-G-04", null,
        () => new PTurnos()),
      D("P-07", "Asignar sobreturno", "Sobreturno", "CU-G-05", "A",
        () => new PSobreturno()),
      D("P-08", "Emitir comprobante de turno", "Comprobante", "CU-G-04",
        null, () => new PComprobante()),
      D("P-09", "Reprogramar o cancelar turno", "Reprogramar o cancelar",
        "CU-G-04", null, () => new PReprogramar()),
      D("P-10", "Acreditar paciente y registrar modalidad de pago",
        "Acreditación", "CU-G-06", null, () => new PAcreditar()),
      D("P-11", "Registrar llegada de paciente de laboratorio sin turno",
        "Laboratorio sin turno", "CU-G-05", "B", () => new PLaboratorio()),
      D("P-12", "Solicitar estudio urgente (guardia/internación)",
        "Solicitar estudio urgente", "CU-G-05", "B", () => new PUrgente()),
      D("P-13", "Atender al paciente", "Sala de espera", "CU-G-08", null,
        () => new PSalaEspera()),
      D("P-14", "Registrar atención y turno de control", "Atención",
        "CU-G-08", "A", () => new PAtencion()),
      D("P-15", "Registrar realización del estudio y consumo de insumos",
        "Estudio", "CU-G-08", "B", () => new PEstudio()),
      D("P-16", "Entregar resultado del estudio", "Entrega de resultados",
        "CU-G-09", "B", () => new PEntrega()),
      D("P-17", "Controlar insumos y reponer stock", "Insumos", "CU-G-10",
        null, () => new PInsumos()),
      D("P-18", "Generar reportes de gestión", "Reportes", "CU-G-11", null,
        () => new PReportes()),
      D("P-19", "Configurar parámetros de agenda", "Parámetros de agenda",
        "CU-G-03", "A", () => new PParametros()),
      D("P-20", "Asignar consultorio e insumos al profesional",
        "Asignar consultorio e insumos", "CU-G-12", "A",
        () => new PAsignacion()),
      D("P-21", "Publicar agenda de profesional", "Publicar agenda",
        "CU-G-03", "A", () => new PPublicarAgenda()),
      D("P-22", "Registrar ingreso y egreso del profesional",
        "Mi presentismo", "CU-G-13", "A", () => new PPresentismo()),
      D("P-23", "Liquidar honorarios por profesional", "Liquidar " +
        "honorarios", "CU-G-14", "A", () => new PHonorarios()),
      D("P-24", "Gestionar catálogo de estudios", "Catálogo de estudios",
        "CU-G-03", "B", () => new PCatalogo()),
      D("P-25", "Configurar franjas horarias de laboratorio", "Franjas de " +
        "laboratorio", "CU-G-03", "B", () => new PFranjas()),
      D("P-26", "Publicar agenda de estudios por recurso", "Agenda por " +
        "recurso", "CU-G-03", "B", () => new PAgendaRecurso()),
      D("P-27", "Auditar operaciones del sistema", "Auditoría", "CU-G-18",
        null, () => new PAuditoria()),
    }.ToDictionary();
  }

  static class Extensiones
  {
    /// <summary>Lista de pantallas → diccionario por código.</summary>
    public static Dictionary<string, DefPantalla> ToDictionary(
      this List<DefPantalla> l)
    {
      Dictionary<string, DefPantalla> d =
        new Dictionary<string, DefPantalla>();
      foreach (DefPantalla p in l) d[p.Codigo] = p;
      return d;
    }
  }
}
