import type { CargaResumen } from '../types';
import { formatDate } from '../utils/format';
import { EstadoBadge, ResultadoBadge } from './Badges';

interface Props {
  cargas: CargaResumen[];
  onRefresh: () => void;
  onVerDetalle: (idCarga: number) => void;
}

export default function CargasTable({ cargas, onRefresh, onVerDetalle }: Props) {
  return (
    <section className="card">
      <div className="card-header">
        <h2>Historial de cargas</h2>
        <button type="button" className="btn btn-secondary" onClick={onRefresh}>
          Actualizar
        </button>
      </div>

      {cargas.length === 0 ? (
        <p className="muted">Todavía no hay cargas registradas.</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Archivo</th>
                <th>Periodos</th>
                <th>Estado</th>
                <th>Resultado</th>
                <th>Leídos</th>
                <th>Insertados</th>
                <th>No cargados</th>
                <th>Usuario</th>
                <th>Fecha</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {cargas.map((carga) => (
                <tr key={carga.id}>
                  <td>{carga.id}</td>
                  <td className="ellipsis">{carga.nombreArchivo}</td>
                  <td>{carga.periodos ?? '-'}</td>
                  <td>
                    <EstadoBadge estado={carga.estado} />
                  </td>
                  <td>
                    <ResultadoBadge resultado={carga.resultado} />
                  </td>
                  <td>{carga.totalRegistros}</td>
                  <td>{carga.registrosInsertados}</td>
                  <td>{carga.registrosFallidos}</td>
                  <td className="ellipsis">{carga.usuario}</td>
                  <td>{formatDate(carga.fechaRegistro)}</td>
                  <td>
                    <button type="button" className="btn btn-link" onClick={() => onVerDetalle(carga.id)}>
                      Ver detalle
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
