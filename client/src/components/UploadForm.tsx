import { useRef, useState, type FormEvent } from 'react';
import { subirArchivo } from '../api/cargasApi';
import { ApiError } from '../api/httpClient';
import { MAX_FILE_SIZE_MB } from '../config';

interface Props {
  token: string;
  onUploaded: () => void;
  onUnauthorized: () => void;
}

interface Feedback {
  type: 'success' | 'error';
  text: string;
}

// Validacion rapida en el navegador; la validacion real la hace el backend
function validarArchivo(archivo: File): string | null {
  if (!archivo.name.toLowerCase().endsWith('.xlsx')) return 'Solo se permiten archivos Excel (.xlsx).';
  if (archivo.size === 0) return 'El archivo está vacío.';
  if (archivo.size > MAX_FILE_SIZE_MB * 1024 * 1024) return `El archivo supera el tamaño máximo de ${MAX_FILE_SIZE_MB} MB.`;
  return null;
}

export default function UploadForm({ token, onUploaded, onUnauthorized }: Props) {
  const [archivo, setArchivo] = useState<File | null>(null);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [subiendo, setSubiendo] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!archivo) return;

    const error = validarArchivo(archivo);
    if (error) {
      setFeedback({ type: 'error', text: error });
      return;
    }

    setSubiendo(true);
    setFeedback(null);

    try {
      const respuesta = await subirArchivo(archivo, token);
      setFeedback({ type: 'success', text: `Carga #${respuesta.idCarga} registrada. Se está procesando en segundo plano.` });
      setArchivo(null);
      if (inputRef.current) inputRef.current.value = '';
      onUploaded();
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        onUnauthorized();
        return;
      }
      setFeedback({ type: 'error', text: err instanceof ApiError ? err.message : 'No se pudo subir el archivo.' });
    } finally {
      setSubiendo(false);
    }
  }

  return (
    <section className="card">
      <h2>Subir Excel</h2>
      <p className="muted">
        Archivo .xlsx de hasta {MAX_FILE_SIZE_MB} MB con las columnas <code>Periodo</code>, <code>CodigoProducto</code>,{' '}
        <code>NombreProducto</code> y <code>Precio</code>.
      </p>

      <form className="upload-form" onSubmit={handleSubmit}>
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
          onChange={(e) => {
            setArchivo(e.target.files?.[0] ?? null);
            setFeedback(null);
          }}
        />
        <button type="submit" className="btn btn-primary" disabled={!archivo || subiendo}>
          {subiendo ? 'Subiendo...' : 'Subir'}
        </button>
      </form>

      {feedback && <p className={`alert alert-${feedback.type}`}>{feedback.text}</p>}
    </section>
  );
}
