# Clínica SePrice · Sistema de Gestión Administrativa (C#)

Etapa de programación del proyecto de **PP2 – Desarrollo de Sistemas de Información
Orientados a la Gestión y Apoyo a las Decisiones** (Comisión D, Prof. Silvia Cañizares,
Grupo 5). El sistema implementa los **18 procesos generales (CU-G-01 a CU-G-18)** del
*Cuaderno de Especificaciones v2.1*, para los dos circuitos (A · Consultorios externos y
B · Estudios clínicos), con la matriz de perfiles y permisos de la sección 5.3.

| Requisito técnico (cuaderno 6.2) | Cómo se cumple |
|---|---|
| RT-01 Arquitectura | Aplicación de escritorio Windows Forms sobre .NET 8, en C#, en tres capas |
| RT-03 Software del cliente | Windows 10/11 con .NET 8 Desktop Runtime; se instala copiando la carpeta |
| RT-04 Servidor y datos | SQLite (archivo `SePrice.db`); el motor está aislado en `SePrice.Datos/Proveedor.cs` |
| RT-06 Seguridad técnica | Contraseñas con hash y sal (PBKDF2), registro de sesiones y de auditoría |

## Cómo abrirlo y ejecutarlo

1. Requisitos: **Windows 10 u 11** y **Visual Studio 2022** (17.8 o posterior) con la carga
   de trabajo *Desarrollo de escritorio de .NET* (incluye el SDK de .NET 8).
2. Abrir `ClinicaSePrice.sln`. La primera compilación descarga de NuGet el paquete
   `Microsoft.Data.Sqlite` (hace falta internet esa vez).
3. Proyecto de inicio: **SePrice.UI** (ya figura primero en la solución). Ejecutar con F5.
4. La primera vez se crea `SePrice.db` junto al programa, con el esquema y los datos de
   ejemplo. Para empezar de cero, borrar ese archivo.

Desde la consola: `dotnet run --project SePrice.UI` (sistema) y
`dotnet run --project SePrice.Pruebas` (pruebas de aceptación).

### Usuarios de prueba (contraseña de todos: `SePrice2026`)

| Usuario | Perfil | Qué mostrar |
|---|---|---|
| `mruiz` | Administrador/a del sistema | Usuarios, datos de base, configuración, auditoría en sólo lectura |
| `cmendez` | Coordinación médica | Parámetros, asignación de consultorios, publicación de agendas, catálogo |
| `lgomez` | Personal administrativo / Operador de call center | Legajos, turnos, sobreturnos, acreditación, laboratorio, entregas, insumos |
| `sibarra` | Profesional médico/a | Presentismo, sala de espera, atención y turno de control |
| `tvera` | Técnico/a de estudios | Cola de atención (urgencias primero), estudio y consumo de insumos |
| `nrios` | Personal de guardia médica / internación | Solicitud de estudio urgente |
| `dacosta` | Responsable de insumos | Stock, pedido propuesto al proveedor, recepción |
| `gfunes` | Jefe/a administrativo/a | Reportes, liquidación de honorarios, valores de honorarios |
| `ptest` | Personal administrativo | Usuario para probar bajas y restablecer contraseña |

### Configuración (`SePrice.config.txt`, junto al programa)

- `Base=SePrice.db` · archivo de la base de datos.
- `ModoDemo=si` · carga los datos de ejemplo y muestra en el inicio de sesión los accesos
  rápidos de prueba y el reloj de la demostración. **En producción: `ModoDemo=no`.**
- `FechaDemo=2026-10-15` y `HoraDemo=08:50` · el reloj arranca el jueves 15/10/2026 a las
  08:50, como en las escenas de la especificación, y avanza en tiempo real. Desde el login
  se puede cambiar la hora para mostrar, por ejemplo, la llegada tarde o el laboratorio
  fuera de franja. Borrar las dos líneas para usar la fecha y hora reales.

## Estructura de la solución

