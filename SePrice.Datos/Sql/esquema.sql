-- =====================================================================
-- Clínica SePrice · Esquema de la base de datos (SQLite)
-- GENERADO por gen_sql.js desde el diccionario de datos del Cuaderno de
-- especificaciones (sección 11). No editar a mano: se regenera.
-- 29 entidades del DER = 29 tablas.
-- Fechas: TEXT 'aaaa-mm-dd' · Horas: TEXT 'hh:mm' · Fecha y hora: TEXT
-- 'aaaa-mm-dd hh:mm' · Lógico: INTEGER 0/1 · Importes: NUMERIC.
-- =====================================================================
PRAGMA foreign_keys = ON;

-- PACIENTE (alias: Legajo, Padrón de pacientes)
CREATE TABLE IF NOT EXISTS paciente (
  id_paciente INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 8 · PK · Identificador interno (autonumérico).
  dni INTEGER NOT NULL UNIQUE,  -- Numérico 8 · UQ · Documento nacional de identidad. Único: clave de búsqueda en mostrador y call center.
  nro_legajo INTEGER NOT NULL UNIQUE,  -- Numérico 8 · UQ · Número único de legajo, válido para los dos circuitos.
  apellido_nombre TEXT,  -- Alfabético 80 · Apellido y nombre del/la paciente.
  fecha_nacimiento TEXT,  -- Fecha 8 · Para calcular la edad que ven el profesional y el técnico.
  telefono TEXT,  -- Alfanumérico 20 · Teléfono de contacto.
  correo TEXT,  -- Alfanumérico 80 · Correo electrónico (opcional).
  id_obra_social INTEGER REFERENCES obra_social(id_obra_social),  -- Numérico 4 · FK · Cobertura del/la paciente. Vacío = particular.
  nro_afiliado TEXT  -- Alfanumérico 20 · Número de afiliado a la obra social (vacío si es particular).
);

-- OBRA SOCIAL (alias: Cobertura)
CREATE TABLE IF NOT EXISTS obra_social (
  id_obra_social INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 4 · PK · Identificador de la obra social.
  nombre TEXT NOT NULL UNIQUE,  -- Alfanumérico 60 · UQ · Nombre de la obra social.
  plan TEXT  -- Alfanumérico 30 · Plan del/la afiliado/a.
);

-- PROFESIONAL (alias: Médico/a, Técnico/a de estudios)
CREATE TABLE IF NOT EXISTS profesional (
  id_profesional INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 6 · PK · Identificador del profesional o técnico.
  apellido_nombre TEXT,  -- Alfabético 80 · Apellido y nombre.
  matricula TEXT NOT NULL UNIQUE,  -- Alfanumérico 15 · UQ · Matrícula profesional. Única (control de duplicados de CU-G-17).
  id_especialidad INTEGER REFERENCES especialidad(id_especialidad),  -- Numérico 3 · FK · Especialidad del/la médico/a. Vacío en los técnicos.
  tipo TEXT,  -- Alfabético 7 · Valores: «médico» / «técnico».
  estado TEXT  -- Alfabético 8 · Valores: «activo» / «inactivo» (baja lógica).
);

-- ESPECIALIDAD
CREATE TABLE IF NOT EXISTS especialidad (
  id_especialidad INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 3 · PK · Identificador de la especialidad.
  nombre TEXT NOT NULL UNIQUE,  -- Alfabético 50 · UQ · Nombre de la especialidad.
  duracion_minima INTEGER,  -- Numérico 3 · Minutos por turno. Mínimos relevados: 15 (general), 25 (fisio-kinesiología), 30 (salud mental).
  estado TEXT  -- Alfabético 8 · Valores: «activo» / «inactivo».
);

-- CONSULTORIO / SALA (alias: CONSULTORIO, Sala de estudios)
CREATE TABLE IF NOT EXISTS consultorio_sala (
  id_consultorio INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 3 · PK · Identificador del consultorio o la sala.
  numero_nombre TEXT NOT NULL UNIQUE,  -- Alfanumérico 30 · UQ · Número de consultorio o nombre de la sala.
  ubicacion TEXT,  -- Alfanumérico 50 · Piso y sector.
  tipo TEXT,  -- Alfabético 11 · Valores: «consultorio» / «sala».
  equipamiento TEXT,  -- Alfanumérico 100 · Equipamiento de la sala (vacío en consultorios).
  estado TEXT  -- Alfabético 8 · Valores: «activo» / «inactivo».
);

