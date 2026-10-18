using System;
using System.Collections.Generic;
using System.IO;

namespace SePrice.Negocio
{
  /// <summary>
  /// Lee «SePrice.config.txt» (clave=valor), junto al programa.
  /// Claves: Base, ModoDemo, FechaDemo, HoraDemo.
  /// </summary>
  public static class Configuracion
  {
    static Dictionary<string, string> valores =
      new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Archivo de la base de datos.</summary>
    public static string Base
    {
      get { return Valor("Base", "SePrice.db"); }
    }

    /// <summary>Demostración: datos de ejemplo y accesos de prueba.</summary>
    public static bool ModoDemo
    {
      get { return Valor("ModoDemo", "si").StartsWith("s"); }
    }

    /// <summary>
    /// Muestra los códigos de caso de uso (CU-G-xx) y de circuito en las
    /// pantallas. Si no está en el archivo, no se muestran.
    /// </summary>
    public static bool MostrarCodigos
    {
        get { return Valor("MostrarCodigos", "no").StartsWith("s"); }
    }

    /// <summary>Carga el archivo de configuración, si existe.</summary>
    public static void Cargar(string carpeta)
    {
      string ruta = Path.Combine(carpeta, "SePrice.config.txt");
      if (!File.Exists(ruta)) return;
      foreach (string linea in File.ReadAllLines(ruta))
        Leer(linea);
      Reloj.Configurar(Valor("FechaDemo", ""), Valor("HoraDemo", ""));
    }

    /// <summary>Asigna un valor (lo usan las pruebas).</summary>
    public static void Fijar(string clave, string valor)
    {
      valores[clave] = valor;
    }

    static void Leer(string linea)
    {
      string l = linea.Trim();
      int igual = l.IndexOf('=');
      if (l.StartsWith("#") || igual <= 0) return;
      valores[l.Substring(0, igual).Trim()] = l.Substring(igual + 1).Trim();
    }

    static string Valor(string clave, string porDefecto)
    {
      string v;
      return valores.TryGetValue(clave, out v) ? v : porDefecto;
    }
  }
}
