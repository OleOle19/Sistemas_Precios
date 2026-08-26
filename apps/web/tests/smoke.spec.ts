import { test, expect } from "@playwright/test";

test("dashboard renders hero text", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByText("Compara proveedores a partir de fotos y listas comerciales.")).toBeVisible();
});
