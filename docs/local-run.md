# Arranque local
## Windows: configuración con campos ocultos
1. Ejecuta `npm ci` en la raíz.
2. Ejecuta `powershell -NoProfile -STA -ExecutionPolicy Bypass -File scripts/Configurar-Local.ps1`.
3. Elige correo y contraseña de al menos 12 caracteres. Ingresa una clave nueva de OpenAI o déjala vacía para configurarla después. Nunca la pegues en el chat, un README o un archivo versionado. Si quedó expuesta, revócala primero en el panel de OpenAI.
4. Ejecuta `dotnet run --project services/api`.
5. En otra terminal ejecuta `npm run dev:web`.
6. Abre http://localhost:3000.
El formulario cifra valores con DPAPI para la cuenta actual de Windows. El archivo `services/api/storage/local-secrets.json` está ignorado por Git. Reinicia la API cuando cambies la clave. Si vuelves a configurar, una clave vacía conserva la anterior; la contraseña solo se usa para crear la cuenta en una base nueva. Guarda tu contraseña: el formulario no restablece una cuenta existente.
La API resuelve almacenamiento y SQLite respecto de `services/api`, independientemente de la carpeta desde la cual la ejecutes.
## Configuración por variables
En otros sistemas, configura `OPENAI_API_KEY` (solo en el servidor), `Bootstrap__Email`, `Bootstrap__Password` y, opcionalmente, `OpenAI__Model`. También se admiten secretos de usuario de .NET en desarrollo. No incluyas secretos en comandos que queden en el historial de una terminal compartida. Usa el administrador de secretos del entorno.
Modelo predeterminado: `gpt-5.4-mini`. Se envían las imágenes o PDFs a OpenAI Responses con `store=false` y formato JSON estructurado. Esto no elimina todas las posibles políticas de retención del proveedor; revisa las condiciones de tu cuenta antes de usar documentos empresariales sensibles. Una lectura o relectura consume saldo de API. La aplicación no sustituye una clave por datos de prueba.
## SQL Server local
En la terminal donde iniciarás la API:
```powershell
$env:Database__Provider='SqlServer'
$env:ConnectionStrings__DefaultConnection='Server=localhost;Database=SistemasPreciosV2;Integrated Security=True;TrustServerCertificate=True'
dotnet run --project services/api
```
Necesitas permiso para crear la base nueva. `TrustServerCertificate=True` se usa aquí para el certificado de desarrollo local; una instalación remota debe usar certificado y conexión adecuados. Con una instancia Express, sustituye `localhost` por el nombre real de tu instancia.
Puedes ver tablas y consultar el historial desde SSMS. El proyecto utiliza Entity Framework y consultas relacionales; no afirma implementar procedimientos almacenados ni triggers propios.
## PostgreSQL / Docker
```powershell
# Configura las variables del archivo infra/.env.example en un infra/.env privado.
docker compose --env-file infra/.env -f infra/docker-compose.yml up --build
```
Usa un volumen/base nuevos para v2. No borres el volumen del prototipo para solucionar incompatibilidades. El archivo cifrado de Windows no funciona en un contenedor Linux: entrega las variables mediante el entorno privado del contenedor. Solo una instancia de API procesa la cola.
## Uso y comprobación de lectura real
1. Registra dos proveedores o tiendas.
2. Sube una foto clara de una etiqueta y una cotización en PDF o foto, cada una con su proveedor y fecha.
3. Comprueba contra el original que precio, moneda y contenido sean correctos. Si una cotización muestra cantidad pedida, precio unitario y total, el precio a aprobar debe corresponder a una presentación, y contenido a lo que contiene esa presentación.
4. Identifica el mismo producto solo si marca, variante y calidad son equivalentes. Descarta promociones condicionadas, totales e información ajena.
5. Aprueba y confirma el resultado en Comparaciones e Historial. Para una sola fuente se indica “sin comparación”.
6. Un documento aprobado conserva sus precios y no permite otra aprobación ni relectura. Para una observación nueva, sube un archivo nuevo con su fecha.
PEN, USD y EUR se comparan por separado. G se convierten a KG y ML a L; no se convierten KG a L ni moneda a moneda. El precio normalizado se conserva con seis decimales y se muestra hasta cuatro. La diferencia relativa es `(máximo − mínimo) / promedio × 100`.
## Si falla
- **No inicia la API:** configura primero una contraseña local o revisa la conexión al motor elegido.
- **Clave sin configurar:** ingrésala en el formulario y reinicia la API; el botón de carga se habilitará al recargar Documentos.
- **401 de OpenAI:** verifica o sustituye la clave.
- **429 de OpenAI:** revisa cuota/saldo; reintenta manualmente cuando esté resuelto.
- **Lectura incompleta:** divide el archivo o usa una foto más clara. No se aprueban fragmentos de una respuesta incompleta.
- **Lectura interrumpida por reinicio:** queda fallida; el archivo en disco permanece y puedes reintentar. Una llamada interrumpida podría haber consumido saldo.
- **Base del prototipo:** el sistema la rechaza; conserva una copia y selecciona una base nueva. No hay migración automática.
## Copia de seguridad local
Detén la API y guarda juntos la base SQLite y los archivos de `services/api/storage`. No compartas el archivo cifrado de configuración. En SQL Server/PostgreSQL utiliza las herramientas de copia del motor y conserva también los documentos. Aún no hay pantalla de recuperación de contraseña o gestión de usuarios.
