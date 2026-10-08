import { NextRequest } from "next/server";
const base = process.env.API_BASE_URL ?? "http://localhost:8080";
async function proxy(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const { path } = await context.params;
  if (path.some((p) => !/^[a-zA-Z0-9-]+$/.test(p)))
    return new Response("Ruta inválida", { status: 400 });
  const headers = new Headers();
  for (const name of ["cookie", "content-type", "origin", "range"]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  if (Number(request.headers.get("content-length") ?? 0) > 11 * 1024 * 1024)
    return Response.json(
      { message: "El archivo supera 10 MB." },
      { status: 413 },
    );
  try {
    let body: ArrayBuffer | undefined;
    if (request.body && !["GET", "HEAD"].includes(request.method)) {
      const reader = request.body.getReader();
      const chunks: Uint8Array[] = [];
      let size = 0;
      while (true) {
        const chunk = await reader.read();
        if (chunk.done) break;
        size += chunk.value.byteLength;
        if (size > 11 * 1024 * 1024) {
          await reader.cancel();
          return Response.json({ message: "El archivo supera 10 MB." }, { status: 413 });
        }
        chunks.push(chunk.value);
      }
      const bytes = new Uint8Array(size);
      let offset = 0;
      for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.byteLength; }
      body = bytes.buffer;
    }
    const response = await fetch(
      `${base}/${path.join("/")}${request.nextUrl.search}`,
      {
        method: request.method,
        headers,
        cache: "no-store",
        redirect: "manual",
        body,
        signal: AbortSignal.timeout(15000),
      },
    );
    const outgoing = new Headers();
    for (const name of [
      "content-type",
      "content-range",
      "accept-ranges",
      "content-length",
    ])
      if (response.headers.get(name))
        outgoing.set(name, response.headers.get(name)!);
    for (const cookie of response.headers.getSetCookie())
      outgoing.append("set-cookie", cookie);
    outgoing.set("cache-control", "no-store");
    outgoing.set("x-content-type-options", "nosniff");
    return new Response(response.body, {
      status: response.status,
      headers: outgoing,
    });
  } catch {
    return Response.json(
      { message: "El servicio no está disponible. Intenta nuevamente." },
      { status: 503 },
    );
  }
}
export const GET = proxy;
export const POST = proxy;
