# Arranque local
## Windows: configuración con campos ocultos
1. Ejecuta `npm ci` en la raíz.
2. Ejecuta `powershell -NoProfile -STA -ExecutionPolicy Bypass -File scripts/Configurar-Local.ps1`.
3. Escribe el nombre del negocio. En una configuración nueva, elige correo y contraseña de al menos 12 caracteres. Selecciona Gemini (predeterminado) u OpenAI e ingresa la clave del servicio elegido, o déjala vacía para configurarla después. Nunca la pegues en el chat, un README o un archivo versionado. Revoca cualquier clave expuesta en el panel de su proveedor.
4. Ejecuta `dotnet run --project services/api`.
5. En otra terminal ejecuta `npm run dev:web`.
6. Abre http://localhost:3000.
El formulario cifra valores con DPAPI para la cuenta actual de Windows. El archivo `services/api/storage/local-secrets.json` está ignorado por Git. Reinicia la API cuando cambies la clave. Si vuelves a configurar, una clave vacía conserva la clave anterior del servicio seleccionado. Una contraseña vacía conserva la configuración previa; la contraseña solo se usa para crear la cuenta en una base nueva. Guarda tu contraseña: el formulario no restablece una cuenta existente.
La API resuelve almacenamiento y SQLite respecto de `services/api`, independientemente de la carpeta desde la cual la ejecutes.

## Integrantes y permisos
El administrador entra en **Equipo** para crear cuentas con nombre, correo, contraseña inicial y rol. Las cuentas de la misma instalación comparten el negocio aunque sus correos tengan dominios diferentes. Analista puede cargar y aprobar documentos; Consulta solo lee. El administrador puede modificar o deshabilitar el acceso de otros integrantes, pero no el suyo. Las personas cambian su contraseña en **Mi cuenta**, conociendo la actual; se cierran sus sesiones anteriores. No hay recuperación por correo en esta versión.

El nombre del negocio se puede cambiar en el formulario y aplicar al reiniciar, o mediante `Workspace__Name`. No crea otro negocio ni otra base. Para negocios diferentes se requieren instalaciones y bases separadas.
## Configuración por variables
En otros sistemas, configura `Extraction__Provider=Gemini` y `GEMINI_API_KEY`, o `Extraction__Provider=OpenAI` y `OPENAI_API_KEY` (solo en el servidor), además de `Bootstrap__Email` y `Bootstrap__Password`. Los modelos se configuran con `Gemini__Model` u `OpenAI__Model`. También se admiten secretos de usuario de .NET en desarrollo. No incluyas secretos en comandos que queden en el historial de una terminal compartida. Usa el administrador de secretos del entorno.
Gemini usa `gemini-3.1-flash-lite` por defecto; OpenAI usa `gpt-5.4-mini`. Se envían los bytes reales del archivo al proveedor seleccionado con un esquema JSON común y revisión humana obligatoria. OpenAI usa Responses con `store=false`, lo cual no elimina todas sus posibles políticas de retención. Cada lectura utiliza cuota o saldo según el proyecto; el sistema no cambia de proveedor automáticamente. La aplicación no sustituye una clave por datos de prueba.
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
- **429 de Gemini:** revisa límites y cuota en Google AI Studio. Espera al restablecimiento y reintenta manualmente; no necesitas activar facturación para utilizar una cuota gratuita disponible.
- **401/403 de Gemini:** verifica la clave y los permisos del proyecto.
- **404 de Gemini:** comprueba el modelo configurado y su disponibilidad.
- **503 de Gemini:** el servicio está temporalmente ocupado o no disponible; reintenta más tarde.
- **429 de OpenAI:** revisa cuota/saldo; reintenta manualmente cuando esté resuelto.
- **Lectura incompleta:** divide el archivo o usa una foto más clara. No se aprueban fragmentos de una respuesta incompleta.
- **Lectura interrumpida por reinicio:** queda fallida; el archivo en disco permanece y puedes reintentar. Una llamada interrumpida podría haber consumido saldo.
- **Base del prototipo:** el sistema la rechaza; conserva una copia y selecciona una base nueva. No hay migración automática.
## Copia de seguridad local
Detén la API y guarda juntos la base SQLite y los archivos de `services/api/storage`. No compartas el archivo cifrado de configuración. En SQL Server/PostgreSQL utiliza las herramientas de copia del motor y conserva también los documentos. Aún no hay pantalla de recuperación de contraseña o gestión de usuarios.

## Crear una clave de Gemini gratuita
Abre [Google AI Studio](https://aistudio.google.com/), crea una clave en un proyecto del nivel gratuito y guárdala en el formulario local con Gemini seleccionado. No hace falta configurar la clave de OpenAI. En el nivel gratuito, Google puede usar el contenido para mejorar sus productos; usa únicamente los documentos ficticios de `docs/datos-prueba` para la demo. Los límites y condiciones dependen del proyecto y del modelo: [precios oficiales](https://ai.google.dev/gemini-api/docs/pricing).
