using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace SePrice.Datos
{
  /// <summary>
  /// Ejecuta sentencias SQL sobre la conexión de la sesión. Los valores se
  /// pasan como parámetros (@p0, @p1, ...), nunca concatenados al texto.
  /// </summary>
  public static class Consulta
  {
    static DbTransaction transaccion;

    /// <summary>Ejecuta INSERT, UPDATE o DELETE y devuelve las filas.</summary>
    public static int Ejecutar(string sql, params object[] valores)
    {
      using (DbCommand cmd = Crear(sql, valores))
        return cmd.ExecuteNonQuery();
    }

    /// <summary>Ejecuta un INSERT y devuelve el id generado.</summary>
    public static int Insertar(string sql, params object[] valores)
    {
      Ejecutar(sql, valores);
      return Entero("SELECT last_insert_rowid();");
    }

    /// <summary>Devuelve el primer valor del resultado (o null).</summary>
    public static object Escalar(string sql, params object[] valores)
    {
      using (DbCommand cmd = Crear(sql, valores))
      {
        object v = cmd.ExecuteScalar();
        return v == DBNull.Value ? null : v;
      }
    }

    /// <summary>Devuelve un entero (0 si el resultado es vacío).</summary>
    public static int Entero(string sql, params object[] valores)
    {
      object v = Escalar(sql, valores);
      return v == null ? 0 : Convert.ToInt32(v);
    }

    /// <summary>Devuelve un texto (null si el resultado es vacío).</summary>
    public static string Texto(string sql, params object[] valores)
    {
      object v = Escalar(sql, valores);
      return v == null ? null : Convert.ToString(v);
    }

    /// <summary>Devuelve el resultado completo como tabla.</summary>
    public static DataTable Tabla(string sql, params object[] valores)
    {
      using (DbCommand cmd = Crear(sql, valores))
      using (DbDataReader lector = cmd.ExecuteReader())
      {
        DataTable tabla = new DataTable();
        for (int i = 0; i < lector.FieldCount; i++)
          tabla.Columns.Add(lector.GetName(i), typeof(object));
        object[] fila = new object[lector.FieldCount];
        while (lector.Read())
        {
          lector.GetValues(fila);
          tabla.Rows.Add(fila);
        }
        return tabla;
      }
    }

    /// <summary>
    /// Convierte cada fila del resultado con la función dada.
    /// </summary>
    public static List<T> Lista<T>(string sql, Func<Fila, T> convertir,
      params object[] valores)
    {
      List<T> lista = new List<T>();
      foreach (DataRow r in Tabla(sql, valores).Rows)
        lista.Add(convertir(new Fila(r)));
      return lista;
    }

    /// <summary>Primera fila convertida, o el valor por defecto.</summary>
    public static T Uno<T>(string sql, Func<Fila, T> convertir,
      params object[] valores)
    {
      List<T> lista = Lista(sql, convertir, valores);
      return lista.Count == 0 ? default(T) : lista[0];
    }

    /// <summary>
    /// Ejecuta varias operaciones como una sola (todo o nada).
    /// </summary>
    public static void EnTransaccion(Action operaciones)
    {
      if (transaccion != null) { operaciones(); return; }
      transaccion = Conexion.Actual.BeginTransaction();
      try
      {
        operaciones();
        transaccion.Commit();
      }
      catch { transaccion.Rollback(); throw; }
      finally { transaccion.Dispose(); transaccion = null; }
    }

    /// <summary>Ejecuta un script con varias sentencias.</summary>
    public static void EjecutarScript(string script)
    {
      Ejecutar(script);
    }

    internal static void EjecutarSinSesion(string sql)
    {
      Ejecutar(sql);
    }

    static DbCommand Crear(string sql, object[] valores)
    {
      DbCommand cmd = Conexion.Actual.CreateCommand();
      cmd.CommandText = sql;
      cmd.Transaction = transaccion;
      for (int i = 0; i < valores.Length; i++)
        Agregar(cmd, "@p" + i, valores[i]);
      return cmd;
    }

    static void Agregar(DbCommand cmd, string nombre, object valor)
    {
      DbParameter p = cmd.CreateParameter();
      p.ParameterName = nombre;
      if (valor is bool) valor = (bool)valor ? 1 : 0;
      p.Value = valor ?? DBNull.Value;
      cmd.Parameters.Add(p);
    }
  }
}
