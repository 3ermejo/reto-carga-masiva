-- =====================================================================
--  V002 - Procedimientos de seguridad (Auth.Api)
--  Todos son SECURITY DEFINER: el usuario de Auth.Api no tiene acceso
--  directo a las tablas, solo puede ejecutar estas funciones.
-- =====================================================================

CREATE OR REPLACE FUNCTION seguridad.fn_obtener_usuario_por_email(p_email varchar)
RETURNS TABLE (
    id integer, email varchar, nombre_completo varchar, password_hash varchar,
    rol varchar, activo boolean, bloqueado boolean)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = seguridad, pg_temp
AS $$
    SELECT u.id, u.email, u.nombre_completo, u.password_hash, u.rol, u.activo,
           (u.bloqueado_hasta IS NOT NULL AND u.bloqueado_hasta > now())
    FROM seguridad.usuario u
    WHERE u.email = lower(trim(p_email));
$$;

CREATE OR REPLACE FUNCTION seguridad.fn_obtener_usuario_por_id(p_usuario_id integer)
RETURNS TABLE (
    id integer, email varchar, nombre_completo varchar, password_hash varchar,
    rol varchar, activo boolean, bloqueado boolean)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = seguridad, pg_temp
AS $$
    SELECT u.id, u.email, u.nombre_completo, u.password_hash, u.rol, u.activo,
           (u.bloqueado_hasta IS NOT NULL AND u.bloqueado_hasta > now())
    FROM seguridad.usuario u
    WHERE u.id = p_usuario_id;
$$;

-- Suma un intento fallido y bloquea la cuenta al llegar al maximo (atomico)
CREATE OR REPLACE PROCEDURE seguridad.sp_registrar_login_fallido(
    p_usuario_id integer, p_max_intentos integer, p_minutos_bloqueo integer)
LANGUAGE plpgsql SECURITY DEFINER SET search_path = seguridad, pg_temp
AS $$
BEGIN
    UPDATE seguridad.usuario
       SET intentos_fallidos = CASE WHEN intentos_fallidos + 1 >= p_max_intentos THEN 0 ELSE intentos_fallidos + 1 END,
           bloqueado_hasta   = CASE WHEN intentos_fallidos + 1 >= p_max_intentos
                                    THEN now() + make_interval(mins => p_minutos_bloqueo)
                                    ELSE bloqueado_hasta END
     WHERE id = p_usuario_id;
END;
$$;

CREATE OR REPLACE PROCEDURE seguridad.sp_registrar_login_exitoso(p_usuario_id integer)
LANGUAGE sql SECURITY DEFINER SET search_path = seguridad, pg_temp
AS $$
    UPDATE seguridad.usuario
       SET intentos_fallidos = 0, bloqueado_hasta = NULL
     WHERE id = p_usuario_id;
$$;

CREATE OR REPLACE PROCEDURE seguridad.sp_guardar_refresh_token(
    p_usuario_id integer, p_token_hash varchar, p_dias_vigencia integer)
LANGUAGE sql SECURITY DEFINER SET search_path = seguridad, pg_temp
AS $$
    INSERT INTO seguridad.refresh_token (usuario_id, token_hash, fecha_expiracion)
    VALUES (p_usuario_id, p_token_hash, now() + make_interval(days => p_dias_vigencia));
$$;

-- Rotacion del refresh token: el token usado se revoca y se emite uno nuevo.
-- Si alguien reutiliza un token ya revocado (posible robo) se revocan todos
-- los tokens del usuario y se devuelve NULL.
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

CREATE OR REPLACE PROCEDURE seguridad.sp_revocar_refresh_token(p_token_hash varchar)
LANGUAGE sql SECURITY DEFINER SET search_path = seguridad, pg_temp
AS $$
    UPDATE seguridad.refresh_token
       SET fecha_revocacion = now()
     WHERE token_hash = p_token_hash AND fecha_revocacion IS NULL;
$$;
