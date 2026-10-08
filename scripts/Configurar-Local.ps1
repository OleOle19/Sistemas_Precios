$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Security
[System.Windows.Forms.Application]::EnableVisualStyles()
$taskRoot = Split-Path -Parent $PSScriptRoot
$secretPath = Join-Path $taskRoot 'services/api/storage/local-secrets.json'
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Configurar Sistema de Precios'
$form.Size = New-Object System.Drawing.Size(560,440)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.Font = New-Object System.Drawing.Font('Segoe UI',10)
function Add-Field([string]$title, [int]$y, [bool]$hidden) {
    $label = New-Object System.Windows.Forms.Label
    $label.Text = $title; $label.Location = New-Object System.Drawing.Point(24,$y); $label.Size = New-Object System.Drawing.Size(495,24)
    $form.Controls.Add($label)
    $inputBox = New-Object System.Windows.Forms.TextBox
    $inputBox.Location = New-Object System.Drawing.Point(24,($y+27)); $inputBox.Size = New-Object System.Drawing.Size(495,28)
    $inputBox.UseSystemPasswordChar = $hidden; $form.Controls.Add($inputBox)
    return $inputBox
}
$email = Add-Field 'Correo para tu cuenta local' 20 $false
$email.Text = 'admin@precios.local'
$password = Add-Field 'Contraseña local (mínimo 12 caracteres)' 94 $true
$key = Add-Field 'Clave nueva de OpenAI (opcional para configurar después)' 168 $true
$note = New-Object System.Windows.Forms.Label
$note.Text = 'Si compartiste una clave en el chat, revócala y usa una nueva. Los valores se cifran para tu cuenta de Windows y no se suben a GitHub. En una base existente la contraseña se conserva; este formulario no la cambia.'
$note.Location = New-Object System.Drawing.Point(24,238); $note.Size = New-Object System.Drawing.Size(495,78)
$form.Controls.Add($note)
$save = New-Object System.Windows.Forms.Button
$save.Text = 'Guardar configuración'; $save.Location = New-Object System.Drawing.Point(280,332); $save.Size = New-Object System.Drawing.Size(240,40)
$form.Controls.Add($save)
$save.Add_Click({
    if ($password.Text.Length -lt 12 -or $email.Text -notmatch '^[^\s@]+@[^\s@]+\.[^\s@]+$') {
        [System.Windows.Forms.MessageBox]::Show('Escribe un correo válido y una contraseña de al menos 12 caracteres.') | Out-Null; return
    }
    if ($key.Text -and $key.Text -notmatch '^sk-') {
        [System.Windows.Forms.MessageBox]::Show('La clave no tiene el formato esperado. Puedes dejarla vacía por ahora.') | Out-Null; return
    }
    function Protect-Value([string]$value) {
        if (-not $value) { return '' }
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($value)
        return [Convert]::ToBase64String([System.Security.Cryptography.ProtectedData]::Protect($bytes,$null,[System.Security.Cryptography.DataProtectionScope]::CurrentUser))
    }
    $storedKey = Protect-Value $key.Text
    if (-not $storedKey -and (Test-Path -LiteralPath $secretPath)) {
        $previous = Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
        $storedKey = $previous.ApiKey
    }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $secretPath) | Out-Null
    @{Email=$email.Text.Trim().ToLower();Password=(Protect-Value $password.Text);ApiKey=$storedKey} | ConvertTo-Json | Set-Content -LiteralPath $secretPath -Encoding UTF8
    $key.Clear(); $password.Clear()
    [System.Windows.Forms.MessageBox]::Show('Configuración guardada. Reinicia el servicio si ya estaba abierto.') | Out-Null
    $form.Close()
})
[void]$form.ShowDialog()
$form.Dispose()
