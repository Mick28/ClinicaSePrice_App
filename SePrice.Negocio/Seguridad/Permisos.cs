using System.Collections.Generic;
using System.Linq;

namespace SePrice.Negocio
{
  /// <summary>
  /// Los 8 perfiles que inician sesión (cuaderno, 4.4 y 5.3).
  /// </summary>
  public enum Perfil
  {
    Administrador, Coordinacion, Administrativo, Medico, Tecnico,
    Guardia, Insumos, Jefatura
  }

  /// <summary>Procesos que un perfil ejecuta (E) o audita (L).</summary>
  public class Permiso
  {
    public readonly HashSet<string> Edita, Consulta;

    public Permiso(string[] edita, string[] consulta)
    {
      Edita = new HashSet<string>(edita);
      Consulta = new HashSet<string>(consulta);
    }
  }

  /// <summary>
  /// Consultas sobre la matriz de permisos (la matriz está en
  /// Permisos.generado.cs, generada desde el cuaderno).
  /// </summary>
  public static partial class Permisos
  {
    /// <summary>¿El perfil ejecuta el proceso general?</summary>
    public static bool Ejecuta(Perfil perfil, string cu)
    {
      return Matriz[perfil].Edita.Contains(cu);
    }

    /// <summary>¿El perfil lo consulta en sólo lectura (auditoría)?</summary>
    public static bool Audita(Perfil perfil, string cu)
    {
      return Matriz[perfil].Consulta.Contains(cu);
    }

    /// <summary>Procesos que el perfil audita.</summary>
    public static IEnumerable<string> Auditables(Perfil perfil)
    {
      return Matriz[perfil].Consulta.OrderBy(x => x);
    }

    /// <summary>Perfil a partir del rol guardado en USUARIO.rol.</summary>
    public static Perfil DeRol(string rol)
    {
      return Nombres.First(p => p.Value == rol).Key;
    }
  }
}
