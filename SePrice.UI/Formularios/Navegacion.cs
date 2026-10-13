using System;
using System.Collections.Generic;
using System.Linq;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>Una pantalla del mapa (especificación 3.3).</summary>
  class DefPantalla
  {
    public string Codigo, Nombre, Menu, Cu, Circuito;
    public Func<Pantalla> Crear;
  }

  /// <summary>Ítem del menú lateral de un perfil.</summary>
  class ItemMenu
  {
    public string Seccion, Codigo, Texto;
  }

  /// <summary>
  /// Mapa de pantallas y menú por perfil (especificación 3.2 y 3.3). El
  /// menú es exhaustivo: un perfil no ve nada fuera de su lista. Cada ítem
  /// corresponde a un proceso que el perfil ejecuta en la matriz 5.3.
  /// </summary>
  static partial class Navegacion
  {
    public const string Comun = "COMÚN", A = "CONSULTORIOS EXTERNOS",
      B = "ESTUDIOS CLÍNICOS";

    static readonly Dictionary<Perfil, string[]> menus =
      new Dictionary<Perfil, string[]>
    {
      { Perfil.Administrador, new[] { "P-03", "P-27", "P-04", "P-19",
        "P-20", "P-21", "P-24", "P-25", "P-26" } },
      { Perfil.Coordinacion, new[] { "P-04", "P-19", "P-20", "P-21",
        "P-24", "P-25", "P-26" } },
      { Perfil.Administrativo, new[] { "P-05", "P-06", "P-10", "P-17",
        "P-07", "P-11", "P-16" } },
      { Perfil.Medico, new[] { "P-13", "P-22" } },
      { Perfil.Tecnico, new[] { "P-13" } },
      { Perfil.Guardia, new[] { "P-12" } },
      { Perfil.Insumos, new[] { "P-17" } },
      { Perfil.Jefatura, new[] { "P-18", "P-04", "P-23" } },
    };

    // Pantallas que se abren desde otra (no son ítems de menú).
    static readonly Dictionary<string, string[]> hijas =
      new Dictionary<string, string[]>
    {
      { "P-06", new[] { "P-08", "P-09", "P-07" } },
      { "P-07", new[] { "P-08" } }, { "P-09", new[] { "P-08" } },
      { "P-13", new[] { "P-14", "P-15" } },
    };

    /// <summary>Ítems del menú del perfil, con su sección.</summary>
    public static List<ItemMenu> Menu(Perfil p)
    {
      return menus[p].Select(c => Pantallas[c]).Select(d => new ItemMenu
      { Codigo = d.Codigo, Texto = TextoMenu(p, d), Seccion = d.Circuito ==
        "A" ? A : d.Circuito == "B" ? B : Comun }).ToList();
    }

    // Mismo ítem, nombre propio del perfil (especificación 3.2).
    static string TextoMenu(Perfil p, DefPantalla d)
    {
      if (p == Perfil.Tecnico && d.Codigo == "P-13") return "Cola de atención";
      if (p == Perfil.Insumos && d.Codigo == "P-17") return "Insumos y stock";
      if (p == Perfil.Coordinacion && d.Codigo == "P-04")
        return "Especialidades";
      return d.Menu;
    }

    /// <summary>
    /// ¿El perfil puede abrir la pantalla? En modo auditoría, sólo el
    /// Administrador/a y sólo las pantallas de procesos que audita.
    /// </summary>
    public static bool Permitida(Perfil p, string codigo, bool auditoria)
    {
      if (codigo == "P-01") return true;
      DefPantalla d;
      if (!Pantallas.TryGetValue(codigo, out d)) return false;
      if (auditoria) return Permisos.Audita(p, d.Cu);
      if (!Permisos.Ejecuta(p, d.Cu)) return false;
      return menus[p].Contains(codigo) || menus[p].Any(m =>
        hijas.ContainsKey(m) && hijas[m].Contains(codigo));
    }

    /// <summary>
    /// Módulos que el Administrador/a abre en sólo lectura (P-27).
    /// </summary>
    public static readonly string[][] ModulosAuditables =
    {
      new[] { "Pacientes y turnos", "ambos", "P-05", "P-06", "P-09" },
      new[] { "Demanda sin turno", "ambos", "P-07", "P-11", "P-12" },
      new[] { "Acreditación y cobertura", "ambos", "P-10" },
      new[] { "Atención", "ambos", "P-13" },
      new[] { "Resultados", "B", "P-16" },
      new[] { "Insumos", "ambos", "P-17" },
      new[] { "Reportes", "ambos", "P-18" },
      new[] { "Presentismo", "A", "P-22" },
      new[] { "Liquidaciones", "A", "P-23" },
    };
  }
}
