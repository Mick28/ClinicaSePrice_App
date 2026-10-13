using System;

namespace SePrice.Pruebas
{
  /// <summary>
  /// Pruebas de los recorridos de la demostración (especificación, 6) sobre
  /// una base nueva con los datos de ejemplo. Muestra OK o FALLA por regla.
  /// </summary>
  static class Program
  {
    static int Main(string[] args)
    {
      Console.OutputEncoding = System.Text.Encoding.UTF8;
      Escenarios.Correr();
      Console.WriteLine();
      Console.WriteLine("Pruebas: {0} OK · {1} FALLAS", Prueba.Ok,
        Prueba.Fallas);
      return Prueba.Fallas == 0 ? 0 : 1;
    }
  }
}
