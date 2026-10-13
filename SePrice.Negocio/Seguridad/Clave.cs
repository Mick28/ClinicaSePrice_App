using System;
using System.Security.Cryptography;
using System.Text;

namespace SePrice.Negocio
{
  /// <summary>
  /// Contraseñas (RNF-01): nunca se guardan en texto. Se guarda la sal y el
  /// resultado de PBKDF2-SHA256 en 64 caracteres (USUARIO.contrasena).
  /// </summary>
  public static class Clave
  {
    const int Iteraciones = 10000;

    /// <summary>Cifra una contraseña nueva para el usuario.</summary>
    public static string Cifrar(string usuario, string clave)
    {
      byte[] sal = Sal(usuario + ":" + Guid.NewGuid());
      return Codificar(sal, clave);
    }

    /// <summary>Compara la contraseña ingresada con la guardada.</summary>
    public static bool Coincide(string clave, string guardada)
    {
      if (string.IsNullOrEmpty(guardada)) return false;
      byte[] todo = Convert.FromBase64String(guardada);
      byte[] sal = new byte[16];
      Array.Copy(todo, sal, 16);
      return Codificar(sal, clave) == guardada;
    }

    static string Codificar(byte[] sal, string clave)
    {
      using (Rfc2898DeriveBytes k = new Rfc2898DeriveBytes(clave, sal,
        Iteraciones, HashAlgorithmName.SHA256))
      {
        byte[] todo = new byte[48];
        Array.Copy(sal, todo, 16);
        Array.Copy(k.GetBytes(32), 0, todo, 16, 32);
        return Convert.ToBase64String(todo);
      }
    }

    static byte[] Sal(string semilla)
    {
      using (SHA256 sha = SHA256.Create())
      {
        byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes(semilla));
        byte[] sal = new byte[16];
        Array.Copy(h, sal, 16);
        return sal;
      }
    }
  }
}
