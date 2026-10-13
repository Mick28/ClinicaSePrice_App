using System;
using System.IO;
using System.Windows.Forms;
using SePrice.Datos;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// Punto de entrada del sistema de gestión de Clínica SePrice.
  /// </summary>
  static class Program
  {
    [STAThread]
    static void Main()
    {
#if !MONO
      // Las pantallas usan medidas fijas: con pantallas al 125 % o 150 %
      // Windows escala la ventana completa y el texto se ve nítido.
      Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);
#endif
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.ThreadException += (s, e) => Fallo(e.Exception);
      Preparar();
      FrmLogin login = new FrmLogin();
#if MONO
      if (Captura.Activa) login.Shown += (s, e) => Captura.Correr(login);
#endif
      Application.Run(login);
    }

    // Error no previsto: se registra en SePrice_errores.log y se avisa
    // sin jerga técnica (especificación 2.7).
    static void Fallo(Exception e)
    {
      File.AppendAllText(Path.Combine(Application.StartupPath,
        "SePrice_errores.log"), DateTime.Now + " " + e + "\n\n");
#if MONO
      Console.WriteLine("ERROR " + e);
#endif
      Dialogo.Error("Ocurrió un problema inesperado y la operación no se " +
        "completó. Si se repite, avisá a la administración del sistema.");
    }

    // Lee la configuración y, la primera vez, crea la base de datos.
    static void Preparar()
    {
      string carpeta = Application.StartupPath;
      Configuracion.Cargar(carpeta);
      string ruta = Path.IsPathRooted(Configuracion.Base)
        ? Configuracion.Base : Path.Combine(carpeta, Configuracion.Base);
      Configuracion.Fijar("Base", ruta);
      BaseDatos.Preparar(ruta, Configuracion.ModoDemo);
    }
  }
}