```
ClinicaSePrice.sln
├─ SePrice.Datos     Conexión única, consultas parametrizadas, creación de la base
│   └─ Sql/          esquema.sql (29 tablas) y datos_ejemplo.sql · GENERADOS del modelo
├─ SePrice.Negocio   Reglas, sesión, permisos (matriz 5.3), auditoría y servicios por CU
│   ├─ Seguridad/    Sesion, Clave, Permisos y Permisos.generado.cs (GENERADO de la 5.3)
│   └─ Servicios/    Un servicio por proceso general (Turnos, Acreditacion, Honorarios…)
├─ SePrice.UI        Windows Forms: login, inicio por perfil y pantallas P-01 a P-27
│   ├─ Formularios/  FrmLogin, FrmPrincipal (marco, menú por perfil), Navegacion
│   ├─ Pantallas/    Una clase por pantalla de la especificación de interfaz
│   ├─ Dialogos/     Los cuatro diálogos: Informar, Confirmar, Ingresar y Error
│   └─ Estilo/       Colores, tipografía, campos, tablas, avisos y botones (spec 2.x)
├─ SePrice.Pruebas   Pruebas de aceptación: las 8 escenas de la demostración + matriz 5.3
├─ herramientas/     codigo_limpio.py y scripts de verificación en Linux (Mono)
└─ Capturas/         25 capturas del sistema funcionando, con los datos de ejemplo
```

Las capas sólo se llaman hacia abajo (UI → Negocio → Datos). Ninguna pantalla toca la
base: todo pasa por un servicio de negocio, que primero controla el permiso del perfil
(`Sesion.Exigir("CU-G-xx")`) y registra la auditoría.

## Recomendaciones de la cátedra aplicadas (material de la Etapa de programación)

- **La conexión es «la puerta» de la sesión**: se abre al iniciar sesión y se cierra al
  cerrarla (`Conexion.Abrir` / `Conexion.Cerrar`). Ninguna pantalla abre conexiones.
- **Un único procedimiento público de cierre de sesión** (`Sesion.Cerrar`, CU-G-15) para
  todos los perfiles: registra la hora de cierre y cierra la conexión. El botón
  «Cerrar sesión» está en el encabezado de todas las pantallas y abre la ventana
  «¿Querés cerrar la sesión?» (`DlgCerrarSesion`): quién tiene la sesión, desde cuándo,
  aviso si la pantalla tiene datos sin guardar («Cerrar sesión igual»), Esc para volver
  a trabajar y Enter para confirmar.
- **Cada perfil ve sólo sus funciones** desde el inicio de sesión (menú y accesos
  rápidos generados de la matriz 5.3). Si alguien llega a una pantalla ajena, vuelve a su
  Inicio con un aviso; el Administrador/a abre los módulos operativos en sólo lectura.
- **Código limpio**: líneas de hasta 80 caracteres y funciones de hasta 15 líneas, con
  comentarios. Se controla con `python3 herramientas/codigo_limpio.py .`
  (resultado actual: **0 observaciones** en los 105 archivos .cs). El mismo script
  controla que ningún control use `MaximumSize` con alto 0: en .NET eso lo deja con
  alto 0 (invisible), aunque en otras plataformas se vea bien. Para limitar sólo el
  ancho se usa `Ui.Tope(ancho)`.
- **Prefijos de controles**: `txt`, `cbo`, `chk`, `opt`, `dtp`, `num`, `lbl`, `tbl`, `btn`.
- **Botón «Blanquear»** en cada formulario de carga.
- **Validación en la búsqueda**: el DNI se ingresa una sola vez; el paciente queda «en
  curso» (barra superior) para el turno, el comprobante y la acreditación.
- **Diálogos estándar**: Informar, Confirmar («¿Los datos ingresados son correctos?»),
  Ingresar y Error. Los errores de regla se muestran junto al campo que los causa.

## Pantallas y procesos

