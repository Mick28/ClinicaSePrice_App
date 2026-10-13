using System;
using System.Collections.Generic;
using System.Linq;
using SePrice.Negocio;
using static SePrice.Pruebas.Prueba;

namespace SePrice.Pruebas
{
  static partial class Escenarios
  {
    // Escena 2 · Toma de turnos (lgomez).
    static void Escena2Turnos()
    {
      Escena("2 · Toma de turnos");
      BaseNueva();
      Ingresar("lgomez");
      Si(Pacientes.PorDni(41555002) == null, "DNI inexistente");
      Paciente p = Pacientes.Alta(new Paciente { Dni = 41555002,
        Nombre = "Castro, Lucía", Nacimiento = "1999-02-11",
        Telefono = "11 5555-0909" });
      Si(p.Legajo == 10331, "legajo nuevo N.° 10.331");
      Rechaza(() => Pacientes.Alta(new Paciente { Dni = 41555002,
        Nombre = "Otra", Nacimiento = "2000-01-01", Telefono = "1" }),
        "Ya existe", "legajo duplicado");
      Escena2Asignar(p);
    }

    // Franjas libres, asignación y comprobante.
    static void Escena2Asignar(Paciente p)
    {
      List<Franja> f = Turnos.Disponibles(2, 1, Reloj.Hoy);
      Si(f.Count > 0 && f.All(x => x.Inicio > Reloj.Ahora),
        "franjas libres de Cardiología, sólo futuras");
      Si(!f.Any(x => Formato.Hora(x.Inicio) == "09:00" && x.Inicio.Date ==
        Reloj.Hoy), "la franja de 09:00 de hoy está ocupada");
      int t = Turnos.Asignar(p.Id, f[0], "telefónica");
      Si(Turnos.Comprobante(t).Numero == t, "comprobante del turno");
      Escena2Sobreturno(p);
      Escena2Estudio(p, t);
    }

    static void Escena2Sobreturno(Paciente p)
    {
      int ag = Turnos.AgendasDelDia(2, Reloj.Hoy)[0].Id;
      Si(!Turnos.HorasConCupo(ag).Contains("10:00"),
        "10:00 sin cupo de sobreturno");
      Rechaza(() => Turnos.AsignarSobreturno(p.Id, ag, "10:00", "personal"),
        "1 sobreturno por hora", "cupo agotado");
      int s = Turnos.AsignarSobreturno(p.Id, ag, "11:00", "personal");
      Si(Turnos.Comprobante(s).Sobreturno, "comprobante con marca Sobreturno");
    }

    static void Escena2Estudio(Paciente p, int turnoA)
    {
      List<Franja> f = Turnos.DisponiblesEstudio(1, Reloj.Hoy);
      Si(f.Count > 0 && f.All(x => x.Inicio.Hour < 11),
        "ecografía con ayuno: sólo franjas de 07:00 a 11:00");
      Rechaza(() => Turnos.DisponiblesEstudio(4, Reloj.Hoy), "orden de " +
        "llegada", "laboratorio no usa turnos");
      int t = Turnos.Asignar(p.Id, f[0], "personal");
      Si(Turnos.Comprobante(t).Indicaciones.Contains("ayuno"),
        "comprobante con la indicación de ayuno");
      List<Franja> otra = Turnos.Disponibles(2, 1, Reloj.Hoy);
      Turnos.Reprogramar(turnoA, otra[1]);
      Si(Turnos.Comprobante(turnoA).FechaHora == otra[1].Inicio,
        "turno reprogramado");
      Turnos.Cancelar(t, "Pedido del paciente");
      Si(Turnos.DelPaciente(p.Id).Rows.Count == 2, "quedan 2 vigentes");
    }

    // Escena 3 · Inicio de jornada (sibarra; luego lgomez).
    static void Escena3Jornada()
    {
      Escena("3 · Inicio de jornada");
      BaseNueva("08:50");
      Ingresar("sibarra");
      Rechaza(() => Atencion.SalaDeEspera(), "registrá tu ingreso",
        "sala de espera bloqueada sin ingreso");
      Si(Presentismo.RegistrarIngreso() == null, "ingreso en horario");
      Si(Atencion.SalaDeEspera().Count == 3, "3 pacientes acreditados");
      Reloj.Fijar(Reloj.Hoy.AddHours(10).AddMinutes(20));
      Ingresar("lgomez");
      Si(Turnos.Afectados().Rows.Count == 3,
        "Lic. Bello sin ingreso: 3 turnos afectados");
      Si(Presentismo.AusentesHoy().Contains("Bello"), "agenda ausente");
    }
  }
}
