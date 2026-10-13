using System;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Usuario del sistema (entidad USUARIO) con su persona.</summary>
  public class Usuario
  {
    public int Id;
    public string NombreUsuario, Nombre, Rol, Estado, FechaAlta;
    public Perfil Perfil;
    public int? IdEmpleado, IdProfesional;
    public string UltimoIngreso;

    /// <summary>Primer nombre, para el saludo: «Martín».</summary>
    public string PrimerNombre
    {
      get
      {
        int coma = Nombre.IndexOf(',');
        string n = coma < 0 ? Nombre : Nombre.Substring(coma + 1).Trim();
        int espacio = n.IndexOf(' ');
        return espacio < 0 ? n : n.Substring(0, espacio);
      }
    }
  }

  /// <summary>Resultado de un intento de inicio de sesión (CU-G-01).</summary>
  public enum ResultadoIngreso
  {
    Correcto, Incorrecto, Deshabilitado, CambiarClave
  }

  /// <summary>
  /// Sesión del usuario (CU-G-01 y CU-G-15). Abre la conexión al ingresar
  /// y la cierra al salir. <see cref="Cerrar"/> es el procedimiento único
  /// de cierre que usan todos los perfiles (material de la Etapa 3, eje 3).
  /// </summary>
  public static class Sesion
  {
    /// <summary>
    /// Usuario con la sesión abierta (null si no hay sesión).
    /// </summary>
    public static Usuario Actual { get; private set; }

    /// <summary>Hora de inicio de la sesión.</summary>
    public static DateTime Inicio { get; private set; }

    static int idSesion;

    /// <summary>Valida las credenciales y abre la sesión.</summary>
    public static ResultadoIngreso Iniciar(string usuario, string clave)
    {
      Conexion.Abrir(Configuracion.Base);
      string guardada;
      Usuario u = Usuarios.Buscar(usuario, out guardada);
      ResultadoIngreso r = Validar(u, clave, guardada);
      if (r == ResultadoIngreso.Incorrecto
        || r == ResultadoIngreso.Deshabilitado)
      {
        Conexion.Cerrar();
        return r;
      }
      Abrir(u);
      return r;
    }

    static ResultadoIngreso Validar(Usuario u, string clave, string guardada)
    {
      if (u == null || !Clave.Coincide(clave, guardada))
        return ResultadoIngreso.Incorrecto;
      if (u.Estado != "habilitado") return ResultadoIngreso.Deshabilitado;
      return Usuarios.DebeCambiarClave(u.Id)
        ? ResultadoIngreso.CambiarClave : ResultadoIngreso.Correcto;
    }

    static void Abrir(Usuario u)
    {
      Actual = u;
      Inicio = Reloj.Ahora;
      idSesion = Consulta.Insertar("INSERT INTO registro_de_sesion " +
        "(id_usuario, inicio) VALUES (@p0, @p1)",
        u.Id, Formato.IsoHora(Inicio));
    }

    /// <summary>
    /// Cierra la sesión: registra la hora de cierre y cierra la conexión.
    /// Es el mismo procedimiento para los 8 perfiles (CU-G-15).
    /// </summary>
    public static void Cerrar()
    {
      if (Actual == null) return;
      Consulta.Ejecutar("UPDATE registro_de_sesion SET cierre = @p0, " +
        "tipo_cierre = 'manual' WHERE id_sesion = @p1",
        Formato.IsoHora(Reloj.Ahora), idSesion);
      Actual = null;
      Conexion.Cerrar();
    }

    /// <summary>
    /// Control del lado del sistema (especificación 3.5.6, regla 8): la
    /// operación sólo sigue si el perfil ejecuta ese proceso general.
    /// </summary>
    public static void Exigir(string cu)
    {
      ReglaException.Exigir(Actual != null && Permisos.Ejecuta(
        Actual.Perfil, cu), "Esta función no corresponde a tu perfil.");
    }

    /// <summary>Como <see cref="Exigir"/>, pero para consultar.</summary>
    public static void ExigirLectura(string cu)
    {
      ReglaException.Exigir(Actual != null && (Permisos.Ejecuta(
        Actual.Perfil, cu) || Permisos.Audita(Actual.Perfil, cu)),
        "Esta función no corresponde a tu perfil.");
    }
  }
}
