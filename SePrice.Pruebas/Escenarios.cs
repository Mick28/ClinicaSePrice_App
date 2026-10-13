using SePrice.Negocio;
using static SePrice.Pruebas.Prueba;

namespace SePrice.Pruebas
{
  /// <summary>
  /// Recorridos de la demostración (especificación, sección 6).
  /// </summary>
  static partial class Escenarios
  {
    public static void Correr()
    {
      Escena0Administracion();
      Escena1PuestaEnMarcha();
      Escena2Turnos();
      Escena3Jornada();
      Escena4Recepcion();
      Escena5Atencion();
      Escena6Insumos();
      Escena7Gestion();
      Permisos53();
    }

    // Escena 0 · Administración (mruiz): usuarios, permisos y auditoría.
    static void Escena0Administracion()
    {
      Escena("0 · Administración");
      BaseNueva();
      Ingresar("mruiz");
      Si(Usuarios.Activos() == 9, "9 usuarios habilitados de ejemplo");
      Rechaza(() => Usuarios.Alta(new DatosUsuario { Nombre = "X",
        NombreUsuario = "lgomez", Perfil = Perfil.Administrativo,
        Clave = "12345678", Repetir = "12345678" }), "ya existe",
        "usuario repetido");
      int id = Usuarios.Alta(new DatosUsuario { Nombre = "Sosa, Eva",
        NombreUsuario = "esosa", Perfil = Perfil.Administrativo,
        Clave = "Clave2026", Repetir = "Clave2026" });
      Si(id > 0, "alta de usuario");
      Escena0Bajas();
    }

    // Bajas, permisos y acceso de usuarios deshabilitados.
    static void Escena0Bajas()
    {
      Usuarios.DarDeBaja(Usuarios.Buscar("ptest", out _).Id);
      Rechaza(() => Usuarios.DarDeBaja(Sesion.Actual.Id), "vos mismo",
        "el administrador no se da de baja a sí mismo");
      Rechaza(() => Sesion.Exigir("CU-G-04"), "no corresponde",
        "el administrador no da turnos");
      Sesion.Cerrar();
      Si(Sesion.Iniciar("ptest", "SePrice2026")
        == ResultadoIngreso.Deshabilitado, "usuario dado de baja no ingresa");
      Si(Sesion.Iniciar("mruiz", "otra") == ResultadoIngreso.Incorrecto,
        "contraseña incorrecta");
    }
  }
}
