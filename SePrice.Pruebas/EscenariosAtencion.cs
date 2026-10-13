using System;
using System.Collections.Generic;
using System.Linq;
using SePrice.Negocio;
using static SePrice.Pruebas.Prueba;

namespace SePrice.Pruebas
{
  static partial class Escenarios
  {
    // Escena 4 · Recepción (lgomez y nrios).
    static void Escena4Recepcion()
    {
      Escena("4 · Recepción");
      BaseNueva("10:25");
      Ingresar("lgomez");
      Escena4Cobertura();
      Escena4Diferida();
      Escena4Estudios();
      Escena4Guardia();
    }

    // Cobertura válida y rechazada (continúa como particular).
    static void Escena4Cobertura()
    {
      TurnoDeHoy t = Acreditacion.TurnosDeHoy(1)[0];
      Validacion v = Acreditacion.Validar(t.IdTurno, null, false);
      Si(v.Resultado == "válida", "cobertura válida (Sanitas)");
      Si(Acreditacion.Acreditar(t.IdTurno, "obra social", v, false) == null,
        "acreditado en horario");
      t = Acreditacion.TurnosDeHoy(6)[0];
      v = Acreditacion.Validar(t.IdTurno, null, false);
      Rechaza(() => Acreditacion.Acreditar(t.IdTurno, "obra social", v,
        false), "rechazada", "cobertura rechazada");
      Si(Acreditacion.Acreditar(t.IdTurno, "particular", null, false) != null,
        "continúa como particular (con aviso de demora)");
    }

    static void Escena4Diferida()
    {
      TurnoDeHoy t = Acreditacion.TurnosDeHoy(8)[0];
      Validacion v = Acreditacion.Validar(t.IdTurno, null, false);
      Si(v.Resultado == "pendiente", "obra social sin respuesta: diferida");
      Acreditacion.Acreditar(t.IdTurno, "obra social", v, false);
      Si(Acreditacion.Pendientes().Rows.Count == 2, "2 pendientes de " +
        "regularizar (la de septiembre y la de hoy)");
    }

    static void Escena4Estudios()
    {
      Paciente p = Pacientes.PorDni(30123456);
      int turno = Turnos.Asignar(p.Id, Turnos.DisponiblesEstudio(2,
        Reloj.Hoy).First(f => f.Inicio.Date == Reloj.Hoy), "personal");
      Rechaza(() => Acreditacion.Validar(turno, null, true), "orden médica",
        "estudio sin orden médica: bloqueado");
      int orden = Acreditacion.RegistrarOrden(p.Id, new DatosOrden {
        Solicitante = "Dr. Roca", Matricula = "MN 11.111", Estudios = "Rx",
        Emision = Reloj.Hoy.AddDays(-2), Vencimiento = Reloj.Hoy.AddDays(28) });
      Validacion v = Acreditacion.Validar(turno, orden, true);
      Acreditacion.Acreditar(turno, "obra social", v, true);
      Escena4Laboratorio(p);
    }

    // Laboratorio sin turno: orden de llegada y franja horaria.
    static void Escena4Laboratorio(Paciente p)
    {
      List<Estudio> lab = Catalogo.Estudios(false);
      Si(Demanda.RegistrarLlegada(p.Id, lab.Where(e => e.Nombre ==
        "Glucemia").ToList()) == "L-001", "laboratorio: orden L-001");
      Reloj.Fijar(Reloj.Hoy.AddHours(11).AddMinutes(30));
      Rechaza(() => Demanda.RegistrarLlegada(p.Id, lab.Where(e => e.Nombre
        == "Glucemia").ToList()), "Próximo horario", "fuera de franja");
    }

    static void Escena4Guardia()
    {
      Ingresar("nrios");
      Si(Pacientes.PorDni(22418770) != null, "la guardia busca por DNI");
      Rechaza(() => Pacientes.Alta(new Paciente { Dni = 1234567 }),
        "no corresponde", "la guardia no crea legajos");
      int id = Demanda.SolicitarUrgente(new SolicitudUrgente {
        IdPaciente = 3, IdEstudio = 3, Sector = "internación",
        Prioridad = "urgente", Episodio = "Hab. 512",
        Solicitante = "Dr. Paz" });
      Si(id > 0 && Demanda.MisSolicitudes().Rows.Count == 2,
        "solicitud enviada: 2 de hoy en «Mis solicitudes»");
    }

    // Escena 5 · Atención (sibarra y tvera).
    static void Escena5Atencion()
    {
      Escena("5 · Atención");
      BaseNueva("08:55");
      Ingresar("sibarra");
      Presentismo.RegistrarIngreso();
      List<EnEspera> sala = Atencion.SalaDeEspera();
      Si(sala.Any(x => x.Etiqueta == "Sobreturno"), "sobreturno identificado");
      Atencion.Llamar(sala[0].IdTurno);
      Atencion.RegistrarAtencion(sala[0].IdTurno, true, 15);
      Si(Atencion.SalaDeEspera().Count == 2, "atendido sale de la sala");
      Ingresar("lgomez");
      Si(Turnos.ControlesPendientes().Rows.Count == 1,
        "administración ve el control pedido");
      Escena5Estudios();
    }

    // Cola de estudios: la urgencia primero; comprobante y stock.
    static void Escena5Estudios()
    {
      Ingresar("tvera");
      List<EnEspera> cola = Atencion.ColaDeEstudios();
      Si(cola.Count == 2 && cola[0].Etiqueta == "Urgencia",
        "la urgencia va primero en la cola");
      EnEspera eco = cola.First(x => x.Detalle == "Ecografía abdominal");
      Atencion.Llamar(eco.IdTurno);
      EstudioRegistrado r = Atencion.RegistrarEstudio(eco.IdTurno,
        new Dictionary<int, int> { { 1, 1 }, { 4, 1 } }, Reloj.Hoy.AddDays(2));
      Si(r.Comprobante.StartsWith("R-") && r.Alertas.Count == 1,
        "comprobante de retiro y alerta de stock de gel");
    }
  }
}
