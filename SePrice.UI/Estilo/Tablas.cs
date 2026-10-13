using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace SePrice.UI
{
  /// <summary>Convierte listas de objetos en tablas para las grillas.</summary>
  static class Tablas
  {
    /// <summary>
    /// Una columna por propiedad pública (tipos anónimos incluidos).
    /// </summary>
    public static DataTable De<T>(IEnumerable<T> filas)
    {
      DataTable t = new DataTable();
      PropertyInfo[] props = typeof(T).GetProperties();
      foreach (PropertyInfo p in props) t.Columns.Add(Nombre(p.Name));
      foreach (T f in filas)
      {
        object[] v = new object[props.Length];
        for (int i = 0; i < props.Length; i++) v[i] = props[i].GetValue(f);
        t.Rows.Add(v);
      }
      return t;
    }

    // «Último» → «Último ingreso»; el resto, igual.
    static string Nombre(string propiedad)
    {
      return propiedad == "Último" ? "Último ingreso" : propiedad;
    }
  }
}
