import { test, expect, request, type APIRequestContext } from "@playwright/test";
import { randomUUID } from "node:crypto";
const password = "TestOnlyPassword2026!";
const baseURL = "http://localhost:3000";
const unknownId = "00000000-0000-0000-0000-000000000001";
async function login(api: APIRequestContext, email: string, value = password) {
  expect((await api.post("/api/auth/login", { data: { email, password: value } })).status()).toBe(200);
}

test("anonymous and forged sessions cannot enter private pages or read/write data", async ({ page }) => {
  for (const path of ["/", "/suppliers", "/documents", `/documents/${unknownId}`, "/comparisons", "/history", "/team", "/account"]) {
    await page.goto(path);
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole("link", { name: "Proveedores", exact: true })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Cerrar sesión" })).toHaveCount(0);
  }
  for (const path of ["auth/me", "suppliers", "documents", `documents/${unknownId}/file`, "comparisons/current", "comparisons/history", "dashboard/summary", "settings/extraction", "team/users"]) {
    expect((await page.request.get(`/api/${path}`)).status()).toBe(401);
  }
  for (const path of ["suppliers", "documents", `documents/${unknownId}/reprocess`, `documents/${unknownId}/review`, "team/users", `team/users/${unknownId}/role`, "auth/password"]) {
    expect((await page.request.post(`/api/${path}`, { data: {} })).status()).toBe(401);
  }
  await page.context().addCookies([{ name: "sistemas-precios-session", value: "forged", domain: "localhost", path: "/" }]);
  await page.goto("/suppliers");
  await expect(page).toHaveURL(/\/login$/);
  expect((await page.request.get("/api/suppliers")).status()).toBe(401);
});

