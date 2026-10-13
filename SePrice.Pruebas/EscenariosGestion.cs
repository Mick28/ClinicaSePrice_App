using System;
using System.Collections.Generic;
using System.Linq;
using SePrice.Negocio;
using static SePrice.Pruebas.Prueba;

namespace SePrice.Pruebas
{
  static partial class Escenarios
  {
    // Escena 6 · Insumos y resultados (dacosta y lgomez).
    static void Escena6Insumos()
    {
      Escena("6 · Insumos y resultados");
      BaseNueva();
      Ingresar("dacosta");
      Si(Insumos.BajoMinimo() == 3, "3 insumos bajo mínimo");
      int pedido = Insumos.ArmarPropuesto();
      int id;
      List<int[]> det = Insumos.Propuesto(out id);
      Si(pedido > 0 && det.Count == 3, "pedido propuesto con 3 insumos");
      Insumos.Enviar(pedido, 1, det.ToDictionary(d => d[0], d => d[1]));
      Si(Insumos.SinRecibir() == 2, "2 pedidos enviados sin recibir");
      Insumos.RegistrarRecepcion(1);
      Si(Insumos.SinRecibir() == 1, "recepción registrada");
      Escena6Consumo();
    }

    // Consumo en consultorio: alerta al bajar del mínimo.
    static void Escena6Consumo()
    {
      Ingresar("lgomez");
      Si(Insumos.RegistrarConsumo(1, 6, 60) != null,
        "consumo de bajalenguas dispara la alerta");
      Si(Insumos.RegistrarConsumo(1, 4, 1) == null, "consumo sin alerta");
      Escena6Resultados();
    }

    static void Escena6Resultados()
    {
      Si(Resultados.Pendientes().Rows.Count > 20 && Resultados
        .EntregasRecientes().Rows.Count == 12, "pendientes de entrega y " +
        "últimas entregas a la vista");
      List<ResultadoPendiente> r = Resultados.Buscar("r-3375");
      Si(r.Count == 1, "busca por comprobante");
      Resultados.Entregar(r[0], "El/la paciente", null);
      ResultadoPendiente nolisto = Resultados.Buscar("R-3381")[0];
      Rechaza(() => Resultados.Entregar(nolisto, "Raúl", null),
        "estará listo", "resultado todavía no disponible");
    }

    // Escena 7 · Gestión (gfunes).
    static void Escena7Gestion()
    {
      Escena("7 · Gestión");
      BaseNueva();
      Ingresar("gfunes");
      DateTime d1 = new DateTime(2026, 9, 1), d2 = new DateTime(2026, 9, 30);
      Reporte rep = Reportes.Generar(d1, d2, null);
      Si(rep.Atendidos == 253 && rep.PorRubro.Count > 3 && rep.Cancelados >
        0 && rep.OcupacionSalas > 0, "reporte de septiembre: 253 atendidos" +
        " o realizados, con cancelaciones y ocupación de salas");
      Rechaza(() => Reportes.Generar(new DateTime(2026, 8, 1),
        new DateTime(2026, 8, 7), null), "No hay movimientos",
        "período sin datos");
      Escena7Honorarios(d1, d2);
    }

    // Honorarios: observadas, valor faltante y cierre.
    static void Escena7Honorarios(DateTime d1, DateTime d2)
    {
      Liquidacion l = Honorarios.Calcular(1, d1, d2);
      Si(l.Atenciones.Count(a => a.Observada) == 1, "1 atención observada");
      Rechaza(() => Honorarios.Cerrar(1, d1, d2), "no tiene valor",
        "valor sin definir");
      Honorarios.FijarValor(2, "particular", 24000);
      Si(Honorarios.Cerrar(1, d1, d2) > 0, "liquidación cerrada");
      Si(Honorarios.Calcular(1, d1, d2).Atenciones.All(a => a.Observada),
        "sólo queda la observada, sin doble liquidación");
      Rechaza(() => Sesion.Exigir("CU-G-16"), "no corresponde",
        "la jefatura no gestiona usuarios");
    }

    // Permisos: la matriz generada coincide con el cuaderno (5.3).
    static void Permisos53()
    {
      Escena("Matriz de permisos (5.3)");
      Si(Permisos.Ejecuta(Perfil.Administrador, "CU-G-17") &&
        Permisos.Audita(Perfil.Administrador, "CU-G-14") &&
        !Permisos.Ejecuta(Perfil.Administrador, "CU-G-14"),
        "Admin: configura y audita, no liquida");
      Si(Permisos.Ejecuta(Perfil.Medico, "CU-G-13") &&
        !Permisos.Ejecuta(Perfil.Tecnico, "CU-G-13"),
        "el técnico no tiene presentismo");
      Si(Permisos.Auditables(Perfil.Administrador).Count() == 11,
        "el Admin audita 11 procesos");
    }
  }
}
