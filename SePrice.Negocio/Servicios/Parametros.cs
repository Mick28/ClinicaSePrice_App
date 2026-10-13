using System;
using System.Collections.Generic;
using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>Un parámetro del sistema (entidad PARÁMETRO).</summary>
  public class Parametro
  {
    public string Clave, Descripcion, Valor, Unidad, Circuito, Vigencia;
    public bool AConfirmar;
    public string UltimaModificacion;
  }

  /// <summary>
  /// CU-G-03 Configurar agendas, catálogos y parámetros: parámetros de
  /// agenda (CU-A-03, P-19) y franjas de laboratorio (CU-B-04, P-25).
  /// </summary>
  public static class Parametros
  {
    const string Cu = "CU-G-03";

    /// <summary>Valor de un parámetro.</summary>
    public static string Valor(string clave)
    {
      return Consulta.Texto("SELECT valor FROM parametro WHERE clave = @p0",
        clave);
    }

    /// <summary>Valor numérico de un parámetro.</summary>
    public static int Numero(string clave)
    {
      return int.Parse(Valor(clave));
    }

    /// <summary>
    /// Parámetros de un circuito, con su última modificación.
    /// </summary>
    public static List<Parametro> DelCircuito(string circuito)
    {
      return Consulta.Lista("SELECT p.*, (SELECT MAX(a.fecha_hora) || ' · '" +
        " || u.nombre_usuario FROM registro_de_auditoria a JOIN usuario u " +
        "ON u.id_usuario = a.id_usuario WHERE a.entidad = 'PARÁMETRO' AND " +
        "a.id_registro = p.clave) AS ultima FROM parametro p WHERE " +
        "p.circuito = @p0 ORDER BY p.clave", Leer, circuito);
    }

    /// <summary>Cambia el valor de un parámetro (queda auditado).</summary>
    public static void Cambiar(string clave, string valor)
    {
      Sesion.Exigir(Cu);
      Validar(clave, valor);
      string anterior = Valor(clave);
      Consulta.Ejecutar("UPDATE parametro SET valor = @p0, vigencia_desde " +
        "= @p1 WHERE clave = @p2", valor, Formato.Iso(Reloj.Hoy), clave);
      Auditoria.Cambio(Circuito(clave), "Parámetros (CU-G-03)",
        "PARÁMETRO", clave, anterior, valor);
    }

    static string Circuito(string clave)
    {
      return Consulta.Texto("SELECT circuito FROM parametro WHERE clave = " +
        "@p0", clave);
    }

    static void Validar(string clave, string valor)
    {
      if (clave.StartsWith("lab_con") || clave.StartsWith("lab_sin"))
      {
        Franja(valor);
        return;
      }
      if (clave == "lab_dias") return;
      int n;
      ReglaException.Exigir(int.TryParse(valor, out n) && n > 0,
        "Ingresá un número mayor que cero.", "valor");
      ReglaException.Exigir(clave != "duracion_minima_general" || n >= 15,
        "La duración no puede ser menor a 15 minutos.", "valor");
    }

    /// <summary>
    /// Lee una franja «07:00-11:00» y valida que sea correcta.
    /// </summary>
    public static int[] Franja(string valor)
    {
      string[] p = (valor ?? "").Split('-');
      TimeSpan d, h;
      ReglaException.Exigir(p.Length == 2 && TimeSpan.TryParse(p[0], out d)
        && TimeSpan.TryParse(p[1], out h), "Ingresá la franja como " +
        "07:00-11:00.", "valor");
      int desde = Formato.Minutos(p[0].Trim());
      int hasta = Formato.Minutos(p[1].Trim());
      ReglaException.Exigir(hasta > desde, "La hora de fin tiene que ser " +
        "posterior a la de inicio.", "valor");
      return new[] { desde, hasta };
    }

    /// <summary>
    /// Duración mínima de una especialidad: nunca menor a la general ni a
    /// los mínimos relevados (25 fisio-kinesiología, 30 salud mental).
    /// </summary>
    public static void ValidarDuracion(string especialidad, int minutos)
    {
      int minimo = Minimo(especialidad);
      ReglaException.Exigir(minutos >= minimo, "Para " + especialidad +
        " la duración mínima es de " + minimo + " minutos.", "duracion");
    }

    static int Minimo(string especialidad)
    {
      string e = especialidad.ToLowerInvariant();
      if (e.Contains("salud mental")) return 30;
      if (e.Contains("kinesio")) return 25;
      return Numero("duracion_minima_general");
    }

    /// <summary>Cambia la duración de una especialidad (P-19).</summary>
    public static void CambiarDuracion(int idEspecialidad, int minutos)
    {
      Sesion.Exigir(Cu);
      Fila e = Consulta.Uno("SELECT * FROM especialidad WHERE " +
        "id_especialidad = @p0", f => f, idEspecialidad);
      ValidarDuracion(e.Texto("nombre"), minutos);
      Consulta.Ejecutar("UPDATE especialidad SET duracion_minima = @p0 " +
        "WHERE id_especialidad = @p1", minutos, idEspecialidad);
      Auditoria.Cambio("A", "Parámetros (CU-G-03)", "ESPECIALIDAD",
        idEspecialidad, "duracion_minima: " + e.Texto("duracion_minima"),
        "duracion_minima: " + minutos);
    }

    static Parametro Leer(Fila f)
    {
      return new Parametro
      {
        Clave = f.Texto("clave"), Descripcion = f.Texto("descripcion"),
        Valor = f.Texto("valor"), Unidad = f.Texto("unidad"),
        Circuito = f.Texto("circuito"), Vigencia = f.Texto("vigencia_desde"),
        AConfirmar = f.Logico("a_confirmar"),
        UltimaModificacion = f.Texto("ultima")
      };
    }
  }
}
