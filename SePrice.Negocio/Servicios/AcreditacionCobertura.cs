using System;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  public static partial class Acreditacion
  {
    // ---------- CU-G-07 Validar cobertura y autorización ----------

    /// <summary>
    /// Consulta a la obra social y registra la validación. En consultorios
    /// es de afiliación y puede quedar «pendiente» (modo diferido); en
    /// estudios es una autorización y exige la orden médica.
    /// </summary>
    public static Validacion Validar(int idTurno, int? idOrden, bool estudio)
    {
      Sesion.Exigir("CU-G-07");
      ReglaException.Exigir(!estudio || idOrden.HasValue, "Para acreditar " +
        "un estudio con obra social se necesita la orden médica.", "orden");
      Fila p = Consulta.Uno("SELECT pa.id_obra_social, o.nombre FROM turno" +
        " t JOIN paciente pa ON pa.id_paciente = t.id_paciente JOIN " +
        "obra_social o ON o.id_obra_social = pa.id_obra_social WHERE " +
        "t.id_turno = @p0", f => f, idTurno);
      ReglaException.Exigir(p != null, "El/la paciente no tiene obra social" +
        " en su legajo: acreditá como particular.");
      string r = ObraSocialSimulada.Consultar(p.Texto("nombre"));
      ReglaException.Exigir(!(estudio && r == "pendiente"), "La obra social" +
        " no respondió y el estudio necesita su autorización. Reintentá o " +
        "continuá como particular.");
      return Guardar(idTurno, p.Entero("id_obra_social"), idOrden, estudio,
        r);
    }

    static Validacion Guardar(int idTurno, int idOs, int? idOrden,
      bool estudio, string resultado)
    {
      string refe = resultado == "válida"
        ? (estudio ? "AUT-" : "REF-") + (5000 + idTurno * 7) : null;
      int id = Consulta.Insertar("INSERT INTO validacion_de_cobertura " +
        "(id_turno, id_obra_social, id_orden, tipo, modo, resultado, " +
        "nro_referencia, fecha_hora) VALUES (@p0, @p1, @p2, @p3, @p4, @p5," +
        " @p6, @p7)", idTurno, idOs, idOrden, estudio ? "autorización"
        : "afiliación", resultado == "pendiente" ? "diferida" : "en línea",
        resultado, refe, Formato.IsoHora(Reloj.Ahora));
      Auditoria.Alta(estudio ? "B" : "A", "Validar cobertura (CU-G-07)",
        "VALIDACIÓN DE COBERTURA", id, resultado + " · " + refe);
      return new Validacion { Id = id, Resultado = resultado,
        Referencia = refe, Mensaje = Mensaje(resultado, refe) };
    }

    static string Mensaje(string resultado, string referencia)
    {
      if (resultado == "válida") return "Válida · Ref. " + referencia;
      if (resultado == "rechazada")
        return "Rechazada: plan sin cobertura para esta práctica.";
      return "Pendiente (validación diferida): la obra social no respondió." +
        " Podés acreditar; la atención queda marcada para regularizar.";
    }

    // ---------- CU-G-06 Acreditar ----------

    /// <summary>
    /// Acredita el turno con la modalidad de pago. Devuelve un aviso si el
    /// paciente llegó después del horario del turno (decide el personal).
    /// </summary>
    public static string Acreditar(int idTurno, string modalidad,
      Validacion v, bool estudio)
    {
      Sesion.Exigir("CU-G-06");
      if (modalidad == "obra social") ExigirValidacion(v, estudio);
      else ReglaException.Exigir(modalidad == "particular",
        "Elegí la modalidad de pago.", "modalidad");
      int n = Consulta.Ejecutar("UPDATE turno SET estado = 'acreditado', " +
        "modalidad_pago = @p0, arancel_informado = @p1 WHERE id_turno = @p2" +
        " AND estado = 'reservado'", modalidad, modalidad == "particular"
        ? (object)Arancel(estudio) : null, idTurno);
      ReglaException.Exigir(n == 1, "Ese turno ya no está reservado.");
      Auditoria.Cambio(estudio ? "B" : "A", Modulo, "TURNO", idTurno,
        "estado: reservado", "estado: acreditado · " + modalidad);
      return Demora(idTurno);
    }

    static void ExigirValidacion(Validacion v, bool estudio)
    {
      ReglaException.Exigir(v != null, "Validá la cobertura antes de " +
        "acreditar, o elegí «Particular».", "cobertura");
      ReglaException.Exigir(v.Resultado != "rechazada", "La cobertura fue " +
        "rechazada: continuá como particular.", "cobertura");
      ReglaException.Exigir(!estudio || v.Resultado == "válida",
        "El estudio necesita la autorización de la obra social.");
    }

    static string Demora(int idTurno)
    {
      DateTime turno = Formato.Leer(Consulta.Texto("SELECT fecha_hora FROM " +
        "turno WHERE id_turno = @p0", idTurno));
      int min = (int)(Reloj.Ahora - turno).TotalMinutes;
      return min > 0 ? "El turno era a las " + Formato.Hora(turno) +
        " y la llegada se registró a las " + Formato.Hora(Reloj.Ahora) +
        " (" + min + " minutos después)." : null;
    }

    // ---------- Validaciones pendientes (modo diferido) ----------

    /// <summary>Validaciones pendientes de regularizar.</summary>
    public static DataTable Pendientes()
    {
      Sesion.ExigirLectura("CU-G-07");
      return Consulta.Tabla("SELECT v.id_validacion AS Id, v.fecha_hora AS" +
        " Fecha, pa.apellido_nombre AS Paciente, o.nombre AS [Obra social]" +
        " FROM validacion_de_cobertura v JOIN turno t ON t.id_turno = " +
        "v.id_turno JOIN paciente pa ON pa.id_paciente = t.id_paciente " +
        "JOIN obra_social o ON o.id_obra_social = v.id_obra_social WHERE " +
        "v.resultado = 'pendiente' ORDER BY v.fecha_hora");
    }

    /// <summary>
    /// Registra la respuesta posterior: válida (queda regularizada) o
    /// rechazada (la atención se reclasifica como particular).
    /// </summary>
    public static void Regularizar(int idValidacion, bool valida)
    {
      Sesion.Exigir("CU-G-07");
      int idTurno = Consulta.Entero("SELECT id_turno FROM " +
        "validacion_de_cobertura WHERE id_validacion = @p0", idValidacion);
      Consulta.Ejecutar("UPDATE validacion_de_cobertura SET resultado = " +
        "@p0, fecha_regularizacion = @p1 WHERE id_validacion = @p2",
        valida ? "válida" : "rechazada", Formato.Iso(Reloj.Hoy),
        idValidacion);
      if (!valida) Consulta.Ejecutar("UPDATE turno SET modalidad_pago = " +
        "'particular', arancel_informado = @p0 WHERE id_turno = @p1",
        Arancel(false), idTurno);
      Auditoria.Cambio("A", "Validar cobertura (CU-G-07)",
        "VALIDACIÓN DE COBERTURA", idValidacion, "resultado: pendiente",
        "resultado: " + (valida ? "válida (regularizada)" : "rechazada"));
    }
  }

  /// <summary>
  /// Simulación del servicio de las obras sociales (no hay integración
  /// real: RT y RNF-06). Respuestas fijas por obra social, para la demo.
  /// </summary>
  public static class ObraSocialSimulada
  {
    /// <summary>«válida», «rechazada» o «pendiente» (no responde).</summary>
    public static string Consultar(string obraSocial)
    {
      if (obraSocial.Contains("Andina")) return "rechazada";
      if (obraSocial.Contains("Prevenir")) return "pendiente";
      return "válida";
    }
  }
}
