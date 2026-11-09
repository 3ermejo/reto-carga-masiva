import type { EstadoCarga, ResultadoCarga } from '../types';
import { estadoLabels, resultadoLabels } from '../utils/format';

export function EstadoBadge({ estado }: { estado: EstadoCarga }) {
  return <span className={`badge badge-estado-${estado.toLowerCase()}`}>{estadoLabels[estado]}</span>;
}

export function ResultadoBadge({ resultado }: { resultado: ResultadoCarga | null }) {
  if (!resultado) return <span className="muted-inline">-</span>;
  return <span className={`badge badge-resultado-${resultado.toLowerCase()}`}>{resultadoLabels[resultado]}</span>;
}
