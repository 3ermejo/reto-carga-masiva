-- =====================================================================
--  V006 - Reenvio de la notificacion
--  Si BulkLoad finalizo la carga pero no pudo publicar la notificacion,
--  el reintento la encontraba en estado Finalizado y la omitia: el correo
--  nunca se enviaba y la carga no llegaba a Notificado.
--  Ahora, si la carga esta Finalizado (pero no Notificado), se devuelve
--  REENVIAR_NOTIFICACION para que BulkLoad vuelva a publicar el mensaje.
--  Notification es idempotente: si ya estaba notificada, no reenvia el correo.
-- =====================================================================

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

    IF v_estado = 'Notificado' THEN
        RETURN 'OMITIR';
    END IF;

    IF v_estado = 'Finalizado' THEN
        RETURN 'REENVIAR_NOTIFICACION';
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