test("one business shares data across emails, with roles enforced in API and UI", async ({ page, browser }) => {
  const admin = await request.newContext({ baseURL });
  const analyst = await request.newContext({ baseURL });
  const viewer = await request.newContext({ baseURL });
  const suffix = randomUUID();
  const analystEmail = `analyst-${suffix}@example.org`;
  const viewerEmail = `viewer-${suffix}@example.net`;
  try {
    await login(admin, "e2e@precios.local");
    const me = await (await admin.get("/api/auth/me")).json();
    const create = (fullName: string, email: string, role: string, value = password) => admin.post("/api/team/users", { data: { fullName, email, role, password: value } });
    const analystResponse = await create("Ana Analista", analystEmail, "Analyst");
    expect(analystResponse.status()).toBe(201);
    const member = await analystResponse.json();
    expect((await create("Vera Consulta", viewerEmail, "Viewer")).status()).toBe(201);
    expect((await create("Duplicada", analystEmail.toUpperCase(), "Viewer")).status()).toBe(400);
    expect((await create("Débil", `weak-${suffix}@example.org`, "Viewer", "123")).status()).toBe(400);
    expect((await create("Rol inválido", `bad-${suffix}@example.org`, "1")).status()).toBe(400);
    expect((await admin.post(`/api/team/users/${me.id}/role`, { data: { role: "Disabled" } })).status()).toBe(400);
    const users = await (await admin.get("/api/team/users")).json();
    expect(users.every((u: object) => !Object.keys(u).some(key => /password/i.test(key)))).toBe(true);
    await login(analyst, analystEmail);
    await login(viewer, viewerEmail);
    const supplierName = `Negocio compartido ${suffix}`;
    expect((await analyst.post("/api/suppliers", { data: { name: supplierName } })).status()).toBe(201);
    for (const api of [admin, analyst, viewer]) {
      expect((await (await api.get("/api/suppliers")).json()).some((s: { name: string }) => s.name === supplierName)).toBe(true);
      expect((await (await api.get("/api/auth/me")).json()).workspaceName).toBe(me.workspaceName);
    }
    for (const api of [analyst, viewer]) {
      expect((await api.get("/api/team/users")).status()).toBe(403);
      expect((await api.post("/api/team/users", { data: {} })).status()).toBe(403);
      expect((await api.post(`/api/team/users/${member.id}/role`, { data: { role: "Admin" } })).status()).toBe(403);
    }
    for (const path of ["suppliers", "documents", `documents/${unknownId}/reprocess`, `documents/${unknownId}/review`]) {
      expect((await viewer.post(`/api/${path}`, { data: {} })).status()).toBe(403);
    }
    const readonlyContext = await browser.newContext({ storageState: await viewer.storageState() });
    const readonlyPage = await readonlyContext.newPage();
    await readonlyPage.goto(`${baseURL}/suppliers`);
    await expect(readonlyPage.getByText(supplierName, { exact: true })).toBeVisible();
    await expect(readonlyPage.getByRole("button", { name: "Registrar", exact: true })).toHaveCount(0);
    await expect(readonlyPage.getByRole("link", { name: "Equipo", exact: true })).toHaveCount(0);
    await readonlyPage.goto(`${baseURL}/documents`);
    await expect(readonlyPage.getByRole("button", { name: "Enviar para leer precios" })).toHaveCount(0);
    await readonlyPage.goto(`${baseURL}/team`);
    await expect(readonlyPage).toHaveURL(`${baseURL}/`);
    await readonlyContext.close();
    // Existing cookies immediately use the changed role, without signing in again.
    expect((await admin.post(`/api/team/users/${member.id}/role`, { data: { role: "Viewer" } })).status()).toBe(200);
    expect((await analyst.post("/api/suppliers", { data: { name: `Blocked ${suffix}` } })).status()).toBe(403);
    expect((await (await analyst.get("/api/auth/me")).json()).role).toBe("Viewer");
    expect((await admin.post(`/api/team/users/${member.id}/role`, { data: { role: "Disabled" } })).status()).toBe(200);
    expect((await analyst.get("/api/suppliers")).status()).toBe(401);
    expect((await analyst.post("/api/auth/login", { data: { email: analystEmail, password } })).status()).toBe(401);
    expect((await admin.post(`/api/team/users/${member.id}/role`, { data: { role: "Analyst" } })).status()).toBe(200);
    await login(analyst, analystEmail);
    // Also prove the team creation flow through the administrator's screen.
    await page.context().addCookies((await admin.storageState()).cookies);
    await page.goto("/team");
    await page.getByLabel("Nombre completo").fill("Cuenta desde pantalla");
    await page.getByLabel("Correo del integrante").fill(`ui-${suffix}@example.com`);
    await page.getByLabel("Contraseña inicial").fill(password);
    await page.getByRole("button", { name: "Crear cuenta", exact: true }).click();
    await expect(page.getByText("Cuenta desde pantalla", { exact: true })).toBeVisible();
  } finally { await admin.dispose(); await analyst.dispose(); await viewer.dispose(); }
});

test("changing a password invalidates every previous session", async ({ page }) => {
  const admin = await request.newContext({ baseURL });
  const first = await request.newContext({ baseURL });
  const second = await request.newContext({ baseURL });
  const email = `password-${randomUUID()}@example.com`;
  const nextPassword = "DifferentTestPassword2026!";
  try {
    await login(admin, "e2e@precios.local");
    expect((await admin.post("/api/team/users", { data: { fullName: "Cambio contraseña", email, password, role: "Viewer" } })).status()).toBe(201);
    await login(first, email); await login(second, email);
    expect((await first.post("/api/auth/password", { data: { currentPassword: "wrong", newPassword: nextPassword } })).status()).toBe(400);
    expect((await first.post("/api/auth/password", { data: { currentPassword: password, newPassword: password } })).status()).toBe(400);
    await page.context().addCookies((await first.storageState()).cookies);
    await page.goto("/account");
    await page.getByLabel("Contraseña actual", { exact: true }).fill(password);
    await page.getByLabel("Nueva contraseña", { exact: true }).fill(nextPassword);
    await page.getByLabel("Confirmar nueva contraseña", { exact: true }).fill(nextPassword);
    await page.getByRole("button", { name: "Guardar contraseña", exact: true }).click();
    await expect(page).toHaveURL(/\/login$/);
    expect((await first.get("/api/auth/me")).status()).toBe(401);
    expect((await second.get("/api/auth/me")).status()).toBe(401);
    expect((await first.post("/api/auth/login", { data: { email, password } })).status()).toBe(401);
    await login(first, email, nextPassword);
  } finally { await admin.dispose(); await first.dispose(); await second.dispose(); }
});
