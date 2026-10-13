using System.Collections.Generic;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>
  /// CU-G-16 Gestionar usuarios del sistema (CU-B-18, CU-A-20). Padrón
  /// único: cada usuario corresponde a un empleado o a un profesional.
  /// </summary>
  public static class Usuarios
  {
    const string Cu = "CU-G-16", Modulo = "Usuarios (CU-G-16)";
    const string Provisoria = "contraseña provisoria";

    const string Select = "SELECT u.*, COALESCE(e.apellido_nombre, " +
      "p.apellido_nombre) AS nombre, (SELECT MAX(inicio) FROM " +
      "registro_de_sesion s WHERE s.id_usuario = u.id_usuario) AS ultimo " +
      "FROM usuario u LEFT JOIN empleado_administrativo e ON " +
      "e.id_empleado = u.id_empleado LEFT JOIN profesional p ON " +
      "p.id_profesional = u.id_profesional ";

    /// <summary>Busca por nombre de usuario (inicio de sesión).</summary>
    public static Usuario Buscar(string nombreUsuario, out string clave)
    {
      clave = Consulta.Texto("SELECT contrasena FROM usuario WHERE " +
        "nombre_usuario = @p0", (nombreUsuario ?? "").Trim());
      return Consulta.Uno(Select + "WHERE u.nombre_usuario = @p0", Leer,
        (nombreUsuario ?? "").Trim());
    }

    /// <summary>Lista filtrada por texto (usuario o nombre) y estado.</summary>
    public static List<Usuario> Listar(string texto, string estado)
    {
      Sesion.Exigir(Cu);
      return Consulta.Lista(Select + "WHERE (u.nombre_usuario LIKE @p0 OR " +
        "COALESCE(e.apellido_nombre, p.apellido_nombre) LIKE @p0) AND " +
        "(@p1 IS NULL OR u.estado = @p1) ORDER BY u.nombre_usuario", Leer,
        "%" + (texto ?? "").Trim() + "%", estado);
    }

    /// <summary>Cantidad de usuarios habilitados (Inicio del Admin).</summary>
    public static int Activos()
    {
      return Consulta.Entero("SELECT COUNT(*) FROM usuario WHERE " +
        "estado = 'habilitado'");
    }

    /// <summary>Alta de un usuario nuevo, habilitado.</summary>
    public static int Alta(DatosUsuario d)
    {
      Sesion.Exigir(Cu);
      ValidarAlta(d);
      int id = 0;
      Consulta.EnTransaccion(() => id = Insertar(d));
      Auditoria.Alta("ambos", Modulo, "USUARIO", id, d.NombreUsuario +
        " · " + Permisos.Nombres[d.Perfil]);
      return id;
    }

    static void ValidarAlta(DatosUsuario d)
    {
      ReglaException.Exigir(!string.IsNullOrWhiteSpace(d.NombreUsuario),
        "Completá el nombre de usuario.", "usuario");
      ReglaException.Exigir(Consulta.Entero("SELECT COUNT(*) FROM usuario " +
        "WHERE nombre_usuario = @p0", d.NombreUsuario.Trim()) == 0,
        "Ese nombre de usuario ya existe. Elegí otro.", "usuario");
      ValidarClave(d.Clave, d.Repetir);
      if (EsProfesional(d.Perfil)) ValidarProfesional(d);
      else ReglaException.Exigir(!string.IsNullOrWhiteSpace(d.Nombre),
        "Completá el nombre y apellido.", "nombre");
    }

    static void ValidarProfesional(DatosUsuario d)
    {
      ReglaException.Exigir(d.IdProfesional.HasValue,
        "Elegí el profesional al que corresponde el usuario.", "persona");
      ReglaException.Exigir(Consulta.Entero("SELECT COUNT(*) FROM usuario " +
        "WHERE id_profesional = @p0", d.IdProfesional) == 0,
        "Ese profesional ya tiene un usuario.", "persona");
    }

    /// <summary>Valida una contraseña nueva y su repetición.</summary>
    public static void ValidarClave(string clave, string repetir)
    {
      ReglaException.Exigir((clave ?? "").Length >= 8,
        "La contraseña debe tener al menos 8 caracteres.", "clave");
      ReglaException.Exigir(clave == repetir,
        "Las contraseñas no coinciden.", "repetir");
    }

    static int Insertar(DatosUsuario d)
    {
      int? emp = null, prof = d.IdProfesional;
      if (!EsProfesional(d.Perfil))
      {
        emp = Consulta.Insertar("INSERT INTO empleado_administrativo " +
          "(apellido_nombre, rol) VALUES (@p0, @p1)", d.Nombre.Trim(),
          RolEmpleado(d.Perfil));
        prof = null;
      }
      return Consulta.Insertar("INSERT INTO usuario (nombre_usuario, " +
        "contrasena, rol, estado, fecha_alta, id_empleado, id_profesional)" +
        " VALUES (@p0, @p1, @p2, 'habilitado', @p3, @p4, @p5)",
        d.NombreUsuario.Trim(), Clave.Cifrar(d.NombreUsuario, d.Clave),
        Permisos.Nombres[d.Perfil], Formato.Iso(Reloj.Hoy), emp, prof);
    }

    /// <summary>Cambia el perfil (dentro del mismo tipo de persona).</summary>
    public static void CambiarPerfil(int id, Perfil nuevo)
    {
      Sesion.Exigir(Cu);
      Usuario u = PorId(id);
      ReglaException.Exigir(EsProfesional(u.Perfil) == EsProfesional(nuevo),
        "Un profesional sólo puede tener perfil de profesional, y un " +
        "empleado, de empleado.", "perfil");
      Consulta.Ejecutar("UPDATE usuario SET rol = @p0 WHERE id_usuario = " +
        "@p1", Permisos.Nombres[nuevo], id);
      Auditoria.Cambio("ambos", Modulo, "USUARIO", id, "rol: " + u.Rol,
        "rol: " + Permisos.Nombres[nuevo]);
    }

    /// <summary>Baja lógica: el usuario no puede volver a ingresar.</summary>
    public static void DarDeBaja(int id)
    {
      Sesion.Exigir(Cu);
      ReglaException.Exigir(id != Sesion.Actual.Id,
        "No podés darte de baja a vos mismo/a.");
      CambiarEstado(id, "deshabilitado", "baja");
    }

    /// <summary>Vuelve a habilitar un usuario dado de baja.</summary>
    public static void Reactivar(int id)
    {
      Sesion.Exigir(Cu);
      CambiarEstado(id, "habilitado", "modificación");
    }

    static void CambiarEstado(int id, string estado, string accion)
    {
      Usuario u = PorId(id);
      Consulta.Ejecutar("UPDATE usuario SET estado = @p0 WHERE " +
        "id_usuario = @p1", estado, id);
      Auditoria.Registrar("ambos", Modulo, accion, "USUARIO", id,
        "estado: " + u.Estado, "estado: " + estado);
    }

    /// <summary>Asigna una contraseña provisoria (CU-B-18).</summary>
    public static void Restablecer(int id, string clave, string repetir)
    {
      Sesion.Exigir(Cu);
      ValidarClave(clave, repetir);
      Usuario u = PorId(id);
      Consulta.Ejecutar("UPDATE usuario SET contrasena = @p0 WHERE " +
        "id_usuario = @p1", Clave.Cifrar(u.NombreUsuario, clave), id);
      Auditoria.Cambio("ambos", Modulo, "USUARIO", id, null, Provisoria);
    }

    /// <summary>
    /// ¿Tiene una contraseña provisoria todavía no cambiada? Se sabe por la
    /// auditoría: hubo un restablecimiento después de su último ingreso.
    /// </summary>
    public static bool DebeCambiarClave(int id)
    {
      return Consulta.Entero("SELECT COUNT(*) FROM registro_de_auditoria " +
        "a WHERE a.entidad = 'USUARIO' AND a.id_registro = @p0 AND " +
        "a.valor_nuevo = @p1 AND a.fecha_hora >= COALESCE((SELECT " +
        "MAX(inicio) FROM registro_de_sesion WHERE id_usuario = @p2), '')",
        id.ToString(), Provisoria, id) > 0;
    }

    /// <summary>El usuario de la sesión reemplaza su contraseña.</summary>
    public static void CambiarMiClave(string clave, string repetir)
    {
      ValidarClave(clave, repetir);
      Usuario u = Sesion.Actual;
      Consulta.Ejecutar("UPDATE usuario SET contrasena = @p0 WHERE " +
        "id_usuario = @p1", Clave.Cifrar(u.NombreUsuario, clave), u.Id);
      Auditoria.Cambio("ambos", Modulo, "USUARIO", u.Id, Provisoria,
        "contraseña cambiada por el usuario");
    }

    /// <summary>
    /// Profesionales (médicos o técnicos) que no tienen usuario.
    /// </summary>
    public static List<Opcion> ProfesionalesSinUsuario(Perfil perfil)
    {
      string tipo = perfil == Perfil.Tecnico ? "técnico" : "médico";
      return Consulta.Lista("SELECT id_profesional AS id, apellido_nombre " +
        "|| ' (' || matricula || ')' AS texto FROM profesional p WHERE " +
        "tipo = @p0 AND estado = 'activo' AND NOT EXISTS (SELECT 1 FROM " +
        "usuario u WHERE u.id_profesional = p.id_profesional) ORDER BY 2",
        Opcion.Leer, tipo);
    }

    /// <summary>Usuario por id.</summary>
    public static Usuario PorId(int id)
    {
      return Consulta.Uno(Select + "WHERE u.id_usuario = @p0", Leer, id);
    }

    /// <summary>¿El perfil corresponde a un profesional de la salud?</summary>
    public static bool EsProfesional(Perfil p)
    {
      return p == Perfil.Medico || p == Perfil.Tecnico;
    }

    static string RolEmpleado(Perfil p)
    {
      switch (p)
      {
        case Perfil.Administrador: return "administración del sistema";
        case Perfil.Coordinacion: return "coordinación";
        case Perfil.Guardia: return "guardia";
        case Perfil.Insumos: return "insumos";
        case Perfil.Jefatura: return "jefatura";
        default: return "mostrador";
      }
    }

    static Usuario Leer(Fila f)
    {
      return new Usuario
      {
        Id = f.Entero("id_usuario"), Estado = f.Texto("estado"),
        NombreUsuario = f.Texto("nombre_usuario"), Rol = f.Texto("rol"),
        Nombre = f.Texto("nombre"), FechaAlta = f.Texto("fecha_alta"),
        Perfil = Permisos.DeRol(f.Texto("rol")),
        IdEmpleado = f.EnteroONulo("id_empleado"),
        IdProfesional = f.EnteroONulo("id_profesional"),
        UltimoIngreso = f.Texto("ultimo")
      };
    }
  }

  /// <summary>Datos del formulario de alta de usuario (P-03).</summary>
  public class DatosUsuario
  {
    public string Nombre, NombreUsuario, Clave, Repetir;
    public Perfil Perfil;
    public int? IdProfesional;
  }
}
