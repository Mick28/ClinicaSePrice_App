using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SePrice.Negocio;

namespace SePrice.UI
{
  partial class PInicio
  {
    // Panel «Para hoy»: indicadores sólo del perfil (máximo 3).
    Control Indicadores()
    {
      Fila f = new Fila();
      foreach (object[] i in DelPerfil())
      {
        Tarjeta t = Ui.Indicador((string)i[0], i[1].ToString(), (string)i[2],
          (Color)i[3]);
        string destino = (string)i[4];
        if (destino != null) Enlazar(t, destino);
        f.Controls.Add(t);
      }
      return f;
    }

    void Enlazar(Control c, string destino)
    {
      c.Cursor = Cursors.Hand;
      c.Click += (s, e) => Ir(destino);
      foreach (Control h in c.Controls) Enlazar(h, destino);
    }

    static object[] I(string t, object v, string nota, Color c, string ir)
    {
      return new[] { t, v, nota, c, ir };
    }

    static object[][] Coordinacion()
    {
      return new[] { I("Agendas sin publicar", Agendas.SinPublicar(),
        "Próximos 7 días", Tema.Advertencia, "P-21"),
        I("Profesionales sin consultorio", Asignaciones.SinConsultorio(),
        "Asignalos antes de publicar", Tema.Primario, "P-20") };
    }

    IEnumerable<object[]> DelPerfil()
    {
      switch (perfil)
      {
        case Perfil.Administrador: return Admin();
        case Perfil.Coordinacion: return Coordinacion();
        case Perfil.Administrativo: return Mostrador();
        case Perfil.Medico: return Medico();
        case Perfil.Tecnico: return Tecnico();
        case Perfil.Guardia: return new[] { I("Solicitudes urgentes de " +
          "hoy", Demanda.MisSolicitudes().Rows.Count, "Enviadas por vos",
          Tema.Error, "P-12") };
        case Perfil.Insumos: return Insumos_();
        default: return Jefatura();
      }
    }

    static IEnumerable<object[]> Admin()
    {
      return new[] {
        I("Usuarios activos", Usuarios.Activos(), "Usuarios y roles",
          Tema.Primario, "P-03"),
        I("Agendas sin publicar", Agendas.SinPublicar(), "Próximos 7 días",
          Tema.Advertencia, "P-21"),
        I("Cambios registrados hoy", Auditar.CambiosDeHoy(), "Auditoría",
          Tema.Exito, "P-27") };
    }

    static IEnumerable<object[]> Mostrador()
    {
      return new[] {
        I("Turnos del día", Turnos.DelDia(), "Reservados y acreditados",
          Tema.Primario, "P-06"),
        I("Pacientes en espera", Turnos.EnEspera(), "Acreditados sin " +
          "llamar", Tema.Info, "P-10"),
        I("Alertas de stock", Insumos.BajoMinimo(), "Insumos bajo mínimo",
          Tema.Advertencia, "P-17") };
    }

    static IEnumerable<object[]> Medico()
    {
      Jornada j = Presentismo.Hoy();
      string estado = j.Ingreso == null ? "Sin registrar" : "Presente";
      return new[] {
        I("Mi presentismo", estado, j.TieneAgenda ? j.Lugar + " · " +
          j.Desde + " a " + j.Hasta : "Sin agenda hoy", j.Ingreso == null
          ? Tema.Advertencia : Tema.Exito, "P-22"),
        I("Pacientes en espera", Presentismo.PacientesEsperando(),
          "Acreditados", Tema.Info, "P-13") };
    }

    static IEnumerable<object[]> Tecnico()
    {
      List<EnEspera> cola = Atencion.ColaDeEstudios();
      return new[] {
        I("En cola", cola.Count, Atencion.MisSalas() ?? "Sin agenda hoy",
          Tema.Primario, "P-13"),
        I("Urgencias", cola.Count(c => c.Etiqueta == "Urgencia"),
          "Guardia e internación", Tema.Error, "P-13") };
    }

    static IEnumerable<object[]> Insumos_()
    {
      return new[] {
        I("Insumos bajo mínimo", Insumos.BajoMinimo(), "Alerta de " +
          "reposición", Tema.Advertencia, "P-17"),
        I("Pedidos enviados sin recibir", Insumos.SinRecibir(),
          "Al proveedor", Tema.Primario, "P-17") };
    }

    static IEnumerable<object[]> Jefatura()
    {
      return new[] {
        I("Atenciones del mes", Reportes.AtencionesDelMes(),
          "Atendidas y realizadas",
          Tema.Primario, "P-18"),
        I("Atenciones observadas", Honorarios.Observadas(), "Validación " +
          "pendiente: no se liquidan", Tema.Advertencia, "P-23") };
    }
  }
}
