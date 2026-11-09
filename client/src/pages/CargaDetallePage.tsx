import { useCallback, useEffect, useState } from 'react';
import { descargarArchivo, obtenerCarga } from '../api/cargasApi';
import { ApiError } from '../api/httpClient';
import { EstadoBadge, ResultadoBadge } from '../components/Badges';
import FallidosTable from '../components/FallidosTable';
import RegistrosTable from '../components/RegistrosTable';
import Timeline from '../components/Timeline';
import { POLLING_INTERVAL_MS } from '../config';
import type { CargaDetalle } from '../types';
import { formatDate, isInProgress } from '../utils/format';

interface Props {
  idCarga: number;
  token: string;
  onVolver: () => void;
  onUnauthorized: () => void;
}

type Tab = 'registros' | 'fallidos';

export default function CargaDetallePage({ idCarga, token, onVolver, onUnauthorized }: Props) {
  const [carga, setCarga] = useState<CargaDetalle | null>(null);
  const [tab, setTab] = useState<Tab>('registros');
  const [error, setError] = useState<string | null>(null);

  const cargarDetalle = useCallback(async () => {
    try {
      setCarga(await obtenerCarga(idCarga, token));
      setError(null);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        onUnauthorized();
        return;
      }
      setError(err instanceof ApiError && err.status === 404 ? 'La carga no existe.' : 'No se pudo obtener el detalle.');
    }
  }, [idCarga, token, onUnauthorized]);

  useEffect(() => {
    cargarDetalle();
  }, [cargarDetalle]);

  const enCurso = carga !== null && isInProgress(carga.estado, carga.fechaRegistro);

  useEffect(() => {
    if (!enCurso) return;

    const timer = window.setInterval(cargarDetalle, POLLING_INTERVAL_MS);
    return () => window.clearInterval(timer);
  }, [enCurso, cargarDetalle]);

  async function handleDescargar() {
    if (!carga) return;
    try {
      await descargarArchivo(carga.id, carga.nombreArchivo, token);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'No se pudo descargar el archivo.');
    }
  }

  return (
    <>
      <button type="button" className="btn btn-link" onClick={onVolver}>
        ← Volver al historial
      </button>

      {error && <p className="alert alert-error">{error}</p>}

      {carga && (
        <>
          <section className="card">
            <div className="card-header">
              <h2>
                Carga #{carga.id} · {carga.nombreArchivo}
              </h2>
              <button type="button" className="btn btn-secondary" onClick={handleDescargar}>
                Descargar Excel
              </button>
            </div>

            <div className="summary">
              <div>
                <span className="label">Estado</span>
                <EstadoBadge estado={carga.estado} />
              </div>
              <div>
                <span className="label">Resultado</span>
                <ResultadoBadge resultado={carga.resultado} />
              </div>
              <div>
                <span className="label">Periodos</span>
                {carga.periodos ?? '-'}
              </div>
              <div>
                <span className="label">Registros leídos</span>
                {carga.totalRegistros}
              </div>
              <div>
                <span className="label">Insertados</span>
                {carga.registrosInsertados}
              </div>
              <div>
                <span className="label">No cargados</span>
                {carga.registrosFallidos}
              </div>
              <div>
                <span className="label">Subido por</span>
                {carga.usuario}
              </div>
              <div>
                <span className="label">Fecha de registro</span>
                {formatDate(carga.fechaRegistro)}
              </div>
              <div>
                <span className="label">Fecha de fin</span>
                {formatDate(carga.fechaFin)}
              </div>
            </div>

            {carga.mensaje && <p className="alert alert-info">{carga.mensaje}</p>}
          </section>

          <section className="card">
            <h3>Trazabilidad</h3>
            <Timeline historial={carga.historial} />
          </section>

          <section className="card">
            <div className="tabs">
              <button
                type="button"
                className={tab === 'registros' ? 'tab active' : 'tab'}
                onClick={() => setTab('registros')}
              >
                Registros cargados ({carga.registrosInsertados})
              </button>
              <button
                type="button"
                className={tab === 'fallidos' ? 'tab active' : 'tab'}
                onClick={() => setTab('fallidos')}
              >
                No cargados ({carga.registrosFallidos})
              </button>
            </div>

            {tab === 'registros' ? (
              <RegistrosTable idCarga={carga.id} token={token} version={carga.estado} />
            ) : (
              <FallidosTable idCarga={carga.id} token={token} version={carga.estado} />
            )}
          </section>
        </>
      )}
    </>
  );
}
