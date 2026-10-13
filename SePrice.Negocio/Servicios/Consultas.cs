using System;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>
  /// Consultas de sólo lectura de la operación, para las pantallas abiertas
  /// en modo auditoría por el Administrador/a (CU-G-18). Cada una exige el
  /// permiso de lectura del proceso que muestra (matriz 5.3).
  /// </summary>
  public static class Consultas
  {
    /// <summary>Padrón de pacientes (P-05).</summary>
    public static DataTable Pacientes()
    {
      Sesion.ExigirLectura("CU-G-02");
      return Consulta.Tabla("SELECT p.nro_legajo AS Legajo, " +
        "p.apellido_nombre AS Paciente, p.dni AS DNI, p.fecha_nacimiento AS" +
        " Nacimiento, p.telefono AS Teléfono, COALESCE(o.nombre, " +
        "'Particular') AS [Obra social], p.nro_afiliado AS Afiliado FROM " +
        "paciente p LEFT JOIN obra_social o ON o.id_obra_social = " +
        "p.id_obra_social ORDER BY p.apellido_nombre");
    }

    const string Turnos = "SELECT t.fecha_hora AS [Fecha y hora], " +
      "pa.apellido_nombre AS Paciente, COALESCE(e.nombre, p.apellido_nombre)" +
      " AS [Profesional o estudio], t.procedencia AS Origen, " +
      "t.modalidad_toma AS Toma, t.modalidad_pago AS Pago, t.estado AS " +
      "Estado, u.nombre_usuario AS [Registró] FROM turno t JOIN paciente pa" +
      " ON pa.id_paciente = t.id_paciente LEFT JOIN agenda a ON a.id_agenda" +
      " = t.id_agenda LEFT JOIN profesional p ON p.id_profesional = " +
      "a.id_profesional LEFT JOIN detalle_turno_estudio x ON x.id_turno = " +
      "t.id_turno LEFT JOIN estudio e ON e.id_estudio = x.id_estudio LEFT " +
      "JOIN usuario u ON u.id_empleado = t.id_empleado ";

    /// <summary>Turnos de un día (P-06, P-08, P-09).</summary>
    public static DataTable TurnosDelDia(DateTime dia)
    {
      Sesion.ExigirLectura("CU-G-04");
      return Consulta.Tabla(Turnos + "WHERE t.fecha_hora LIKE @p0 ORDER BY " +
        "t.fecha_hora", Formato.Iso(dia) + "%");
    }

    /// <summary>
    /// Demanda sin turno del día: sobreturnos, laboratorio, guardia.
    /// </summary>
    public static DataTable DemandaDelDia(DateTime dia)
    {
      Sesion.ExigirLectura("CU-G-05");
      return Consulta.Tabla(Turnos + "WHERE t.fecha_hora LIKE @p0 AND " +
        "t.procedencia <> 'agenda' ORDER BY t.fecha_hora",
        Formato.Iso(dia) + "%");
    }

    /// <summary>
    /// Acreditaciones y validaciones de cobertura del día (P-10).
    /// </summary>
    public static DataTable Acreditaciones(DateTime dia)
    {
      Sesion.ExigirLectura("CU-G-06");
      return Consulta.Tabla("SELECT t.fecha_hora AS Turno, pa.apellido_nombre" +
        " AS Paciente, t.modalidad_pago AS Pago, t.arancel_informado AS " +
        "Arancel, v.tipo AS Validación, v.modo AS Modo, v.resultado AS " +
        "Resultado, v.nro_referencia AS Referencia, t.estado AS Estado FROM " +
        "turno t JOIN paciente pa ON pa.id_paciente = t.id_paciente LEFT " +
        "JOIN validacion_de_cobertura v ON v.id_turno = t.id_turno WHERE " +
        "t.fecha_hora LIKE @p0 AND t.estado <> 'reservado' ORDER BY 1",
        Formato.Iso(dia) + "%");
    }

    /// <summary>Atenciones y estudios del día (P-13, P-14, P-15).</summary>
    public static DataTable Atenciones(DateTime dia)
    {
      Sesion.ExigirLectura("CU-G-08");
      return Consulta.Tabla(Turnos + "WHERE t.fecha_hora LIKE @p0 AND " +
        "t.estado IN ('acreditado', 'en atención', 'atendido', 'realizado'," +
        " 'entregado') ORDER BY t.fecha_hora", Formato.Iso(dia) + "%");
    }

    /// <summary>Resultados de estudios y su entrega (P-16).</summary>
    public static DataTable Resultados()
    {
      Sesion.ExigirLectura("CU-G-09");
      return Consulta.Tabla("SELECT r.nro_comprobante_retiro AS " +
        "Comprobante, pa.apellido_nombre AS Paciente, e.nombre AS Estudio, " +
        "r.fecha_realizacion AS Realizado, r.fecha_estimada AS Estimada, " +
        "r.fecha_entrega AS Entregado, r.receptor AS Receptor FROM " +
        "resultado_de_estudio r JOIN detalle_turno_estudio x ON " +
        "x.id_detalle = r.id_detalle JOIN turno t ON t.id_turno = x.id_turno" +
        " JOIN paciente pa ON pa.id_paciente = t.id_paciente JOIN estudio e " +
        "ON e.id_estudio = x.id_estudio ORDER BY r.fecha_realizacion DESC");
    }

    /// <summary>
    /// Registros de presentismo de todos los profesionales (P-22).
    /// </summary>
    public static DataTable Presentismo()
    {
      Sesion.ExigirLectura("CU-G-13");
      return Consulta.Tabla("SELECT r.fecha AS Fecha, p.apellido_nombre AS " +
        "Profesional, COALESCE(r.hora_ingreso, '—') AS Ingreso, " +
        "COALESCE(r.hora_egreso, '—') AS Egreso, r.estado AS Estado FROM " +
        "registro_de_presentismo r JOIN profesional p ON p.id_profesional = " +
        "r.id_profesional ORDER BY r.fecha DESC, p.apellido_nombre");
    }
  }
}
