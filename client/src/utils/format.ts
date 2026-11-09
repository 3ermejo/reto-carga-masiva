import type { EstadoCarga, ResultadoCarga } from '../types';

export const estadoLabels: Record<EstadoCarga, string> = {
  Pendiente: 'Pendiente',
  EnProceso: 'En proceso',
  Cargado: 'Cargado',
  Finalizado: 'Finalizado',
  Notificado: 'Notificado',
};

export const resultadoLabels: Record<ResultadoCarga, string> = {
  Exitoso: 'Exitoso',
  ConObservaciones: 'Con observaciones',
  Rechazado: 'Rechazado',
  Error: 'Error',
};

export function formatDate(value: string | null): string {
  return value ? new Date(value).toLocaleString('es-PE') : '-';
}

export function formatPrice(value: number): string {
  return value.toLocaleString('es-PE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/** Una carga sigue "viva" hasta que se notifica (o se cae la notificacion despues de unos minutos). */
export function isInProgress(estado: EstadoCarga, fechaRegistro: string): boolean {
  if (estado === 'Notificado') return false;
  if (estado === 'Finalizado') return Date.now() - new Date(fechaRegistro).getTime() < 5 * 60 * 1000;
  return true;
}
