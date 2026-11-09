import { useCallback, useEffect, useState } from 'react';
import { listarCargas } from '../api/cargasApi';
import { ApiError } from '../api/httpClient';
import CargasTable from '../components/CargasTable';
import UploadForm from '../components/UploadForm';
import { POLLING_INTERVAL_MS } from '../config';
import type { AuthSession, CargaResumen } from '../types';
import { isInProgress } from '../utils/format';

interface Props {
  session: AuthSession;
  onVerDetalle: (idCarga: number) => void;
  onUnauthorized: () => void;
}

export default function CargasPage({ session, onVerDetalle, onUnauthorized }: Props) {
  const [cargas, setCargas] = useState<CargaResumen[]>([]);
  const [error, setError] = useState<string | null>(null);
  const token = session.accessToken;
  const puedeCargar = session.usuario.rol !== 'Consulta';

  const cargarHistorial = useCallback(async () => {
    try {
      setCargas(await listarCargas(token));
      setError(null);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        onUnauthorized();
        return;
      }
      setError('No se pudo obtener el historial de cargas.');
    }
  }, [token, onUnauthorized]);

  useEffect(() => {
    cargarHistorial();
  }, [cargarHistorial]);

  // Polling: mientras haya cargas en curso se consulta el estado cada pocos segundos
  const hayCargasEnCurso = cargas.some((c) => isInProgress(c.estado, c.fechaRegistro));

  useEffect(() => {
    if (!hayCargasEnCurso) return;

    const timer = window.setInterval(cargarHistorial, POLLING_INTERVAL_MS);
    return () => window.clearInterval(timer);
  }, [hayCargasEnCurso, cargarHistorial]);

  return (
    <>
      {puedeCargar ? (
        <UploadForm token={token} onUploaded={cargarHistorial} onUnauthorized={onUnauthorized} />
      ) : (
        <p className="alert alert-info">Tu usuario es de solo consulta: puedes revisar las cargas pero no subir archivos.</p>
      )}

      {error && <p className="alert alert-error">{error}</p>}

      <CargasTable cargas={cargas} onRefresh={cargarHistorial} onVerDetalle={onVerDetalle} />
    </>
  );
}