| Pantalla | Nombre (= caso de uso) | Proceso | Circuito |
|---|---|---|---|
| P-00 | Iniciar sesión en el sistema | CU-G-01 | ambos |
| P-01 | Inicio (uno por perfil, con indicadores y accesos rápidos) | — | ambos |
| P-03 | Gestionar usuarios del sistema | CU-G-16 | ambos |
| P-04 | Registrar especialidades, profesionales, consultorios y salas | CU-G-17 | ambos |
| P-05 | Gestionar legajo de paciente | CU-G-02 | ambos |
| P-06 | Asignar turno | CU-G-04 | ambos |
| P-07 | Asignar sobreturno | CU-G-05 | A |
| P-08 | Emitir comprobante de turno | CU-G-04 | ambos |
| P-09 | Reprogramar o cancelar turno | CU-G-04 | ambos |
| P-10 | Acreditar paciente y registrar modalidad de pago (+ validar cobertura) | CU-G-06, CU-G-07 | ambos |
| P-11 | Registrar llegada de paciente de laboratorio sin turno | CU-G-05 | B |
| P-12 | Solicitar estudio urgente (guardia/internación) | CU-G-05 | B |
| P-13 | Atender al paciente (sala de espera / cola de atención) | CU-G-08 | ambos |
| P-14 | Registrar atención y turno de control | CU-G-08 | A |
| P-15 | Registrar realización del estudio y consumo de insumos | CU-G-08 | B |
| P-16 | Entregar resultado del estudio | CU-G-09 | B |
| P-17 | Controlar insumos y reponer stock | CU-G-10 | ambos |
| P-18 | Generar reportes de gestión | CU-G-11 | ambos |
| P-19 | Configurar parámetros de agenda | CU-G-03 | A |
| P-20 | Asignar consultorio e insumos al profesional | CU-G-12 | A |
| P-21 | Publicar agenda de profesional | CU-G-03 | A |
| P-22 | Registrar ingreso y egreso del profesional | CU-G-13 | A |
| P-23 | Liquidar honorarios por profesional | CU-G-14 | A |
| P-24 | Gestionar catálogo de estudios | CU-G-03 | B |
| P-25 | Configurar franjas horarias de laboratorio | CU-G-03 | B |
| P-26 | Publicar agenda de estudios por recurso | CU-G-03 | B |
| P-27 | Auditar operaciones del sistema | CU-G-18 | ambos |
| P-02 | Cerrar sesión en el sistema (botón del encabezado y ventana de confirmación) | CU-G-15 | ambos |

## Pruebas de aceptación

`SePrice.Pruebas` recorre las escenas de la demostración sobre una base nueva y controla
la matriz 5.3: 0 Administración · 1 Puesta en marcha · 2 Toma de turnos · 3 Inicio de
jornada · 4 Recepción · 5 Atención · 6 Insumos y resultados · 7 Gestión.
Resultado: **94 controles OK, 0 fallas** (verificado también sobre el runtime de .NET 8).

## Coherencia con el modelo

- `esquema.sql`, `datos_ejemplo.sql` y `Permisos.generado.cs` **no se editan a mano**: se
  generan desde el diccionario de datos y la matriz 5.3 con `gen_sql.js` y `gen_datos.js`
  (paquete de fuentes del proyecto, `rebuild.sh`). El verificador del cuaderno controla que
  haya una tabla por entidad del DER (29) y que los permisos coincidan con la 5.3.
- Los nombres de pantallas, botones, estados y mensajes siguen la especificación de
  interfaz y la lista única de estados del turno.

## Supuestos y límites del prototipo (declarados en el cuaderno)

- **Obra social simulada**: Sanitas Federal responde «válida», Cobertura Andina
  «rechazada» y Prevenir Salud «sin respuesta» (validación diferida, CU-A-11).
- **Valores de ejemplo, a confirmar con el cliente**: aranceles, honorarios, tolerancia de
  ingreso, ocupación de salas. La prestación *Cardiología · particular* queda sin valor a
  propósito para mostrar el aviso de la escena 7.
- El estado **«ausente» del turno** existe, pero ningún proceso lo asigna todavía
  (pendiente del equipo, cuaderno 12.4): no hay botón «No se presentó».
- La guardia sólo busca por DNI: no crea legajos (matriz 5.3).
- No se envían correos ni se imprime en impresora fiscal: los comprobantes y reportes se
  imprimen o guardan como PDF desde la vista previa de Windows.
- **SQLite** alcanza para la demostración y un puesto. Para varios puestos en simultáneo
  se recomienda un motor servidor (RT-04); sólo cambia `Proveedor.cs`.
- `SePrice.UI/Captura.cs` sólo se compila en la verificación con Mono (símbolo `MONO`):
  recorre las pantallas y guarda capturas. En Visual Studio no forma parte del programa.

## Verificación fuera de Windows (equipo de desarrollo)

`herramientas/compilar_mono.sh` compila con Mono en Linux (símbolo `MONO`, que cambia el
proveedor de SQLite por `Mono.Data.Sqlite`) y `herramientas/capturar.sh` toma capturas de
las pantallas en un servidor gráfico virtual. En Windows no hacen falta.
