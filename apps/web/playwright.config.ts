import { defineConfig } from "@playwright/test";
import { randomUUID } from "node:crypto";
export default defineConfig({
  testDir: "./tests",
  workers: 1,
  timeout: 60000,
  use: { baseURL: "http://localhost:3000", trace: "retain-on-failure" },
  webServer: [
    {
      command: "dotnet run --project ../../services/api --no-launch-profile",
      url: "http://localhost:8080/healthz",
      timeout: 120000,
      reuseExistingServer: false,
      env: {
        ASPNETCORE_URLS: "http://localhost:8080",
        ASPNETCORE_ENVIRONMENT: "Development",
        Database__Provider: "Sqlite",
        ConnectionStrings__DefaultConnection: `Data Source=storage/e2e-${randomUUID()}.db`,
        Bootstrap__Password: "TestOnlyPassword2026!",
        Bootstrap__Email: "e2e@precios.local",
        LocalSecrets__Enabled: "false",
        OPENAI_API_KEY: "",
        OpenAI__ApiKey: "",
        GEMINI_API_KEY: "",
        Gemini__ApiKey: "",
        Extraction__Provider: "Gemini",
      },
    },
    {
      command: "npm run dev",
      url: "http://localhost:3000/login",
      timeout: 120000,
      reuseExistingServer: false,
      env: { API_BASE_URL: "http://localhost:8080" },
    },
  ],
});
