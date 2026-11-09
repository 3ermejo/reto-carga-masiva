import type {
  CargaAceptada,
  CargaDetalle,
  CargaResumen,
  PagedResult,
  RegistroFallido,
  RegistroProcesado,
} from '../types';
import { request, send } from './httpClient';

export function subirArchivo(archivo: File, token: string): Promise<CargaAceptada> {
  const form = new FormData();
  form.append('archivo', archivo);
  return request<CargaAceptada>('/api/cargas', { method: 'POST', body: form }, token);
}

export function listarCargas(token: string): Promise<CargaResumen[]> {
  return request<CargaResumen[]>('/api/cargas', {}, token);
}

export function obtenerCarga(idCarga: number, token: string): Promise<CargaDetalle> {
  return request<CargaDetalle>(`/api/cargas/${idCarga}`, {}, token);
}

export function listarRegistros(idCarga: number, pagina: number, token: string): Promise<PagedResult<RegistroProcesado>> {
  return request<PagedResult<RegistroProcesado>>(`/api/cargas/${idCarga}/registros?pagina=${pagina}&tamano=20`, {}, token);
}

export function listarFallidos(idCarga: number, pagina: number, token: string): Promise<PagedResult<RegistroFallido>> {
  return request<PagedResult<RegistroFallido>>(`/api/cargas/${idCarga}/fallidos?pagina=${pagina}&tamano=20`, {}, token);
}

/** Descarga el Excel original (se usa fetch porque la descarga necesita el JWT). */
export async function descargarArchivo(idCarga: number, nombreArchivo: string, token: string): Promise<void> {
  const response = await send(`/api/cargas/${idCarga}/archivo`, {}, token);
  const blob = await response.blob();

  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = nombreArchivo;
  link.click();
  URL.revokeObjectURL(url);
}
