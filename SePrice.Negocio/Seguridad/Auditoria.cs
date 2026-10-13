using SePrice.Datos;

namespace SePrice.Negocio
{
  /// <summary>
  /// Registro único de auditoría (RNF-03, entidad REGISTRO DE AUDITORÍA):
  /// cada alta, baja o modificación, y cada consulta o exportación hecha
  /// desde la auditoría, queda con el usuario, la fecha y la hora.
  /// </summary>
  public static class Auditoria
  {
    /// <summary>Registra una operación del usuario de la sesión.</summary>
    public static void Registrar(string circuito, string modulo,
      string accion, string entidad, object idRegistro,
      string anterior = null, string nuevo = null)
    {
      if (Sesion.Actual == null) return;
      Consulta.Ejecutar("INSERT INTO registro_de_auditoria (id_usuario, " +
        "fecha_hora, circuito, modulo, accion, entidad, id_registro, " +
        "valor_anterior, valor_nuevo) VALUES " +
        "(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)",
        Sesion.Actual.Id, Formato.IsoHora(Reloj.Ahora), circuito, modulo,
        accion, entidad, idRegistro == null ? null : idRegistro.ToString(),
        Recortar(anterior), Recortar(nuevo));
    }

    /// <summary>Atajo para un alta.</summary>
    public static void Alta(string circuito, string modulo, string entidad,
      object id, string nuevo)
    {
      Registrar(circuito, modulo, "alta", entidad, id, null, nuevo);
    }

    /// <summary>Atajo para una modificación.</summary>
    public static void Cambio(string circuito, string modulo,
      string entidad, object id, string anterior, string nuevo)
    {
      Registrar(circuito, modulo, "modificación", entidad, id, anterior,
        nuevo);
    }

    // valor_anterior y valor_nuevo: Alfanumérico 500 (diccionario).
    static string Recortar(string texto)
    {
      if (texto == null || texto.Length <= 500) return texto;
      return texto.Substring(0, 497) + "...";
    }
  }
}
