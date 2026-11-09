export type Rol = 'Administrador' | 'Operador' | 'Consulta';

export interface Usuario {
  id: number;
  email: string;
  nombreCompleto: string;
  rol: Rol;
}

// El refresh token no llega aqui: viaja en una cookie HttpOnly que JavaScript no puede leer
export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  usuario: Usuario;
}

export type EstadoCarga = 'Pendiente' | 'EnProceso' | 'Cargado' | 'Finalizado' | 'Notificado';

export type ResultadoCarga = 'Exitoso' | 'ConObservaciones' | 'Rechazado' | 'Error';

export interface CargaResumen {
  id: number;
  nombreArchivo: string;
  usuario: string;
  periodos: string | null;
  estado: EstadoCarga;
  resultado: ResultadoCarga | null;
  totalRegistros: number;
  registrosInsertados: number;
  registrosFallidos: number;
  fechaRegistro: string;
  fechaFin: string | null;
}

export interface HistorialEstado {
  estado: EstadoCarga;
  detalle: string | null;
  origen: string;
  fecha: string;
}

export interface CargaDetalle extends CargaResumen {
  mensaje: string | null;
  tamanoBytes: number;
  fechaInicioProceso: string | null;
  fechaNotificacion: string | null;
  historial: HistorialEstado[];
}

export interface RegistroProcesado {
  fila: number;
  periodo: string;
  codigoProducto: string;
  nombreProducto: string;
  precio: number;
}

export interface RegistroFallido {
  fila: number;
  codigoProducto: string | null;
  motivo: 'Existente' | 'DatoInvalido' | 'PeriodoRechazado';
  detalle: string;
}

export interface PagedResult<T> {
  items: T[];
  pagina: number;
  tamano: number;
  total: number;
}

export interface CargaAceptada {
  idCarga: number;
  estado: EstadoCarga;
}
