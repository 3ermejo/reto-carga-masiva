-- =====================================================================
--  V003 - Procedimientos de carga masiva
--  El flujo de estados se controla aqui (maquina de estados en la BD):
--    Pendiente -> EnProceso -> Cargado -> Finalizado -> Notificado
--  Cada cambio de estado deja un registro en carga_archivo_historial.
-- =====================================================================

-- ---------------------------------------------------------------------
--  Control.Api: registra el archivo con estado Pendiente
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION carga.fn_registrar_carga(
    p_nombre_archivo varchar, p_usuario varchar, p_usuario_id integer,
    p_ruta_archivo varchar, p_tamano_bytes bigint)
RETURNS integer
LANGUAGE plpgsql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
DECLARE
    v_carga_id integer;
BEGIN
    INSERT INTO carga.carga_archivo (nombre_archivo, usuario, usuario_id, ruta_archivo, tamano_bytes, estado)
    VALUES (p_nombre_archivo, p_usuario, p_usuario_id, p_ruta_archivo, p_tamano_bytes, 'Pendiente')
    RETURNING id INTO v_carga_id;

    INSERT INTO carga.carga_archivo_historial (carga_id, estado, detalle, origen)
    VALUES (v_carga_id, 'Pendiente', 'Archivo recibido. Usuario: ' || p_usuario, 'control-api');

    RETURN v_carga_id;
END;
$$;

-- ---------------------------------------------------------------------
--  BulkLoad: pasa la carga a EnProceso.
--  Devuelve OK, OMITIR (ya terminada: mensaje repetido) o NO_EXISTE.
--  Si la carga quedo a medias (el worker se cayo) se limpia y se reprocesa.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION carga.fn_iniciar_proceso(p_carga_id integer)
RETURNS varchar
LANGUAGE plpgsql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
DECLARE
    v_estado varchar;
BEGIN
    SELECT estado INTO v_estado
      FROM carga.carga_archivo
     WHERE id = p_carga_id
       FOR UPDATE;

    IF NOT FOUND THEN
        RETURN 'NO_EXISTE';
    END IF;

    IF v_estado IN ('Finalizado', 'Notificado') THEN
        RETURN 'OMITIR';
    END IF;

    IF v_estado IN ('EnProceso', 'Cargado') THEN
        DELETE FROM carga.data_procesada WHERE carga_id = p_carga_id;
        DELETE FROM carga.detalle_carga WHERE carga_id = p_carga_id;
        DELETE FROM carga.carga_archivo_periodo WHERE carga_id = p_carga_id;
    END IF;

    UPDATE carga.carga_archivo
       SET estado = 'EnProceso', fecha_inicio_proceso = now(), periodos = NULL,
           total_registros = 0, registros_insertados = 0, registros_fallidos = 0
     WHERE id = p_carga_id;

    INSERT INTO carga.carga_archivo_historial (carga_id, estado, detalle, origen)
    VALUES (p_carga_id, 'EnProceso',
            CASE WHEN v_estado = 'Pendiente' THEN 'Inicio del procesamiento' ELSE 'Reprocesamiento' END,
            'bulkload-worker');

    RETURN 'OK';
END;
$$;

-- ---------------------------------------------------------------------
--  BulkLoad: validacion de duplicidad por periodo.
--  Un Excel puede traer varios periodos; se revisan todos:
--   - Algun periodo con otra carga Cargado/Finalizado/Notificado -> RECHAZADO
--   - Algun periodo con otra carga Pendiente/EnProceso            -> BLOQUEADO
--   - Sin conflictos: los periodos quedan reservados para esta carga -> OK
--  Las cargas rechazadas o con error no cuentan.
--  Los advisory locks evitan que dos cargas del mismo periodo pasen a la vez.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION carga.fn_validar_periodos(p_carga_id integer, p_periodos varchar[])
RETURNS TABLE (resultado varchar, periodo varchar, carga_conflicto integer, estado_conflicto varchar)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
#variable_conflict use_column
DECLARE
    v_periodo varchar;
    v_id      integer;
    v_estado  varchar;
