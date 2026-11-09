import { useEffect, useState } from 'react';
import { listarRegistros } from '../api/cargasApi';
import type { PagedResult, RegistroProcesado } from '../types';
import { formatPrice } from '../utils/format';
import Pagination from './Pagination';

interface Props {
  idCarga: number;
  token: string;
  /** Cuando cambia (por ejemplo el estado de la carga) se vuelve a consultar */
  version: string;
}

export default function RegistrosTable({ idCarga, token, version }: Props) {
  const [pagina, setPagina] = useState(1);
  const [resultado, setResultado] = useState<PagedResult<RegistroProcesado> | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    listarRegistros(idCarga, pagina, token)
      .then((data) => {
        setResultado(data);
        setError(null);
      })
      .catch(() => setError('No se pudieron obtener los registros.'));
  }, [idCarga, pagina, token, version]);

  if (error) return <p className="alert alert-error">{error}</p>;
  if (!resultado || resultado.total === 0) return <p className="muted">No hay registros cargados.</p>;

  return (
    <>
      <div className="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>Fila</th>
              <th>Periodo</th>
              <th>Código</th>
              <th>Nombre</th>
              <th className="number">Precio</th>
            </tr>
          </thead>
          <tbody>
            {resultado.items.map((registro) => (
              <tr key={registro.fila}>
                <td>{registro.fila}</td>
                <td>{registro.periodo}</td>
                <td>{registro.codigoProducto}</td>
                <td>{registro.nombreProducto}</td>
                <td className="number">{formatPrice(registro.precio)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pagina={pagina} tamano={resultado.tamano} total={resultado.total} onChange={setPagina} />
    </>
  );
}
