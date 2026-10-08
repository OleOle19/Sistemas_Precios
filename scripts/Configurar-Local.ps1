$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Security
[System.Windows.Forms.Application]::EnableVisualStyles()
$taskRoot = Split-Path -Parent $PSScriptRoot
$secretPath = Join-Path $taskRoot 'services/api/storage/local-secrets.json'
$previous = @{}
if (Test-Path -LiteralPath $secretPath) {
    $loaded = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
    foreach ($property in $loaded.PSObject.Properties) { $previous[$property.Name] = $property.Value }
}
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Configurar Sistema de Precios - Gemini / OpenAI'
$form.Size = New-Object System.Drawing.Size(580,540)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.TopMost = $true
$form.Font = New-Object System.Drawing.Font('Segoe UI',10)
function Add-Field([string]$title, [int]$y, [bool]$hidden) {
    $label = New-Object System.Windows.Forms.Label
    $label.Text = $title; $label.Location = New-Object System.Drawing.Point(24,$y); $label.Size = New-Object System.Drawing.Size(515,24)
    $form.Controls.Add($label)
    $inputBox = New-Object System.Windows.Forms.TextBox
    $inputBox.Location = New-Object System.Drawing.Point(24,($y+27)); $inputBox.Size = New-Object System.Drawing.Size(515,28)
    $inputBox.UseSystemPasswordChar = $hidden; $form.Controls.Add($inputBox)
    return $inputBox
}
$email = Add-Field 'Correo de tu cuenta local' 18 $false
$email.Text = 'admin@precios.local'
if ($previous.Email) { $email.Text = $previous.Email; $email.ReadOnly = $true }
$password = Add-Field 'Contraseña (vacío conserva la anterior; no la restablece)' 88 $true
if ($previous.Password) { $password.Enabled = $false }
$providerLabel = New-Object System.Windows.Forms.Label
$providerLabel.Text = 'Servicio para leer fotos y PDF'; $providerLabel.Location = New-Object System.Drawing.Point(24,162); $providerLabel.Size = New-Object System.Drawing.Size(515,24)
$form.Controls.Add($providerLabel)
$provider = New-Object System.Windows.Forms.ComboBox
$provider.DropDownStyle = 'DropDownList'; $provider.Location = New-Object System.Drawing.Point(24,189); $provider.Size = New-Object System.Drawing.Size(515,28)
[void]$provider.Items.Add('Gemini'); [void]$provider.Items.Add('OpenAI')
$provider.SelectedIndex = 0
if ($previous.ExtractionProvider -eq 'OpenAI') { $provider.SelectedIndex = 1 }
$form.Controls.Add($provider)
$key = Add-Field 'Clave del servicio elegido (vacío conserva su clave anterior)' 232 $true
$note = New-Object System.Windows.Forms.Label
$note.Text = 'Gemini: crea una clave en Google AI Studio, en un proyecto gratuito. Usa documentos ficticios: Google puede usar su contenido para mejorar productos. Las claves se cifran en este equipo y no se suben a GitHub.'
$note.Location = New-Object System.Drawing.Point(24,310); $note.Size = New-Object System.Drawing.Size(515,80)
$form.Controls.Add($note)
$provider.Add_SelectedIndexChanged({ $key.Clear() })
$save = New-Object System.Windows.Forms.Button
$save.Text = 'Guardar configuración'; $save.Location = New-Object System.Drawing.Point(294,422); $save.Size = New-Object System.Drawing.Size(245,40)
$form.Controls.Add($save)
$save.Add_Click({
    try {
        if ($email.Text -notmatch '^[^\s@]+@[^\s@]+\.[^\s@]+$' -or ($password.Text -and $password.Text.Length -lt 12) -or (-not $password.Text -and -not $previous.Password)) {
            [System.Windows.Forms.MessageBox]::Show('Escribe un correo válido y, en una configuración nueva, una contraseña de al menos 12 caracteres.') | Out-Null; return
        }
        $selected = [string]$provider.SelectedItem
        $keyValue = $key.Text.Trim()
        if ($keyValue -and ($keyValue -match '\s' -or $keyValue.Length -lt 16 -or ($selected -eq 'Gemini' -and $keyValue -match '^sk-'))) {
            [System.Windows.Forms.MessageBox]::Show('Revisa que la clave corresponda al servicio elegido y esté completa. No pegues claves en el chat.') | Out-Null; return
        }
        function Protect-Value([string]$value) {
            $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
            return [Convert]::ToBase64String([System.Security.Cryptography.ProtectedData]::Protect($bytes,$null,[System.Security.Cryptography.DataProtectionScope]::CurrentUser))
        }
        $keyField = 'GeminiApiKey'
        if ($selected -eq 'OpenAI') { $keyField = 'ApiKey' }
        if ($keyValue) { $previous[$keyField] = Protect-Value $keyValue }
        if (-not $previous.Password -and $password.Text) { $previous.Password = Protect-Value $password.Text }
        $previous.Email = $email.Text.Trim().ToLower()
        $previous.ExtractionProvider = $selected
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $secretPath) | Out-Null
        $previous | ConvertTo-Json | Set-Content -LiteralPath $secretPath -Encoding UTF8
        $key.Clear(); $password.Clear()
        [System.Windows.Forms.MessageBox]::Show('Configuración guardada. Reinicia el sistema para aplicar el servicio y la clave elegidos.') | Out-Null
        $form.Close()
    } catch {
        [System.Windows.Forms.MessageBox]::Show('No se pudo guardar la configuración. Comprueba los permisos de la carpeta local.') | Out-Null
    }
})
$form.Add_Shown({ $form.Activate() })
[void]$form.ShowDialog()
$form.Dispose()
