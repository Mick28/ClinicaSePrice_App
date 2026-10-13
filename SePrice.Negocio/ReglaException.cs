using System;

namespace SePrice.Negocio
{
  /// <summary>
  /// Una regla de negocio impidió la operación. El mensaje está escrito
  /// para el usuario: dice qué pasó y qué hacer (especificación, 2.7).
  /// </summary>
  public class ReglaException : Exception
  {
    /// <summary>
    /// Campo de la pantalla al que se refiere el error, si hay.
    /// </summary>
    public string Campo { get; private set; }

    public ReglaException(string mensaje, string campo = null)
      : base(mensaje)
    {
      Campo = campo;
    }

    /// <summary>Lanza la excepción si la condición no se cumple.</summary>
    public static void Exigir(bool condicion, string mensaje,
      string campo = null)
    {
      if (!condicion) throw new ReglaException(mensaje, campo);
    }
  }
}
