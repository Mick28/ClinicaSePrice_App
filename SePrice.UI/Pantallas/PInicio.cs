using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  /// <summary>
  /// P-01 Inicio: pantalla principal de cada perfil (CU-G-01). Muestra sólo
  /// sus accesos y, como mucho, tres indicadores del día (especificación).
  /// </summary>
  partial class PInicio : Pantalla
  {
    /// <summary>Parámetro de la variante «acceso no permitido».</summary>
    public static readonly object AccesoDenegado = new object();

    Perfil perfil;

    protected override string TituloVisible
    {
      get
      {
        string saludo = Reloj.Ahora.Hour < 13 ? "Buen día" : Reloj.Ahora.Hour
          < 20 ? "Buenas tardes" : "Buenas noches";
        return saludo + ", " + Sesion.Actual.PrimerNombre;
      }
    }

    protected override void ConstruirNormal()
    {
      perfil = Sesion.Actual.Perfil;
      if (perfil == Perfil.Administrativo) Presentismo.ControlarAusencias();
      Contenido.Controls.Add(Ui.Nota(Formato.Larga(Reloj.Hoy) + " · " +
        Permisos.Nombres[perfil] + ". " + Tarea()));
      if (Parametro == AccesoDenegado) Denegado();
      Avisos();
      Contenido.Controls.Add(Indicadores());
      Contenido.Controls.Add(Ui.Subtitulo("Accesos rápidos"));
      Contenido.Controls.Add(Accesos());
      if (perfil == Perfil.Administrador) UltimosCambios();
    }

    // Lo que hace cada perfil (cuaderno, 4.4).
    string Tarea()
    {
      switch (perfil)
      {
        case Perfil.Administrador: return "Configurás el sistema y revisás " +
          "la operación en sólo lectura desde Auditoría.";
        case Perfil.Coordinacion: return "Configurás especialidades, " +
          "parámetros, catálogo y agendas.";
        case Perfil.Administrativo: return "Tu mostrador del día.";
        case Perfil.Medico: return "Tu jornada en consultorios externos.";
        case Perfil.Tecnico: return "Tu cola de estudios de hoy.";
        case Perfil.Guardia: return "Pedidos de estudios urgentes.";
        case Perfil.Insumos: return "Stock y pedidos al proveedor.";
        default: return "Indicadores de gestión, honorarios y datos de base.";
      }
    }

    void Denegado()
    {
      Contenido.Controls.Add(Ui.Aviso(TipoAviso.Error, "Esta función no " +
        "corresponde a tu perfil. Te llevamos a tu Inicio."));
    }

    void Avisos()
    {
      if (perfil == Perfil.Administrador)
        Contenido.Controls.Add(Ui.Aviso(TipoAviso.Info, "Modo de " +
          "configuración: este perfil no tiene funciones de atención " +
          "diaria (turnos, acreditación, atención, liquidación); las ve " +
          "sólo en modo auditoría. Matriz de permisos, cuaderno 5.3."));
      if (perfil == Perfil.Medico && SinIngreso())
        Contenido.Controls.Add(Ui.Aviso(TipoAviso.Advertencia, "Todavía " +
          "no registraste tu ingreso. La sala de espera se habilita cuando " +
          "lo hagas (Mi presentismo)."));
      if (perfil == Perfil.Administrativo) Ausentes();
    }

    // Agendas con «profesional ausente»: acceso a los turnos afectados.
    void Ausentes()
    {
      string aus = Presentismo.AusentesHoy();
      if (aus == null) return;
      Contenido.Controls.Add(Ui.Aviso(TipoAviso.Advertencia, "Agenda " +
        "marcada «profesional ausente»: " + aus + ". Hay turnos para " +
        "reprogramar o cancelar."));
      Contenido.Controls.Add(Ui.BotonesIzq(Ui.Boton("Ver turnos afectados",
        TipoBoton.Secundario, (s, e) => Ir("P-09", PReprogramar
        .VerAfectados))));
    }

    static bool SinIngreso()
    {
      Jornada j = Presentismo.Hoy();
      return j.TieneAgenda && j.Ingreso == null;
    }
  }
}
