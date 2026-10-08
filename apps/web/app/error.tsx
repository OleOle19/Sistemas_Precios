"use client";
export default function ErrorPage({ reset }: { reset: () => void }) {
  return (
    <div className="card">
      <h2>No pudimos cargar esta página</h2>
      <p>Comprueba que el servicio esté disponible e intenta nuevamente.</p>
      <button className="primary-button" onClick={reset}>
        Intentar nuevamente
      </button>
    </div>
  );
}
