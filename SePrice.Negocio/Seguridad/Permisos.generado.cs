// GENERADO por gen_sql.js desde etapa3.js (matriz 5.3 del Cuaderno de
// especificaciones). No editar a mano: se regenera.
using System.Collections.Generic;

namespace SePrice.Negocio
{
  /// <summary>Matriz de perfiles y permisos (cuaderno, sección 5.3).</summary>
  public static partial class Permisos
  {
    /// <summary>Procesos que cada perfil ejecuta (E) o audita (L).</summary>
    static readonly Dictionary<Perfil, Permiso> Matriz =
      new Dictionary<Perfil, Permiso>
    {
      { Perfil.Administrador, new Permiso(
          new string[] { "CU-G-01", "CU-G-03", "CU-G-12", "CU-G-15", "CU-G-16", "CU-G-17", "CU-G-18" },
          new string[] { "CU-G-02", "CU-G-04", "CU-G-05", "CU-G-06", "CU-G-07", "CU-G-08", "CU-G-09", "CU-G-10", "CU-G-11", "CU-G-13", "CU-G-14" }) },
      { Perfil.Coordinacion, new Permiso(
          new string[] { "CU-G-01", "CU-G-03", "CU-G-12", "CU-G-15", "CU-G-17" },
          new string[] {  }) },
      { Perfil.Administrativo, new Permiso(
          new string[] { "CU-G-01", "CU-G-02", "CU-G-04", "CU-G-05", "CU-G-06", "CU-G-07", "CU-G-09", "CU-G-10", "CU-G-15" },
          new string[] {  }) },
      { Perfil.Medico, new Permiso(
          new string[] { "CU-G-01", "CU-G-08", "CU-G-13", "CU-G-15" },
          new string[] {  }) },
      { Perfil.Tecnico, new Permiso(
          new string[] { "CU-G-01", "CU-G-08", "CU-G-15" },
          new string[] {  }) },
      { Perfil.Guardia, new Permiso(
          new string[] { "CU-G-01", "CU-G-05", "CU-G-15" },
          new string[] {  }) },
      { Perfil.Insumos, new Permiso(
          new string[] { "CU-G-01", "CU-G-10", "CU-G-15" },
          new string[] {  }) },
      { Perfil.Jefatura, new Permiso(
          new string[] { "CU-G-01", "CU-G-11", "CU-G-14", "CU-G-15", "CU-G-17" },
          new string[] {  }) },
    };

    /// <summary>Nombre de cada perfil, igual que en el cuaderno.</summary>
    public static readonly Dictionary<Perfil, string> Nombres =
      new Dictionary<Perfil, string>
    {
      { Perfil.Administrador, "Administrador/a del sistema" },
      { Perfil.Coordinacion, "Coordinación médica" },
      { Perfil.Administrativo, "Personal administrativo / Operador de call center" },
      { Perfil.Medico, "Profesional médico/a" },
      { Perfil.Tecnico, "Técnico/a de estudios" },
      { Perfil.Guardia, "Personal de guardia médica / internación" },
      { Perfil.Insumos, "Responsable de insumos" },
      { Perfil.Jefatura, "Jefe/a administrativo/a" },
    };
  }
}
