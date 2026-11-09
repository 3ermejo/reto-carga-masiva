-- =====================================================================
--  V004 - Permisos (minimo privilegio)
--  Los usuarios de los microservicios los crea el migrador antes de
--  ejecutar los scripts. No tienen permisos sobre las tablas: solo pueden
--  ejecutar los procedimientos que necesitan.
-- =====================================================================

REVOKE ALL ON ALL TABLES IN SCHEMA seguridad, carga FROM PUBLIC;
REVOKE ALL ON ALL ROUTINES IN SCHEMA seguridad, carga FROM PUBLIC;

GRANT USAGE ON SCHEMA seguridad TO auth_svc;
GRANT USAGE ON SCHEMA carga TO control_svc, bulkload_svc, notification_svc;

-- Auth.Api
GRANT EXECUTE ON FUNCTION  seguridad.fn_obtener_usuario_por_email(varchar)          TO auth_svc;
GRANT EXECUTE ON FUNCTION  seguridad.fn_obtener_usuario_por_id(integer)             TO auth_svc;
GRANT EXECUTE ON PROCEDURE seguridad.sp_registrar_login_fallido(integer, integer, integer) TO auth_svc;
GRANT EXECUTE ON PROCEDURE seguridad.sp_registrar_login_exitoso(integer)            TO auth_svc;
GRANT EXECUTE ON PROCEDURE seguridad.sp_guardar_refresh_token(integer, varchar, integer) TO auth_svc;
GRANT EXECUTE ON FUNCTION  seguridad.fn_rotar_refresh_token(varchar, varchar, integer) TO auth_svc;
GRANT EXECUTE ON PROCEDURE seguridad.sp_revocar_refresh_token(varchar)              TO auth_svc;

-- Control.Api
GRANT EXECUTE ON FUNCTION  carga.fn_registrar_carga(varchar, varchar, integer, varchar, bigint) TO control_svc;
GRANT EXECUTE ON PROCEDURE carga.sp_finalizar_carga(integer, integer, integer, integer, varchar, varchar, varchar) TO control_svc;
GRANT EXECUTE ON FUNCTION  carga.fn_obtener_carga(integer)                          TO control_svc;
GRANT EXECUTE ON FUNCTION  carga.fn_listar_cargas(integer, integer)                 TO control_svc;
GRANT EXECUTE ON FUNCTION  carga.fn_listar_historial(integer)                       TO control_svc;
GRANT EXECUTE ON FUNCTION  carga.fn_listar_fallidos(integer, integer, integer)      TO control_svc;
GRANT EXECUTE ON FUNCTION  carga.fn_listar_registros(integer, integer, integer)     TO control_svc;

-- BulkLoad.Worker
GRANT EXECUTE ON FUNCTION  carga.fn_iniciar_proceso(integer)                        TO bulkload_svc;
GRANT EXECUTE ON FUNCTION  carga.fn_validar_periodos(integer, varchar[])           TO bulkload_svc;
GRANT EXECUTE ON FUNCTION  carga.fn_insertar_registros(integer, jsonb)              TO bulkload_svc;
GRANT EXECUTE ON PROCEDURE carga.sp_registrar_fallidos(integer, jsonb)              TO bulkload_svc;
GRANT EXECUTE ON PROCEDURE carga.sp_marcar_cargado(integer, integer)                TO bulkload_svc;
GRANT EXECUTE ON PROCEDURE carga.sp_finalizar_carga(integer, integer, integer, integer, varchar, varchar, varchar) TO bulkload_svc;

-- Notification.Worker
GRANT EXECUTE ON FUNCTION  carga.fn_obtener_carga(integer)                          TO notification_svc;
GRANT EXECUTE ON PROCEDURE carga.sp_marcar_notificado(integer, varchar)             TO notification_svc;
