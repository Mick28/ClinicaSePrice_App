using System;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Jornada de hoy del profesional (P-22).</summary>
  public class Jornada
  {
    public string Lugar, Desde, Hasta, Estado, Ingreso, Egreso;

    /// <summary>¿Tiene agenda hoy?</summary>
    public bool TieneAgenda { get { return Desde != null; } }
  }

  /// <summary>
  /// CU-G-13 Registrar ingreso y egreso del profesional (CU-A-17, sólo
  /// Consultorios externos). El ingreso habilita la sala de espera; si vence
  /// la tolerancia sin ingreso, la agenda queda «profesional ausente».
  /// </summary>
  public static class Presentismo
  {
    const string Cu = "CU-G-13", Modulo = "Presentismo (CU-G-13)";

    static int Yo
    {
      get { return Sesion.Actual.IdProfesional ?? 0; }
    }

    /// <summary>
    /// Agenda y registro de hoy del profesional de la sesión.
    /// </summary>
    public static Jornada Hoy()
    {
      Sesion.Exigir(Cu);
      Jornada j = Consulta.Uno("SELECT c.numero_nombre AS l, MIN(" +
        "a.hora_desde) AS d, MAX(a.hora_hasta) AS h FROM agenda a JOIN " +
        "consultorio_sala c ON c.id_consultorio = a.id_consultorio WHERE " +
        "a.id_profesional = @p0 AND a.dia = @p1", f => new Jornada
        { Lugar = f.Texto("l"), Desde = f.Texto("d"), Hasta = f.Texto("h") },
        Yo, Formato.Iso(Reloj.Hoy)) ?? new Jornada();
      Fila r = Registro(Yo);
      j.Estado = r == null ? "sin registrar" : r.Texto("estado");
      j.Ingreso = r?.Texto("hora_ingreso");
      j.Egreso = r?.Texto("hora_egreso");
      return j;
    }

    static Fila Registro(int idProfesional)
    {
      return Consulta.Uno("SELECT * FROM registro_de_presentismo WHERE " +
        "id_profesional = @p0 AND fecha = @p1", f => f, idProfesional,
        Formato.Iso(Reloj.Hoy));
    }

    /// <summary>¿El profesional registró su ingreso hoy?</summary>
    public static bool Presente(int idProfesional)
    {
      Fila r = Registro(idProfesional);
      return r != null && r.Texto("estado") == "presente";
    }

    /// <summary>
    /// Registra el ingreso. Devuelve un aviso si fue después de la
    /// tolerancia desde el inicio de la agenda.
    /// </summary>
    public static string RegistrarIngreso()
    {
      Sesion.Exigir(Cu);
      Jornada j = Hoy();
      ReglaException.Exigir(j.TieneAgenda, "No tenés agenda hoy.");
      ReglaException.Exigir(j.Ingreso == null, "Ya registraste tu ingreso " +
        "a las " + j.Ingreso + ".");
      string hora = Formato.Hora(Reloj.Ahora);
      Guardar(hora);
      int tarde = Formato.Minutos(hora) - Formato.Minutos(j.Desde);
      return tarde > Parametros.Numero("tolerancia_ingreso") ? "Registraste" +
        " el ingreso " + tarde + " minutos después del inicio de tu agenda."
        : null;
    }

    static void Guardar(string hora)
    {
      Consulta.EnTransaccion(() =>
      {
        Consulta.Ejecutar("DELETE FROM registro_de_presentismo WHERE " +
          "id_profesional = @p0 AND fecha = @p1", Yo, Formato.Iso(Reloj.Hoy));
        Consulta.Ejecutar("INSERT INTO registro_de_presentismo (" +
          "id_profesional, fecha, hora_ingreso, estado) VALUES (@p0, @p1, " +
          "@p2, 'presente')", Yo, Formato.Iso(Reloj.Hoy), hora);
        Consulta.Ejecutar("UPDATE agenda SET profesional_ausente = 0 WHERE " +
          "id_profesional = @p0 AND dia = @p1", Yo, Formato.Iso(Reloj.Hoy));
      });
      Auditoria.Alta("A", Modulo, "REGISTRO DE PRESENTISMO", Yo,
        "ingreso " + hora);
    }

    /// <summary>
    /// Pacientes acreditados que todavía esperan al profesional.
    /// </summary>
    public static int PacientesEsperando()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM turno t JOIN agenda a ON" +
        " a.id_agenda = t.id_agenda WHERE a.id_profesional = @p0 AND a.dia" +
        " = @p1 AND t.estado IN ('acreditado', 'en atención')", Yo,
        Formato.Iso(Reloj.Hoy));
    }

    /// <summary>Registra el egreso y cierra la jornada.</summary>
    public static void RegistrarEgreso()
    {
      Sesion.Exigir(Cu);
      Jornada j = Hoy();
      ReglaException.Exigir(j.Ingreso != null, "Primero registrá tu " +
        "ingreso.");
      ReglaException.Exigir(j.Egreso == null, "Ya registraste tu egreso.");
      string hora = Formato.Hora(Reloj.Ahora);
      Consulta.Ejecutar("UPDATE registro_de_presentismo SET hora_egreso = " +
        "@p0 WHERE id_profesional = @p1 AND fecha = @p2", hora, Yo,
        Formato.Iso(Reloj.Hoy));
      Auditoria.Cambio("A", Modulo, "REGISTRO DE PRESENTISMO", Yo, null,
        "egreso " + hora);
    }

    /// <summary>Registros de la semana del profesional de la sesión.</summary>
    public static DataTable Semana()
    {
      Sesion.Exigir(Cu);
      return Consulta.Tabla("SELECT fecha AS Fecha, COALESCE(hora_ingreso," +
        " '—') AS Ingreso, COALESCE(hora_egreso, '—') AS Egreso, estado AS" +
        " Estado FROM registro_de_presentismo WHERE id_profesional = @p0 " +
        "AND fecha > @p1 ORDER BY fecha DESC", Yo,
        Formato.Iso(Reloj.Hoy.AddDays(-7)));
    }

    /// <summary>
    /// Control automático del día (CU-A-17, alternativos): si venció la
    /// tolerancia sin ingreso, registra la ausencia y marca la agenda
    /// «profesional ausente»; los días anteriores sin egreso quedan como
    /// «egreso no registrado».
    /// </summary>
    public static void ControlarAusencias()
    {
      string hoy = Formato.Iso(Reloj.Hoy);
      string limite = Formato.HoraDe(Formato.Minutos(Formato.Hora(
        Reloj.Ahora)) - Parametros.Numero("tolerancia_ingreso"));
      foreach (int id in Consulta.Lista("SELECT DISTINCT a.id_profesional " +
        "AS id FROM agenda a JOIN profesional p ON p.id_profesional = " +
        "a.id_profesional WHERE a.dia = @p0 AND a.hora_desde <= @p1 AND " +
        "p.tipo = 'médico' AND NOT EXISTS (SELECT 1 FROM " +
        "registro_de_presentismo r WHERE r.id_profesional = " +
        "a.id_profesional AND r.fecha = @p0)", f => f.Entero("id"), hoy,
        limite))
        MarcarAusente(id, hoy);
      Consulta.Ejecutar("UPDATE registro_de_presentismo SET estado = " +
        "'egreso no registrado' WHERE fecha < @p0 AND estado = 'presente' " +
        "AND hora_egreso IS NULL", hoy);
    }

    static void MarcarAusente(int idProfesional, string hoy)
    {
      Consulta.Ejecutar("INSERT INTO registro_de_presentismo (" +
        "id_profesional, fecha, estado) VALUES (@p0, @p1, 'ausente')",
        idProfesional, hoy);
      Consulta.Ejecutar("UPDATE agenda SET profesional_ausente = 1 WHERE " +
        "id_profesional = @p0 AND dia = @p1", idProfesional, hoy);
      Auditoria.Alta("A", Modulo + " · control automático",
        "REGISTRO DE PRESENTISMO", idProfesional, "ausente " + hoy);
    }

    /// <summary>Profesionales con la agenda de hoy marcada «ausente».</summary>
    public static string AusentesHoy()
    {
      return Consulta.Texto("SELECT group_concat(n, '; ') FROM (SELECT " +
        "DISTINCT p.apellido_nombre AS n FROM agenda a JOIN profesional p " +
        "ON p.id_profesional = a.id_profesional WHERE a.dia = @p0 AND " +
        "a.profesional_ausente = 1)", Formato.Iso(Reloj.Hoy));
    }
  }
}
