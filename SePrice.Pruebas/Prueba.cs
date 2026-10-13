using System;
using System.IO;
using SePrice.Datos;
using SePrice.Negocio;

namespace SePrice.Pruebas
{
  /// <summary>Herramientas mínimas de prueba (sin paquetes externos).</summary>
  static class Prueba
  {
    public static int Ok, Fallas;

    /// <summary>
    /// Base nueva con los datos de ejemplo y el reloj de la demo.
    /// </summary>
    public static void BaseNueva(string hora = "08:50")
    {
      Sesion.Cerrar();
      string ruta = Path.Combine(Path.GetTempPath(), "SePrice_prueba.db");
      if (File.Exists(ruta)) File.Delete(ruta);
      Configuracion.Fijar("Base", ruta);
      BaseDatos.Preparar(ruta, true);
      Reloj.Configurar("2026-10-15", hora);
    }

    /// <summary>Inicia sesión con la contraseña de la demostración.</summary>
    public static void Ingresar(string usuario)
    {
      Sesion.Cerrar();
      ResultadoIngreso r = Sesion.Iniciar(usuario, "SePrice2026");
      Si(r == ResultadoIngreso.Correcto, "ingresa " + usuario);
    }

    /// <summary>Comprueba una condición.</summary>
    public static void Si(bool condicion, string regla)
    {
      if (condicion) Ok++;
      else Fallas++;
      Console.WriteLine((condicion ? "  OK     " : "  FALLA  ") + regla);
    }

    /// <summary>
    /// Comprueba que la operación sea rechazada con ese mensaje.
    /// </summary>
    public static void Rechaza(Action op, string parteDelMensaje, string regla)
    {
      try
      {
        op();
        Si(false, regla + " (no se rechazó)");
      }
      catch (ReglaException e)
      {
        Si(e.Message.Contains(parteDelMensaje), regla + " → " + e.Message);
      }
    }

    /// <summary>Título de un recorrido.</summary>
    public static void Escena(string titulo)
    {
      Console.WriteLine();
      Console.WriteLine("== " + titulo);
    }
  }
}
