using System;
using System.Data;

namespace SePrice.Datos
{
  /// <summary>
  /// Lectura cómoda de una fila del resultado de una consulta.
  /// </summary>
  public class Fila
  {
    readonly DataRow fila;

    public Fila(DataRow fila)
    {
      this.fila = fila;
    }

    /// <summary>Indica si la columna está vacía (NULL).</summary>
    public bool Vacio(string columna)
    {
      return fila[columna] == DBNull.Value;
    }

    public int Entero(string columna)
    {
      return Vacio(columna) ? 0 : Convert.ToInt32(fila[columna]);
    }

    public int? EnteroONulo(string columna)
    {
      return Vacio(columna) ? (int?)null : Convert.ToInt32(fila[columna]);
    }

    public decimal Importe(string columna)
    {
      return Vacio(columna) ? 0m : Convert.ToDecimal(fila[columna]);
    }

    public string Texto(string columna)
    {
      return Vacio(columna) ? null : Convert.ToString(fila[columna]);
    }

    public bool Logico(string columna)
    {
      return Entero(columna) != 0;
    }
  }
}
