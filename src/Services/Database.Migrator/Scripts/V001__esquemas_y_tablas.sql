-- =====================================================================
--  V001 - Esquemas y tablas
--  seguridad : usuarios y refresh tokens (Auth.Api)
--  carga     : trazabilidad de archivos, auditoria, fallidos y datos
-- =====================================================================

CREATE SCHEMA IF NOT EXISTS seguridad;
CREATE SCHEMA IF NOT EXISTS carga;

-- ---------------------------------------------------------------------
--  Usuarios (fuente de identidad)
-- ---------------------------------------------------------------------
CREATE TABLE seguridad.usuario (
    id                 integer      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email              varchar(150) NOT NULL,
    nombre_completo    varchar(150) NOT NULL,
    password_hash      varchar(100) NOT NULL,
    rol                varchar(30)  NOT NULL,
    activo             boolean      NOT NULL DEFAULT true,
    intentos_fallidos  integer      NOT NULL DEFAULT 0,
    bloqueado_hasta    timestamptz  NULL,
    fecha_registro     timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_usuario_email UNIQUE (email),
    CONSTRAINT ck_usuario_rol CHECK (rol IN ('Administrador', 'Operador', 'Consulta'))
);

-- Solo se guarda el hash del refresh token, nunca el token en claro
CREATE TABLE seguridad.refresh_token (
    id                bigint       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usuario_id        integer      NOT NULL REFERENCES seguridad.usuario (id),
    token_hash        varchar(100) NOT NULL,
    fecha_expiracion  timestamptz  NOT NULL,
    fecha_creacion    timestamptz  NOT NULL DEFAULT now(),
    fecha_revocacion  timestamptz  NULL,
    reemplazado_por   varchar(100) NULL,
    CONSTRAINT uq_refresh_token_hash UNIQUE (token_hash)
);

CREATE INDEX ix_refresh_token_usuario ON seguridad.refresh_token (usuario_id);

-- ---------------------------------------------------------------------
--  CargaArchivo: trazabilidad de cada archivo subido
-- ---------------------------------------------------------------------
CREATE TABLE carga.carga_archivo (
    id                    integer       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre_archivo        varchar(200)  NOT NULL,
    usuario               varchar(150)  NOT NULL,
    usuario_id            integer       NOT NULL,
    ruta_archivo          varchar(500)  NOT NULL,
    tamano_bytes          bigint        NOT NULL,
    periodos              varchar(200)  NULL,
    estado                varchar(30)   NOT NULL,
    resultado             varchar(30)   NULL,
    mensaje               varchar(1000) NULL,
    total_registros       integer       NOT NULL DEFAULT 0,
    registros_insertados  integer       NOT NULL DEFAULT 0,
    registros_fallidos    integer       NOT NULL DEFAULT 0,
    fecha_registro        timestamptz   NOT NULL DEFAULT now(),
    fecha_inicio_proceso  timestamptz   NULL,
    fecha_fin             timestamptz   NULL,
    fecha_notificacion    timestamptz   NULL,
    CONSTRAINT ck_carga_estado
        CHECK (estado IN ('Pendiente', 'EnProceso', 'Cargado', 'Finalizado', 'Notificado')),
    CONSTRAINT ck_carga_resultado
        CHECK (resultado IS NULL OR resultado IN ('Exitoso', 'ConObservaciones', 'Rechazado', 'Error'))
);

CREATE INDEX ix_carga_archivo_usuario ON carga.carga_archivo (usuario_id, fecha_registro DESC);

-- ---------------------------------------------------------------------
--  Periodos que contiene cada carga (un Excel puede traer varios).
--  Se usa para la validacion de duplicidad por periodo.
-- ---------------------------------------------------------------------
CREATE TABLE carga.carga_archivo_periodo (
    carga_id  integer     NOT NULL REFERENCES carga.carga_archivo (id),
    periodo   varchar(7)  NOT NULL,
    PRIMARY KEY (carga_id, periodo)
);

CREATE INDEX ix_carga_archivo_periodo ON carga.carga_archivo_periodo (periodo);

-- ---------------------------------------------------------------------
--  Auditoria: cada cambio de estado queda registrado (quien, cuando, por que)
-- ---------------------------------------------------------------------
CREATE TABLE carga.carga_archivo_historial (
    id        bigint        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    carga_id  integer       NOT NULL REFERENCES carga.carga_archivo (id),
    estado    varchar(30)   NOT NULL,
    detalle   varchar(1000) NULL,
    origen    varchar(50)   NOT NULL,
    fecha     timestamptz   NOT NULL DEFAULT now()
);

CREATE INDEX ix_historial_carga ON carga.carga_archivo_historial (carga_id, fecha);

-- ---------------------------------------------------------------------
--  DetalleCarga: registros que no se cargaron y el motivo (fallidos)
-- ---------------------------------------------------------------------
CREATE TABLE carga.detalle_carga (
    id               bigint       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    carga_id         integer      NOT NULL REFERENCES carga.carga_archivo (id),
    fila             integer      NOT NULL,
    codigo_producto  varchar(50)  NULL,
    motivo           varchar(30)  NOT NULL,
    detalle          varchar(500) NOT NULL,
    datos            jsonb        NULL,
    fecha            timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_detalle_motivo
        CHECK (motivo IN ('Existente', 'DatoInvalido', 'PeriodoRechazado'))
);

CREATE INDEX ix_detalle_carga ON carga.detalle_carga (carga_id, fila);

-- ---------------------------------------------------------------------
--  DataProcesada: registros extraidos del Excel
-- ---------------------------------------------------------------------
CREATE TABLE carga.data_procesada (
    id               bigint         GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    carga_id         integer        NOT NULL REFERENCES carga.carga_archivo (id),
    fila             integer        NOT NULL,
    periodo          varchar(7)     NOT NULL,
    codigo_producto  varchar(50)    NOT NULL,
    nombre_producto  varchar(200)   NOT NULL,
    precio           numeric(14, 2) NOT NULL,
    fecha_registro   timestamptz    NOT NULL DEFAULT now(),
    CONSTRAINT uq_data_procesada_codigo UNIQUE (codigo_producto)
);

CREATE INDEX ix_data_procesada_carga ON carga.data_procesada (carga_id, fila);