BEGIN
    -- Se bloquea cada periodo en orden para evitar deadlocks entre cargas
    FOREACH v_periodo IN ARRAY ARRAY(SELECT DISTINCT x FROM unnest(p_periodos) AS x ORDER BY 1)
    LOOP
        PERFORM pg_advisory_xact_lock(hashtext('periodo:' || v_periodo));
    END LOOP;

    SELECT cp.periodo, c.id, c.estado
      INTO v_periodo, v_id, v_estado
      FROM carga.carga_archivo_periodo cp
      JOIN carga.carga_archivo c ON c.id = cp.carga_id
     WHERE cp.periodo = ANY (p_periodos)
       AND c.id <> p_carga_id
       AND COALESCE(c.resultado, '') NOT IN ('Rechazado', 'Error')
     ORDER BY CASE WHEN c.estado IN ('Cargado', 'Finalizado', 'Notificado') THEN 0 ELSE 1 END, c.id DESC
     LIMIT 1;

    IF v_id IS NULL THEN
        INSERT INTO carga.carga_archivo_periodo (carga_id, periodo)
        SELECT DISTINCT p_carga_id, x FROM unnest(p_periodos) AS x
        ON CONFLICT DO NOTHING;

        UPDATE carga.carga_archivo
           SET periodos = (SELECT string_agg(DISTINCT x, ', ' ORDER BY x) FROM unnest(p_periodos) AS x)
         WHERE id = p_carga_id;

        RETURN QUERY SELECT 'OK'::varchar, NULL::varchar, NULL::integer, NULL::varchar;
    ELSIF v_estado IN ('Cargado', 'Finalizado', 'Notificado') THEN
        RETURN QUERY SELECT 'RECHAZADO'::varchar, v_periodo, v_id, v_estado;
    ELSE
        RETURN QUERY SELECT 'BLOQUEADO'::varchar, v_periodo, v_id, v_estado;
    END IF;
END;
$$;

-- ---------------------------------------------------------------------
--  BulkLoad: inserta los registros validos (JSON) en DataProcesada.
--  Si el CodigoProducto ya existe no se inserta y se devuelve insertado = false
--  para reportarlo como "Existente". ON CONFLICT cubre cargas simultaneas.
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION carga.fn_insertar_registros(p_carga_id integer, p_registros jsonb)
RETURNS TABLE (fila integer, codigo_producto varchar, insertado boolean)
LANGUAGE sql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
    WITH entrada AS (
        SELECT r.*
          FROM jsonb_to_recordset(p_registros) AS r(
                fila integer, periodo varchar, codigo_producto varchar, nombre_producto varchar, precio numeric)
    ),
    insertados AS (
        INSERT INTO carga.data_procesada (carga_id, fila, periodo, codigo_producto, nombre_producto, precio)
        SELECT p_carga_id, e.fila, e.periodo, e.codigo_producto, e.nombre_producto, e.precio
          FROM entrada e
         WHERE NOT EXISTS (SELECT 1 FROM carga.data_procesada d WHERE d.codigo_producto = e.codigo_producto)
         ORDER BY e.fila
        ON CONFLICT ON CONSTRAINT uq_data_procesada_codigo DO NOTHING
        RETURNING data_procesada.fila
    )
    SELECT e.fila, e.codigo_producto::varchar, (i.fila IS NOT NULL)
      FROM entrada e
      LEFT JOIN insertados i ON i.fila = e.fila
     ORDER BY e.fila;
$$;

-- ---------------------------------------------------------------------
--  BulkLoad: guarda los registros fallidos (tabla de auditoria)
-- ---------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE carga.sp_registrar_fallidos(p_carga_id integer, p_fallidos jsonb)
LANGUAGE sql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
    INSERT INTO carga.detalle_carga (carga_id, fila, codigo_producto, motivo, detalle, datos)
    SELECT p_carga_id, f.fila, left(f.codigo_producto, 50), f.motivo, left(f.detalle, 500), f.datos
      FROM jsonb_to_recordset(p_fallidos) AS f(
            fila integer, codigo_producto varchar, motivo varchar, detalle varchar, datos jsonb);
$$;

