using System.Collections.Generic;
using System.Data;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Legajo de paciente (entidad PACIENTE).</summary>
  public class Paciente
  {
    public int Id, Dni, Legajo;
    public int? IdObraSocial;
    public string Nombre, Nacimiento, Telefono, Correo, Afiliado;
    public string ObraSocial, Plan;

    public int Edad
    {
      get { return Formato.Edad(Nacimiento); }
    }

    /// <summary>
    /// Texto de la barra de paciente en curso (especificación).
    /// </summary>
    public string Barra
    {
      get
      {
        return "Paciente: " + Nombre + " · DNI " + Formato.Dni(Dni) + " · " +
          (ObraSocial ?? "Particular");
      }
    }
  }

  /// <summary>
  /// CU-G-02 Gestionar legajo de paciente (CU-A-02 y CU-B-02): un único
  /// legajo por DNI, válido para los dos circuitos.
  /// </summary>
  public static class Pacientes
  {
    const string Cu = "CU-G-02", Modulo = "Pacientes (CU-G-02)";

    const string Select = "SELECT p.*, o.nombre AS os, o.plan FROM " +
      "paciente p LEFT JOIN obra_social o ON o.id_obra_social = " +
      "p.id_obra_social ";

    /// <summary>Busca el legajo por DNI (null si no existe).</summary>
    public static Paciente PorDni(int dni)
    {
      ExigirBusqueda();
      return Consulta.Uno(Select + "WHERE p.dni = @p0", Leer, dni);
    }

    /// <summary>Legajo por id.</summary>
    public static Paciente PorId(int id)
    {
      return Consulta.Uno(Select + "WHERE p.id_paciente = @p0", Leer, id);
    }

    // La guardia busca por DNI dentro de P-12, sin crear ni editar (5.3).
    static void ExigirBusqueda()
    {
      if (Sesion.Actual != null && Sesion.Actual.Perfil == Perfil.Guardia)
        return;
      Sesion.ExigirLectura(Cu);
    }

    /// <summary>Crea el legajo con un número único.</summary>
    public static Paciente Alta(Paciente p)
    {
      Sesion.Exigir(Cu);
      Validar(p);
      ReglaException.Exigir(Consulta.Entero("SELECT COUNT(*) FROM paciente" +
        " WHERE dni = @p0", p.Dni) == 0, "Ya existe un legajo con este " +
        "DNI.", "dni");
      p.Legajo = Consulta.Entero("SELECT COALESCE(MAX(nro_legajo), 10000) " +
        "+ 1 FROM paciente");
      p.Id = Consulta.Insertar("INSERT INTO paciente (dni, nro_legajo, " +
        "apellido_nombre, fecha_nacimiento, telefono, correo, " +
        "id_obra_social, nro_afiliado) VALUES (@p0, @p1, @p2, @p3, @p4, " +
        "@p5, @p6, @p7)", p.Dni, p.Legajo, p.Nombre, p.Nacimiento,
        p.Telefono, Vacio(p.Correo), p.IdObraSocial, Vacio(p.Afiliado));
      Auditoria.Alta("ambos", Modulo, "PACIENTE", p.Id, p.Nombre + " · DNI "
        + p.Dni);
      return PorId(p.Id);
    }

    /// <summary>Actualiza los datos del legajo (el DNI no cambia).</summary>
    public static void Modificar(Paciente p)
    {
      Sesion.Exigir(Cu);
      Validar(p);
      Paciente antes = PorId(p.Id);
      Consulta.Ejecutar("UPDATE paciente SET apellido_nombre = @p0, " +
        "fecha_nacimiento = @p1, telefono = @p2, correo = @p3, " +
        "id_obra_social = @p4, nro_afiliado = @p5 WHERE id_paciente = @p6",
        p.Nombre, p.Nacimiento, p.Telefono, Vacio(p.Correo), p.IdObraSocial,
        Vacio(p.Afiliado), p.Id);
      Auditoria.Cambio("ambos", Modulo, "PACIENTE", p.Id, Resumen(antes),
        Resumen(p));
    }

    static string Resumen(Paciente p)
    {
      return "nombre: " + p.Nombre + "; nacimiento: " + p.Nacimiento +
        "; telefono: " + p.Telefono + "; correo: " + p.Correo +
        "; obra social: " + (p.IdObraSocial?.ToString() ?? "particular") +
        "; afiliado: " + p.Afiliado;
    }

    static void Validar(Paciente p)
    {
      ReglaException.Exigir(p.Dni >= 1000000 && p.Dni <= 99999999,
        "El DNI tiene que tener 7 u 8 dígitos.", "dni");
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(p.Nombre),
        "Completá apellido y nombre.", "nombre");
      ReglaException.Exigir(!string.IsNullOrEmpty(p.Nacimiento) &&
        string.CompareOrdinal(p.Nacimiento, Formato.Iso(Reloj.Hoy)) <= 0,
        "La fecha de nacimiento no puede ser futura.", "nacimiento");
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(p.Telefono),
        "Completá el teléfono.", "telefono");
      ReglaException.Exigir(string.IsNullOrWhiteSpace(p.Correo) ||
        p.Correo.Contains("@"), "Revisá el correo electrónico.", "correo");
      ReglaException.Exigir(p.IdObraSocial == null ||
        !string.IsNullOrWhiteSpace(p.Afiliado),
        "Completá el número de afiliado.", "afiliado");
    }

    /// <summary>Historial de turnos del paciente (pestaña de P-05).</summary>
    public static DataTable Historial(int idPaciente)
    {
      Sesion.ExigirLectura(Cu);
      return Consulta.Tabla("SELECT t.fecha_hora AS Fecha, CASE WHEN " +
        "d.id_estudio IS NULL THEN 'Consultorios externos' ELSE " +
        "'Estudios clínicos' END AS Circuito, COALESCE(es.nombre, " +
        "p.apellido_nombre) AS [Profesional o estudio], t.estado AS " +
        "Estado FROM turno t LEFT JOIN agenda a ON a.id_agenda = " +
        "t.id_agenda LEFT JOIN profesional p ON p.id_profesional = " +
        "a.id_profesional LEFT JOIN detalle_turno_estudio d ON d.id_turno =" +
        " t.id_turno LEFT JOIN estudio es ON es.id_estudio = d.id_estudio " +
        "WHERE t.id_paciente = @p0 ORDER BY t.fecha_hora DESC", idPaciente);
    }

    /// <summary>Obras sociales para el combo (más «Sin cobertura»).</summary>
    public static List<Opcion> ObrasSociales()
    {
      List<Opcion> l = Consulta.Lista("SELECT id_obra_social AS id, " +
        "nombre || ' · plan ' || plan AS texto FROM obra_social ORDER BY " +
        "nombre", Opcion.Leer);
      l.Insert(0, new Opcion(0, "Sin cobertura (particular)"));
      return l;
    }

    static string Vacio(string s)
    {
      return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    static Paciente Leer(Fila f)
    {
      return new Paciente
      {
        Id = f.Entero("id_paciente"), Dni = f.Entero("dni"),
        Legajo = f.Entero("nro_legajo"), Nombre = f.Texto("apellido_nombre"),
        Nacimiento = f.Texto("fecha_nacimiento"),
        Telefono = f.Texto("telefono"), Correo = f.Texto("correo"),
        IdObraSocial = f.EnteroONulo("id_obra_social"),
        Afiliado = f.Texto("nro_afiliado"), ObraSocial = f.Texto("os"),
        Plan = f.Texto("plan")
      };
    }
  }
}
