using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SistemasPrecios.Api.Services;

public static class LocalSecrets
{
    public static void Load(ConfigurationManager config, string contentRoot)
    {
        if (!OperatingSystem.IsWindows() || config["LocalSecrets:Enabled"] == "false") return;
        var path = Path.Combine(contentRoot, "storage", "local-secrets.json");
        if (!File.Exists(path)) return;
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var root = json.RootElement;
        foreach (var (field, setting) in new[] { ("ApiKey", "OpenAI:ApiKey"), ("GeminiApiKey", "Gemini:ApiKey"), ("Password", "Bootstrap:Password") })
        {
            if (!root.TryGetProperty(field, out var encrypted) || string.IsNullOrWhiteSpace(encrypted.GetString())) continue;
            if (!string.IsNullOrWhiteSpace(config[setting])) continue;
            try
            {
                config[setting] = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encrypted.GetString()!), null, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException) { throw new InvalidOperationException("La configuración cifrada pertenece a otra cuenta de Windows. Ejecuta nuevamente Configurar-Local.ps1."); }
        }
        if (root.TryGetProperty("Email", out var email) && !string.IsNullOrWhiteSpace(email.GetString())) config["Bootstrap:Email"] = email.GetString();
        if (root.TryGetProperty("WorkspaceName", out var workspace) && !string.IsNullOrWhiteSpace(workspace.GetString()) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Workspace__Name"))) config["Workspace:Name"] = workspace.GetString();
        if (root.TryGetProperty("ExtractionProvider", out var provider) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Extraction__Provider"))) config["Extraction:Provider"] = provider.GetString();
    }
}