-- AGENDA (alias: Agenda de turnos)
CREATE TABLE IF NOT EXISTS agenda (
  id_agenda INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 8 · PK · Identificador de la agenda de un día.
  id_profesional INTEGER REFERENCES profesional(id_profesional),  -- Numérico 6 · FK · Profesional que atiende (en Estudios, técnico del recurso).
  id_consultorio INTEGER REFERENCES consultorio_sala(id_consultorio),  -- Numérico 3 · FK · Consultorio o sala donde se atiende.
  dia TEXT,  -- Fecha 8 · Día de la agenda.
  hora_desde TEXT,  -- Hora 4 · Inicio de la franja horaria.
  hora_hasta TEXT,  -- Hora 4 · Fin de la franja horaria.
  profesional_ausente INTEGER  -- Lógico 1 · Marca del día cuando venció la tolerancia sin ingreso registrado (CU-G-13).
);

-- EMPLEADO ADMINISTRATIVO (alias: Personal administrativo, Operador de call center)
CREATE TABLE IF NOT EXISTS empleado_administrativo (
  id_empleado INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 6 · PK · Identificador del/la empleado/a.
  apellido_nombre TEXT,  -- Alfabético 80 · Apellido y nombre.
  rol TEXT  -- Alfabético 30 · Puesto: mostrador, call center, coordinación, jefatura, insumos, guardia o administración del 
);

