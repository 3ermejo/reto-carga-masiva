import type { HistorialEstado } from '../types';
import { estadoLabels, formatDate } from '../utils/format';

export default function Timeline({ historial }: { historial: HistorialEstado[] }) {
  return (
    <ol className="timeline">
      {historial.map((item, index) => (
        <li key={`${item.estado}-${index}`}>
          <strong>{estadoLabels[item.estado]}</strong>
          <span className="muted-inline">
            {' '}
            · {formatDate(item.fecha)} · {item.origen}
          </span>
          {item.detalle && <div className="timeline-detail">{item.detalle}</div>}
        </li>
      ))}
    </ol>
  );
}
