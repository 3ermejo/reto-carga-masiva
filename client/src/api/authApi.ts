import type { AuthSession } from '../types';
import { request } from './httpClient';

// credentials: 'include' hace que el navegador guarde y envie la cookie HttpOnly del refresh token

export function login(email: string, password: string): Promise<AuthSession> {
  return request<AuthSession>('/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
    credentials: 'include',
  });
}

export function refresh(): Promise<AuthSession> {
  return request<AuthSession>('/auth/refresh', { method: 'POST', credentials: 'include' });
}

export function logout(): Promise<void> {
  return request<void>('/auth/logout', { method: 'POST', credentials: 'include' });
}
