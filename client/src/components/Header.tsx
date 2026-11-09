import type { Usuario } from '../types';

interface Props {
  usuario: Usuario;
  onLogout: () => void;
}

export default function Header({ usuario, onLogout }: Props) {
  return (
    <header className="topbar">
      <strong>Carga Masiva</strong>
      <div className="topbar-user">
        <span>
          {usuario.nombreCompleto} <span className="role">{usuario.rol}</span>
        </span>
        <button type="button" className="btn btn-secondary" onClick={onLogout}>
          Cerrar sesión
        </button>
      </div>
    </header>
  );
}
