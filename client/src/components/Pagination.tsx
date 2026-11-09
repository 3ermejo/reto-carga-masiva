interface Props {
  pagina: number;
  tamano: number;
  total: number;
  onChange: (pagina: number) => void;
}

export default function Pagination({ pagina, tamano, total, onChange }: Props) {
  const totalPaginas = Math.max(1, Math.ceil(total / tamano));

  return (
    <div className="pagination">
      <button type="button" className="btn btn-secondary" disabled={pagina <= 1} onClick={() => onChange(pagina - 1)}>
        Anterior
      </button>
      <span>
        Página {pagina} de {totalPaginas} ({total} registros)
      </span>
      <button type="button" className="btn btn-secondary" disabled={pagina >= totalPaginas} onClick={() => onChange(pagina + 1)}>
        Siguiente
      </button>
    </div>
  );
}
