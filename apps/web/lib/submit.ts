export async function submit(path: string, body?: FormData | object) {
  const response = await fetch(`/api${path}`, {
    method: "POST",
    headers:
      body instanceof FormData
        ? undefined
        : { "Content-Type": "application/json" },
    body:
      body instanceof FormData ? body : body ? JSON.stringify(body) : undefined,
  });
  if (response.status === 401 && path !== "/auth/login") {
    window.location.assign("/login");
    throw new Error("La sesión expiró.");
  }
  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message ?? "No se pudo guardar. Intenta nuevamente.");
  }
  return response.status === 204 ? null : response.json();
}
