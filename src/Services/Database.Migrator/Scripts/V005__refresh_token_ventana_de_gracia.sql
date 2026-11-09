-- =====================================================================
--  V005 - Rotacion de refresh token con ventana de gracia
--  Si dos peticiones usan el mismo refresh token casi al mismo tiempo
--  (por ejemplo, dos pestañas abiertas al recargar), la segunda solo se
--  rechaza; no se considera un robo. La reutilizacion de un token revocado
--  hace mas de 30 segundos si revoca todos los tokens del usuario.
-- =====================================================================

CREATE OR REPLACE FUNCTION seguridad.fn_rotar_refresh_token(
    p_token_hash varchar, p_nuevo_token_hash varchar, p_dias_vigencia integer)
RETURNS integer
LANGUAGE plpgsql SECURITY DEFINER SET search_path = seguridad, pg_temp
AS $$
DECLARE
    v_token seguridad.refresh_token%ROWTYPE;
BEGIN
    SELECT * INTO v_token
      FROM seguridad.refresh_token
     WHERE token_hash = p_token_hash
       FOR UPDATE;

    IF NOT FOUND THEN
        RETURN NULL;
    END IF;

    IF v_token.fecha_revocacion IS NOT NULL THEN
        IF v_token.reemplazado_por IS NOT NULL AND v_token.fecha_revocacion > now() - interval '30 seconds' THEN
            RETURN NULL;
        END IF;

        UPDATE seguridad.refresh_token
           SET fecha_revocacion = now()
         WHERE usuario_id = v_token.usuario_id AND fecha_revocacion IS NULL;
        RETURN NULL;
    END IF;

    IF v_token.fecha_expiracion <= now() THEN
        RETURN NULL;
    END IF;

    UPDATE seguridad.refresh_token
       SET fecha_revocacion = now(), reemplazado_por = p_nuevo_token_hash
     WHERE id = v_token.id;

    INSERT INTO seguridad.refresh_token (usuario_id, token_hash, fecha_expiracion)
    VALUES (v_token.usuario_id, p_nuevo_token_hash, now() + make_interval(days => p_dias_vigencia));

    RETURN v_token.usuario_id;
END;
$$;