-- ---------------------------------------------------------------------
--  BulkLoad: EnProceso -> Cargado (los datos ya estan en DataProcesada)
-- ---------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE carga.sp_marcar_cargado(p_carga_id integer, p_registros_insertados integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
BEGIN
    UPDATE carga.carga_archivo
       SET estado = 'Cargado', registros_insertados = p_registros_insertados
     WHERE id = p_carga_id AND estado = 'EnProceso';

    IF NOT FOUND THEN
        RAISE EXCEPTION 'La carga % no esta en estado EnProceso', p_carga_id;
    END IF;

    INSERT INTO carga.carga_archivo_historial (carga_id, estado, detalle, origen)
    VALUES (p_carga_id, 'Cargado', p_registros_insertados || ' registros insertados', 'bulkload-worker');
END;
$$;

-- ---------------------------------------------------------------------
--  BulkLoad / Control: cierra la carga (Finalizado) con su resultado.
--  Resultado: Exitoso, ConObservaciones, Rechazado o Error.
-- ---------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE carga.sp_finalizar_carga(
    p_carga_id integer, p_total integer, p_insertados integer, p_fallidos integer,
    p_resultado varchar, p_mensaje varchar, p_origen varchar)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
BEGIN
    UPDATE carga.carga_archivo
       SET estado = 'Finalizado',
           resultado = p_resultado,
           mensaje = left(p_mensaje, 1000),
           total_registros = p_total,
           registros_insertados = p_insertados,
           registros_fallidos = p_fallidos,
           fecha_fin = now()
     WHERE id = p_carga_id AND estado IN ('Pendiente', 'EnProceso', 'Cargado');

    IF NOT FOUND THEN
        RAISE EXCEPTION 'La carga % no se puede finalizar desde su estado actual', p_carga_id;
    END IF;

    INSERT INTO carga.carga_archivo_historial (carga_id, estado, detalle, origen)
    VALUES (p_carga_id, 'Finalizado', left(p_resultado || COALESCE(': ' || p_mensaje, ''), 1000), p_origen);
END;
$$;

-- ---------------------------------------------------------------------
--  Notification: Finalizado -> Notificado (idempotente)
-- ---------------------------------------------------------------------
CREATE OR REPLACE PROCEDURE carga.sp_marcar_notificado(p_carga_id integer, p_destinatario varchar)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
BEGIN
    UPDATE carga.carga_archivo
       SET estado = 'Notificado', fecha_notificacion = now()
     WHERE id = p_carga_id AND estado = 'Finalizado';

    IF FOUND THEN
        INSERT INTO carga.carga_archivo_historial (carga_id, estado, detalle, origen)
        VALUES (p_carga_id, 'Notificado', 'Correo enviado a ' || p_destinatario, 'notification-worker');
    END IF;
END;
$$;

-- ---------------------------------------------------------------------
--  Consultas
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION carga.fn_obtener_carga(p_carga_id integer)
RETURNS TABLE (
    id integer, nombre_archivo varchar, usuario varchar, usuario_id integer, periodos varchar,
    estado varchar, resultado varchar, mensaje varchar, total_registros integer,
    registros_insertados integer, registros_fallidos integer, tamano_bytes bigint, ruta_archivo varchar,
    fecha_registro timestamptz, fecha_inicio_proceso timestamptz, fecha_fin timestamptz,
    fecha_notificacion timestamptz)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
    SELECT c.id, c.nombre_archivo, c.usuario, c.usuario_id, c.periodos, c.estado, c.resultado, c.mensaje,
           c.total_registros, c.registros_insertados, c.registros_fallidos, c.tamano_bytes, c.ruta_archivo,
           c.fecha_registro, c.fecha_inicio_proceso, c.fecha_fin, c.fecha_notificacion
      FROM carga.carga_archivo c
     WHERE c.id = p_carga_id;
$$;

-- p_usuario_id NULL = ver las cargas de todos los usuarios (Administrador / Consulta)
CREATE OR REPLACE FUNCTION carga.fn_listar_cargas(p_usuario_id integer, p_limite integer)
RETURNS TABLE (
    id integer, nombre_archivo varchar, usuario varchar, periodos varchar, estado varchar,
    resultado varchar, total_registros integer, registros_insertados integer,
    registros_fallidos integer, fecha_registro timestamptz, fecha_fin timestamptz)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
    SELECT c.id, c.nombre_archivo, c.usuario, c.periodos, c.estado, c.resultado, c.total_registros,
           c.registros_insertados, c.registros_fallidos, c.fecha_registro, c.fecha_fin
      FROM carga.carga_archivo c
     WHERE p_usuario_id IS NULL OR c.usuario_id = p_usuario_id
     ORDER BY c.id DESC
     LIMIT p_limite;
$$;

CREATE OR REPLACE FUNCTION carga.fn_listar_historial(p_carga_id integer)
RETURNS TABLE (estado varchar, detalle varchar, origen varchar, fecha timestamptz)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
    SELECT h.estado, h.detalle, h.origen, h.fecha
      FROM carga.carga_archivo_historial h
     WHERE h.carga_id = p_carga_id
     ORDER BY h.fecha, h.id;
$$;

CREATE OR REPLACE FUNCTION carga.fn_listar_fallidos(p_carga_id integer, p_pagina integer, p_tamano integer)
RETURNS TABLE (fila integer, codigo_producto varchar, motivo varchar, detalle varchar, total_filas bigint)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
    SELECT d.fila, d.codigo_producto, d.motivo, d.detalle, count(*) OVER ()
      FROM carga.detalle_carga d
     WHERE d.carga_id = p_carga_id
     ORDER BY d.fila, d.id
     LIMIT p_tamano OFFSET (p_pagina - 1) * p_tamano;
$$;

CREATE OR REPLACE FUNCTION carga.fn_listar_registros(p_carga_id integer, p_pagina integer, p_tamano integer)
RETURNS TABLE (
    fila integer, periodo varchar, codigo_producto varchar, nombre_producto varchar,
    precio numeric, total_filas bigint)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = carga, pg_temp
AS $$
    SELECT d.fila, d.periodo, d.codigo_producto, d.nombre_producto, d.precio, count(*) OVER ()
      FROM carga.data_procesada d
     WHERE d.carga_id = p_carga_id
     ORDER BY d.fila
     LIMIT p_tamano OFFSET (p_pagina - 1) * p_tamano;
$$;
