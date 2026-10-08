import { test, expect } from "@playwright/test";
test("real session, supplier registration, no fabricated prices, missing key and logout", async ({
  page,
}) => {
  await page.goto("/");
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel("Correo", { exact: true }).fill("e2e@precios.local");
  await page.getByLabel("Contraseña").fill("TestOnlyPassword2026!");
  await page.getByRole("button", { name: "Entrar", exact: true }).click();
  await expect(
    page.getByRole("heading", {
      name: "Compara proveedores a partir de fotos y listas comerciales.",
    }),
  ).toBeVisible();
  await expect(
    page.getByText("Aún no hay precios de dos proveedores para comparar."),
  ).toBeVisible();
  await page.getByRole("link", { name: "Proveedores", exact: true }).click();
  await page.getByLabel("Nombre", { exact: true }).fill("Tienda de prueba");
  await page.getByRole("button", { name: "Registrar", exact: true }).click();
  await expect(page.getByText("Proveedor registrado.")).toBeVisible();
  await page.getByRole("link", { name: "Documentos", exact: true }).click();
  await expect(
    page.getByRole("button", { name: "Enviar para leer precios" }),
  ).toBeDisabled();
  // Exercise authenticated upload and worker through the real API without a billable API key.
  const suppliers = await (await page.request.get("/api/suppliers")).json();
  const upload = await page.request.post("/api/documents", {
    multipart: {
      supplierId: suppliers[0].id,
      sourceKind: "label",
      observedDate: new Date().toISOString().slice(0, 10),
      file: {
        name: "test.png",
        mimeType: "image/png",
        buffer: Buffer.from(
          "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j4AAAAABJRU5ErkJggg==",
          "base64",
        ),
      },
    },
  });
  expect(upload.status()).toBe(201);
  const doc = await upload.json();
  await page.goto(`/documents/${doc.id}`);
  await expect(
    page.getByText(
      "Configura la clave de OpenAI y vuelve a procesar el archivo.",
      { exact: true },
    ),
  ).toBeVisible({ timeout: 20000 });
  await page.screenshot({
    path: "test-results/document-failure.png",
    fullPage: true,
  });
  const comparison = await page.request.get("/api/comparisons/current");
  expect(await comparison.json()).toEqual([]);
  await page.getByRole("button", { name: "Cerrar sesión" }).click();
  await expect(page).toHaveURL(/\/login$/);
  const protectedResponse = await page.request.get("/api/suppliers");
  expect(protectedResponse.status()).toBe(401);
});
