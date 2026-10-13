using System;
using SePrice.Negocio;
using static SePrice.Pruebas.Prueba;

namespace SePrice.Pruebas
{
  static partial class Escenarios
  {
    // Escena 1 · Puesta en marcha (cmendez y gfunes).
    static void Escena1PuestaEnMarcha()
    {
      Escena("1 · Puesta en marcha");
      BaseNueva();
      Ingresar("cmendez");
      Si(DatosBase.Pestanas(Perfil.Coordinacion).Count == 1,
        "la coordinación sólo ve la pestaña Especialidades");
      Rechaza(() => DatosBase.AltaEspecialidad("Salud mental infantil", 20),
        "30 minutos", "duración menor al mínimo de salud mental");
      Si(DatosBase.AltaEspecialidad("Dermatología", 15) > 0,
        "alta de especialidad");
      Rechaza(() => DatosBase.AltaLugar("sala", "Rayos 2", "PB", "Rx"),
        "no corresponde", "la coordinación no da de alta salas");
      Escena1Asignacion();
      Escena1Agenda();
      Escena1Estudios();
      Escena1Jefatura();
    }

    static void Escena1Asignacion()
    {
      DatosAsignacion d = new DatosAsignacion { IdProfesional = 3,
        IdConsultorio = 1, DiaSemana = "jueves", HoraDesde = "10:00",
        HoraHasta = "11:00", VigenciaDesde = "2026-10-19" };
      Rechaza(() => Asignaciones.Asignar(d), "Ibarra",
        "consultorio ocupado informa quién lo ocupa");
      d.DiaSemana = "miércoles";
      Si(Asignaciones.Asignar(d) > 0, "asignación en otra franja");
      Asignaciones.FijarDotacion(1, 4, 12);
      Si(Asignaciones.Dotacion(1).Rows.Count == 3, "dotación actualizada");
    }

    static void Escena1Agenda()
    {
      DateTime d1 = new DateTime(2026, 10, 19), d2 = new DateTime(2026, 10, 30);
      Si(Agendas.Previsualizar(1, d1, d2).Count == 4,
        "vista previa: 4 días de la Dra. Ibarra (lun y jue)");
      Si(Agendas.Publicar(1, d1, d2) == 48, "se publican 48 turnos");
      Rechaza(() => Agendas.Previsualizar(1, d1, d2), "no tiene",
        "no se duplican días ya publicados");
      Rechaza(() => Agendas.Previsualizar(3, new DateTime(2026, 11, 2),
        new DateTime(2026, 11, 3)), "no tiene consultorio",
        "profesional sin consultorio en el período");
      Parametros.Cambiar("sobreturnos_por_hora", "1");
      Rechaza(() => Parametros.Cambiar("duracion_minima_general", "10"),
        "15 minutos", "duración general menor a 15");
    }

    static void Escena1Estudios()
    {
      DatosEstudio e = new DatosEstudio { Nombre = "Ecografía renal",
        Duracion = 20, SalaEquipo = "Ecografía 1" };
      e.Insumos[1] = 1;
      Si(Catalogo.Alta(e) > 0, "alta de estudio con insumos");
      Rechaza(() => Parametros.Cambiar("lab_con_ayuno", "11:00-07:00"),
        "posterior", "franja de laboratorio inválida");
      AgendaRecurso r = new AgendaRecurso { IdSala = 4, IdTecnico = 6,
        Sala = "Ecografía 1", HoraDesde = "08:00", HoraHasta = "12:00",
        DuracionEstudio = 20, Desde = new DateTime(2026, 10, 15),
        Hasta = new DateTime(2026, 10, 16) };
      r.Dias.Add("jueves");
      Rechaza(() => Agendas.PublicarRecurso(r), "ya tiene agenda",
        "doble reserva del recurso");
      r.Desde = r.Hasta = new DateTime(2026, 10, 22);
      Si(Agendas.PublicarRecurso(r) == 12, "agenda de sala publicada");
    }

    static void Escena1Jefatura()
    {
      Ingresar("gfunes");
      Rechaza(() => DatosBase.AltaProfesional("Ruiz, Ana", "MN 45.210",
        "médico", 2), "MN 45.210", "matrícula duplicada");
      Si(DatosBase.AltaLugar("sala", "Rayos 2", "Planta baja", "Rayos X")
        > 0, "la jefatura da de alta una sala");
      Rechaza(() => DatosBase.AltaEspecialidad("Nutrición", 15),
        "no corresponde", "la jefatura no da de alta especialidades");
    }
  }
}
