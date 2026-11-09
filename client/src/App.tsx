import { useCallback, useEffect, useState } from 'react';
import { logout, refresh } from './api/authApi';
import Header from './components/Header';
import CargaDetallePage from './pages/CargaDetallePage';
import CargasPage from './pages/CargasPage';
import LoginPage from './pages/LoginPage';
import type { AuthSession } from './types';

const REFRESH_MARGIN_MS = 60_000;

export default function App() {
  // El JWT se guarda en memoria; el refresh token esta en una cookie HttpOnly.
  const [session, setSession] = useState<AuthSession | null>(null);
  const [restaurandoSesion, setRestaurandoSesion] = useState(true);
  const [cargaSeleccionada, setCargaSeleccionada] = useState<number | null>(null);

  // Al abrir o recargar la pagina se recupera la sesion con la cookie del refresh token
  useEffect(() => {
    refresh()
      .then(setSession)
      .catch(() => setSession(null))
      .finally(() => setRestaurandoSesion(false));
  }, []);

  // Renueva el JWT un minuto antes de que venza
  useEffect(() => {
    if (!session) return;

    const msHastaRenovar = new Date(session.expiresAtUtc).getTime() - Date.now() - REFRESH_MARGIN_MS;
    const timer = window.setTimeout(async () => {
      try {
        setSession(await refresh());
      } catch {
        setSession(null);
      }
    }, Math.max(msHastaRenovar, 0));

    return () => window.clearTimeout(timer);
  }, [session]);

  const handleLogout = useCallback(async () => {
    try {
      await logout();
    } catch {
      // Si falla el logout igual se limpia la sesion local
    }
    setSession(null);
    setCargaSeleccionada(null);
  }, []);

  if (restaurandoSesion) {
    return <p className="loading">Cargando...</p>;
  }

  if (!session) {
    return <LoginPage onAuthenticated={setSession} />;
  }

  return (
    <div className="layout">
      <Header usuario={session.usuario} onLogout={handleLogout} />
      <main className="content">
        {cargaSeleccionada === null ? (
          <CargasPage session={session} onVerDetalle={setCargaSeleccionada} onUnauthorized={handleLogout} />
        ) : (
          <CargaDetallePage
            idCarga={cargaSeleccionada}
            token={session.accessToken}
            onVolver={() => setCargaSeleccionada(null)}
            onUnauthorized={handleLogout}
          />
        )}
      </main>
    </div>
  );
}
