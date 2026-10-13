using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Elemento de una lista desplegable: id y texto visible.</summary>
  public class Opcion
  {
    public int Id;
    public string Texto;

    public Opcion(int id, string texto)
    {
      Id = id;
      Texto = texto;
    }

    /// <summary>Lo que muestra el combo.</summary>
    public override string ToString() { return Texto; }

    /// <summary>Lee las columnas «id» y «texto» de una consulta.</summary>
    public static Opcion Leer(Fila f)
    {
      return new Opcion(f.Entero("id"), f.Texto("texto"));
    }
  }
}
