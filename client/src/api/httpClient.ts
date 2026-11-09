import { API_URL } from '../config';

export class ApiError extends Error {
  readonly status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

/**
 * Wrapper de fetch: arma la URL del Gateway, agrega el JWT y convierte las
 * respuestas de error (ProblemDetails) en ApiError con un mensaje legible.
 */
export async function send(path: string, options: RequestInit = {}, token?: string): Promise<Response> {
  const headers = new Headers(options.headers);

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  // Con FormData el navegador arma el Content-Type multipart con su boundary
  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  const response = await fetch(`${API_URL}${path}`, { ...options, headers });

  if (!response.ok) {
    throw new ApiError(await readErrorMessage(response), response.status);
  }

  return response;
}

export async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  const response = await send(path, options, token);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

async function readErrorMessage(response: Response): Promise<string> {
  if (response.status === 429) return 'Demasiadas solicitudes. Espere un momento e intente nuevamente.';
  if (response.status === 403) return 'No tiene permiso para realizar esta acción.';

  try {
    const problem = await response.json();

    // Errores de validacion de ASP.NET Core: { errors: { Campo: ["mensaje"] } }
    if (problem.errors) {
      return Object.values(problem.errors as Record<string, string[]>).flat().join(' ');
    }

    return problem.title ?? 'Ocurrió un error inesperado.';
  } catch {
    return 'Ocurrió un error inesperado.';
  }
}
