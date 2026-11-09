import { useEffect, useState } from 'react';
import { listarFallidos } from '../api/cargasApi';
import type { PagedResult, RegistroFallido } from '../types';
import Pagination from './Pagination';

interface Props {
  idCarga: number;
  token: string;
  version: string;
}

const motivoLabels: Record<RegistroFallido['motivo'], string> = {
  Existente: 'Existente',
  DatoInvalido: 'Dato inválido',
  PeriodoRechazado: 'Periodo rechazado',
};

export default function FallidosTable({ idCarga, token, version }: Props) {
  const [pagina, setPagina] = useState(1);
  const [resultado, setResultado] = useState<PagedResult<RegistroFallido> | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listarFallidos(idCarga, pagina, token)
      .then((data) => {
        setResultado(data);
        setError(null);
      })
      .catch(() => setError('No se pudieron obtener los registros no cargados.'));
  }, [idCarga, pagina, token, version]);

  if (error) return <p className="alert alert-error">{error}</p>;
  if (!resultado || resultado.total === 0) return <p className="muted">No hay registros fallidos.</p>;

  return (
    <>
      <div className="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>Fila</th>
              <th>Código</th>
              <th>Motivo</th>
              <th>Detalle</th>
            </tr>
          </thead>
          <tbody>
            {resultado.items.map((fallido, index) => (
              <tr key={`${fallido.fila}-${index}`}>
                <td>{fallido.fila}</td>
                <td>{fallido.codigoProducto ?? '-'}</td>
                <td>
                  <span className={`badge badge-motivo-${fallido.motivo.toLowerCase()}`}>{motivoLabels[fallido.motivo]}</span>
                </td>
                <td className="wrap">{fallido.detalle}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pagina={pagina} tamano={resultado.tamano} total={resultado.total} onChange={setPagina} />
    </>
  );
}