-- TURNO (alias: Reserva, Sobreturno, Demanda sin turno)
CREATE TABLE IF NOT EXISTS turno (
  id_turno INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 10 · PK · Identificador del turno (también de sobreturnos y demanda sin turno).
  id_paciente INTEGER REFERENCES paciente(id_paciente),  -- Numérico 8 · FK · Paciente del turno.
  id_agenda INTEGER REFERENCES agenda(id_agenda),  -- Numérico 8 · FK · Agenda de la que sale la franja. Vacío en laboratorio y guardia/internación.
  id_empleado INTEGER REFERENCES empleado_administrativo(id_empleado),  -- Numérico 6 · FK · Empleado/a que lo registró.
  fecha_hora TEXT,  -- Fecha y hora 12 · Franja reservada o, sin turno, momento de registro.
  estado TEXT,  -- Alfabético 11 · Lista única: reservado, acreditado, en atención, atendido, realizado, entregado, ausente, canc
  procedencia TEXT,  -- Alfabético 16 · Valores: «agenda» / «sobreturno» / «orden de llegada» / «guardia». No es un estado.
  modalidad_toma TEXT,  -- Alfabético 10 · Valores: «personal» / «telefónica».
  modalidad_pago TEXT,  -- Alfabético 11 · Valores: «obra social» / «particular».
  arancel_informado NUMERIC,  -- Numérico 10,2 · Arancel particular informado al/la paciente (lo fija la clínica).
  motivo_cancelacion TEXT,  -- Alfanumérico 100 · Obligatorio si el estado es «cancelado».
  nro_orden_llegada INTEGER,  -- Numérico 4 · Número de orden en laboratorio (sólo Estudios).
  sector_solicitante TEXT,  -- Alfabético 11 · Valores: «guardia» / «internación» (sólo pedidos urgentes).
  prioridad TEXT,  -- Alfabético 7 · Valores: «urgente» / «alta» (sólo pedidos urgentes).
  nro_episodio TEXT,  -- Alfanumérico 15 · Episodio de guardia o internación.
  id_liquidacion INTEGER REFERENCES liquidacion_de_honorarios(id_liquidacion)  -- Numérico 8 · FK · Liquidación que incluyó la atención. Vacío mientras no se liquidó (impide liquidar dos veces
);

-- ESTUDIO (alias: Prestación de diagnóstico)
CREATE TABLE IF NOT EXISTS estudio (
  id_estudio INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 4 · PK · Identificador del estudio en el catálogo.
  nombre TEXT NOT NULL UNIQUE,  -- Alfanumérico 60 · UQ · Nombre del estudio.
  duracion INTEGER,  -- Numérico 3 · Minutos por estudio (vacío en laboratorio).
  requiere_ayuno INTEGER,  -- Lógico 1 · Si es «sí», sólo se atiende de 7 a 11 h.
  requiere_turno INTEGER,  -- Lógico 1 · «No» en laboratorio: se atiende por orden de llegada.
  sala_equipo TEXT  -- Alfanumérico 50 · Sala o equipo de referencia; la sala real se fija al publicar la agenda por recurso.
);

-- DETALLE TURNO-ESTUDIO
CREATE TABLE IF NOT EXISTS detalle_turno_estudio (
  id_detalle INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 10 · PK · Identificador de cada estudio dentro de un turno.
  id_turno INTEGER REFERENCES turno(id_turno),  -- Numérico 10 · FK · Turno.
  id_estudio INTEGER REFERENCES estudio(id_estudio)  -- Numérico 4 · FK · Estudio incluido en el turno.
);

-- INSUMO (alias: Stock de insumos)
CREATE TABLE IF NOT EXISTS insumo (
  id_insumo INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 5 · PK · Identificador del insumo (entidad única para los dos circuitos).
  nombre TEXT NOT NULL UNIQUE,  -- Alfanumérico 60 · UQ · Nombre del insumo.
  tipo TEXT,  -- Alfabético 15 · Valores: «clínico» / «de consultorio» (unificación del 20/09/2026).
  unidad_medida TEXT,  -- Alfabético 15 · Unidad: frasco, par, caja, metro, etc.
  stock_actual INTEGER,  -- Numérico 6 · Existencia actual.
  stock_minimo INTEGER  -- Numérico 6 · Por debajo de este valor se dispara la alerta de reposición (en los dos circuitos).
);

-- ESTUDIO x INSUMO
CREATE TABLE IF NOT EXISTS estudio_x_insumo (
  id_estudio INTEGER REFERENCES estudio(id_estudio),  -- Numérico 4 · FK · Estudio (parte de la clave compuesta).
  id_insumo INTEGER REFERENCES insumo(id_insumo),  -- Numérico 5 · FK · Insumo (parte de la clave compuesta).
  cantidad_necesaria INTEGER,  -- Numérico 4 · Cantidad estándar por prestación.
  PRIMARY KEY (id_estudio, id_insumo)  -- Clave compuesta (entidad asociativa)
);

-- CONSUMO DE INSUMO
CREATE TABLE IF NOT EXISTS consumo_de_insumo (
  id_consumo INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 8 · PK · Identificador del registro de consumo.
  id_insumo INTEGER REFERENCES insumo(id_insumo),  -- Numérico 5 · FK · Insumo consumido.
  id_consultorio INTEGER REFERENCES consultorio_sala(id_consultorio),  -- Numérico 3 · FK · Consultorio donde se usó.
  id_empleado INTEGER REFERENCES empleado_administrativo(id_empleado),  -- Numérico 6 · FK · Empleado/a que registró el consumo.
  fecha TEXT,  -- Fecha 8 · Fecha del consumo.
  cantidad INTEGER  -- Numérico 4 · Cantidad consumida.
);

-- ASIGNACIÓN DE CONSULTORIO
CREATE TABLE IF NOT EXISTS asignacion_de_consultorio (
  id_asignacion INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 6 · PK · Identificador de la asignación.
  id_profesional INTEGER REFERENCES profesional(id_profesional),  -- Numérico 6 · FK · Profesional asignado/a.
  id_consultorio INTEGER REFERENCES consultorio_sala(id_consultorio),  -- Numérico 3 · FK · Consultorio asignado.
  dia_semana TEXT,  -- Alfabético 9 · Valores: lunes a sábado.
  hora_desde TEXT,  -- Hora 4 · Inicio de la franja.
  hora_hasta TEXT,  -- Hora 4 · Fin de la franja. No puede superponerse con otra asignación del mismo consultorio.
  vigencia_desde TEXT,  -- Fecha 8 · Inicio de la vigencia.
  vigencia_hasta TEXT  -- Fecha 8 · Fin de la vigencia (vacío = sin vencimiento).
);

-- DOTACIÓN DE INSUMOS
CREATE TABLE IF NOT EXISTS dotacion_de_insumos (
  id_consultorio INTEGER REFERENCES consultorio_sala(id_consultorio),  -- Numérico 3 · FK · Consultorio (parte de la clave compuesta).
  id_insumo INTEGER REFERENCES insumo(id_insumo),  -- Numérico 5 · FK · Insumo (parte de la clave compuesta).
  cantidad_minima INTEGER,  -- Numérico 4 · Cantidad que debe tener el consultorio. Indica qué reponer; no dispara la alerta.
  PRIMARY KEY (id_consultorio, id_insumo)  -- Clave compuesta (entidad asociativa)
);

-- REGISTRO DE PRESENTISMO (alias: Asistencia del profesional)
CREATE TABLE IF NOT EXISTS registro_de_presentismo (
  id_presentismo INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 8 · PK · Identificador del registro diario.
  id_profesional INTEGER REFERENCES profesional(id_profesional),  -- Numérico 6 · FK · Profesional.
  fecha TEXT,  -- Fecha 8 · Jornada.
  hora_ingreso TEXT,  -- Hora 4 · Hora de ingreso registrada.
  hora_egreso TEXT,  -- Hora 4 · Hora de egreso registrada.
  estado TEXT  -- Alfabético 21 · Valores: «presente» / «ausente» / «egreso no registrado».
);

-- LIQUIDACIÓN DE HONORARIOS (alias: Honorarios)
CREATE TABLE IF NOT EXISTS liquidacion_de_honorarios (
  id_liquidacion INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 8 · PK · Identificador de la liquidación.
  id_profesional INTEGER REFERENCES profesional(id_profesional),  -- Numérico 6 · FK · Profesional liquidado/a.
  periodo_desde TEXT,  -- Fecha 8 · Inicio del período.
  periodo_hasta TEXT,  -- Fecha 8 · Fin del período.
  importe_total NUMERIC,  -- Numérico 12,2 · Total a pagar (no incluye atenciones «observadas»).
  estado TEXT,  -- Alfabético 7 · Valores: «abierta» / «cerrada».
  fecha_cierre TEXT  -- Fecha 8 · Fecha en que se cerró.
);

-- USUARIO (alias: Padrón de usuarios)
CREATE TABLE IF NOT EXISTS usuario (
  id_usuario INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 6 · PK · Identificador del usuario.
  nombre_usuario TEXT NOT NULL UNIQUE,  -- Alfanumérico 20 · UQ · Nombre para iniciar sesión. Único.
  contrasena TEXT,  -- Alfanumérico 64 · Se guarda cifrada; nunca se muestra.
  rol TEXT,  -- Alfabético 40 · Uno de los 8 perfiles: administrador/a del sistema, coordinación médica, personal administrati
  estado TEXT,  -- Alfabético 14 · Valores: «habilitado» / «deshabilitado» (baja lógica).
  fecha_alta TEXT,  -- Fecha 8 · Fecha de alta.
  id_empleado INTEGER REFERENCES empleado_administrativo(id_empleado),  -- Numérico 6 · FK · Empleado/a al que corresponde (vacío si es profesional).
  id_profesional INTEGER REFERENCES profesional(id_profesional)  -- Numérico 6 · FK · Profesional al que corresponde (vacío si es empleado/a).
);

-- REGISTRO DE SESIÓN
CREATE TABLE IF NOT EXISTS registro_de_sesion (
  id_sesion INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 10 · PK · Identificador de la sesión.
  id_usuario INTEGER REFERENCES usuario(id_usuario),  -- Numérico 6 · FK · Usuario que inició la sesión.
  inicio TEXT,  -- Fecha y hora 12 · Fecha y hora de inicio.
  cierre TEXT,  -- Fecha y hora 12 · Fecha y hora de cierre.
  tipo_cierre TEXT  -- Alfabético 16 · Valores: «manual» / «por inactividad».
);

-- REGISTRO DE AUDITORÍA (alias: Auditoría)
CREATE TABLE IF NOT EXISTS registro_de_auditoria (
  id_auditoria INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 12 · PK · Identificador del registro.
  id_usuario INTEGER REFERENCES usuario(id_usuario),  -- Numérico 6 · FK · Usuario que hizo la operación.
  fecha_hora TEXT,  -- Fecha y hora 12 · Momento de la operación.
  circuito TEXT,  -- Alfabético 5 · Valores: «A» / «B» / «ambos».
  modulo TEXT,  -- Alfanumérico 40 · Pantalla o módulo donde se hizo.
  accion TEXT,  -- Alfabético 12 · Valores: «alta» / «modificación» / «baja» / «consulta» / «exportación».
  entidad TEXT,  -- Alfanumérico 40 · Entidad afectada.
  id_registro TEXT,  -- Alfanumérico 20 · Identificador del registro afectado.
  valor_anterior TEXT,  -- Alfanumérico 500 · Valores antes del cambio (vacío en altas y consultas).
  valor_nuevo TEXT  -- Alfanumérico 500 · Valores después del cambio (vacío en bajas y consultas).
);

-- PARÁMETRO (alias: Configuración del sistema)
CREATE TABLE IF NOT EXISTS parametro (
  clave TEXT PRIMARY KEY,  -- Alfanumérico 30 · PK · Nombre del parámetro: duración por especialidad, cupo de sobreturnos, tolerancia de ing
  descripcion TEXT,  -- Alfanumérico 100 · Para qué sirve.
  valor TEXT,  -- Alfanumérico 30 · Valor vigente (por ejemplo, 1 sobreturno por hora; 15 minutos de tolerancia; 07:00–11:00).
  unidad TEXT,  -- Alfabético 15 · minutos, turnos por hora, horario.
  circuito TEXT,  -- Alfabético 5 · Valores: «A» / «B» / «ambos».
  vigencia_desde TEXT,  -- Fecha 8 · Desde cuándo rige (las agendas ya publicadas conservan su configuración).
  a_confirmar INTEGER  -- Lógico 1 · «Sí» si es un valor de ejemplo, a confirmar con el cliente.
);

-- VALOR DE HONORARIO
CREATE TABLE IF NOT EXISTS valor_de_honorario (
  id_valor INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 6 · PK · Identificador del valor.
  id_especialidad INTEGER REFERENCES especialidad(id_especialidad),  -- Numérico 3 · FK · Especialidad (prestación) a la que corresponde.
  modalidad_pago TEXT,  -- Alfabético 11 · Valores: «obra social» / «particular». Los valores por obra social quedan pendientes del esque
  importe NUMERIC,  -- Numérico 10,2 · Importe por atención.
  vigencia_desde TEXT,  -- Fecha 8 · Desde cuándo rige.
  a_confirmar INTEGER  -- Lógico 1 · «Sí» mientras el valor no se relevó con el cliente.
);

-- VALIDACIÓN DE COBERTURA (alias: Autorización)
CREATE TABLE IF NOT EXISTS validacion_de_cobertura (
  id_validacion INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 10 · PK · Identificador de la validación.
  id_turno INTEGER REFERENCES turno(id_turno),  -- Numérico 10 · FK · Turno que se acredita.
  id_obra_social INTEGER REFERENCES obra_social(id_obra_social),  -- Numérico 4 · FK · Obra social consultada.
  id_orden INTEGER REFERENCES orden_medica(id_orden),  -- Numérico 10 · FK · Orden médica que respalda el pedido (sólo Estudios).
  tipo TEXT,  -- Alfabético 12 · Valores: «afiliación» / «autorización».
  modo TEXT,  -- Alfabético 8 · Valores: «en línea» / «diferida» (sólo Consultorios externos).
  resultado TEXT,  -- Alfabético 9 · Valores: «válida» / «rechazada» / «pendiente».
  nro_referencia TEXT,  -- Alfanumérico 20 · Número de referencia o de autorización que devuelve la obra social.
  fecha_hora TEXT,  -- Fecha y hora 12 · Momento de la consulta.
  fecha_regularizacion TEXT  -- Fecha 8 · Cuando una validación pendiente se resuelve.
);

-- ATENCIÓN (alias: Consulta)
CREATE TABLE IF NOT EXISTS atencion (
  id_atencion INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 10 · PK · Identificador de la atención.
  id_turno INTEGER REFERENCES turno(id_turno),  -- Numérico 10 · FK · Turno atendido (relación 1:1: un turno tiene a lo sumo una atención).
  hora_inicio TEXT,  -- Hora 4 · Se registra al llamar al/la paciente.
  hora_fin TEXT,  -- Hora 4 · Se registra al cerrar la atención.
  requiere_control INTEGER,  -- Lógico 1 · «Sí» si el profesional pidió un turno de control.
  plazo_control_dias INTEGER  -- Numérico 3 · Plazo sugerido para el control (7, 15 o 30 días). El turno lo reserva después el personal adminis
);

-- ORDEN MÉDICA
CREATE TABLE IF NOT EXISTS orden_medica (
  id_orden INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 10 · PK · Identificador de la orden.
  id_paciente INTEGER REFERENCES paciente(id_paciente),  -- Numérico 8 · FK · Paciente que la presenta.
  profesional_solicitante TEXT,  -- Alfabético 80 · Médico/a que indica el estudio (puede ser externo a la clínica).
  matricula_solicitante TEXT,  -- Alfanumérico 15 · Matrícula del/la solicitante.
  estudios_indicados TEXT,  -- Alfanumérico 200 · Estudios que indica la orden, tal como están escritos.
  fecha_emision TEXT,  -- Fecha 8 · Fecha de la orden.
  fecha_vencimiento TEXT  -- Fecha 8 · Una orden vencida no habilita la acreditación.
);

-- RESULTADO DE ESTUDIO (alias: Informe)
CREATE TABLE IF NOT EXISTS resultado_de_estudio (
  id_resultado INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 10 · PK · Identificador del resultado.
  id_detalle INTEGER REFERENCES detalle_turno_estudio(id_detalle),  -- Numérico 10 · FK · Estudio realizado (relación 1:1).
  id_tecnico INTEGER REFERENCES profesional(id_profesional),  -- Numérico 6 · FK · Técnico/a que realizó el estudio.
  fecha_realizacion TEXT,  -- Fecha 8 · Fecha del estudio.
  nro_comprobante_retiro TEXT NOT NULL UNIQUE,  -- Alfanumérico 12 · UQ · Comprobante que recibe el/la paciente.
  fecha_estimada TEXT,  -- Fecha 8 · Fecha estimada del resultado.
  informe TEXT,  -- Alfanumérico 2000 · Texto del informe (o referencia al archivo).
  fecha_entrega TEXT,  -- Fecha 8 · Fecha de entrega.
  receptor TEXT  -- Alfanumérico 90 · Nombre y documento de quien retira; en internación, el sector.
);

-- PEDIDO DE REPOSICIÓN
CREATE TABLE IF NOT EXISTS pedido_de_reposicion (
  id_pedido INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 8 · PK · Identificador del pedido.
  fecha TEXT,  -- Fecha 8 · Fecha del pedido.
  destino TEXT,  -- Alfabético 9 · Valores: «interno» (depósito, Consultorios) / «proveedor» (Estudios).
  id_proveedor INTEGER REFERENCES proveedor(id_proveedor),  -- Numérico 5 · FK · Proveedor (vacío en pedidos internos).
  id_empleado INTEGER REFERENCES empleado_administrativo(id_empleado),  -- Numérico 6 · FK · Quien emite el pedido.
  estado TEXT  -- Alfabético 9 · Valores: «propuesto» / «enviado» / «recibido».
);

-- DETALLE DE PEDIDO
CREATE TABLE IF NOT EXISTS detalle_de_pedido (
  id_pedido INTEGER REFERENCES pedido_de_reposicion(id_pedido),  -- Numérico 8 · FK · Pedido (parte de la clave compuesta).
  id_insumo INTEGER REFERENCES insumo(id_insumo),  -- Numérico 5 · FK · Insumo pedido (parte de la clave compuesta).
  cantidad INTEGER,  -- Numérico 5 · Cantidad pedida.
  PRIMARY KEY (id_pedido, id_insumo)  -- Clave compuesta (entidad asociativa)
);

-- PROVEEDOR
CREATE TABLE IF NOT EXISTS proveedor (
  id_proveedor INTEGER PRIMARY KEY AUTOINCREMENT,  -- Numérico 5 · PK · Identificador del proveedor.
  razon_social TEXT NOT NULL UNIQUE,  -- Alfanumérico 80 · UQ · Razón social.
  contacto TEXT  -- Alfanumérico 80 · Teléfono o correo para enviar pedidos.
);
