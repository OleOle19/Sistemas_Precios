# Sistema de precios comparativos mediante fotos e IA
Aplicación para convertir fotos de etiquetas y cotizaciones de proveedores en precios revisados y comparables. Nació como propuesta de un familiar que trabaja en sistemas: **“Sistemas de precios comparativos mediante fotos usando IA”**.
## Qué permite hacer
1. Iniciar sesión en el negocio con una cuenta propia y registrar proveedores o tiendas según el rol asignado.
2. Subir JPG, PNG, WebP o PDF de hasta 10 MB, indicar tipo de fuente y fecha del precio.
3. Leer el archivo con Gemini u OpenAI y obtener producto, contenido, unidad, precio y moneda. Los datos ilegibles quedan vacíos; los fallos no generan precios de ejemplo.
4. Contrastar el resultado con el original, corregir cada fila y descartar totales o filas ajenas. Todas las extracciones requieren aprobación humana.
5. Comparar el último precio observado por proveedor para productos equivalentes, dentro de la misma moneda y unidad base.
6. Consultar historial y variaciones sin mezclar soles, dólares o euros.
**Ejemplo:** una bolsa de 5 KG a PEN 22.50 se compara como PEN 4.50/KG. Una de 1,000 G a PEN 6.00 equivale a PEN 6.00/KG. La identidad del producto debe coincidir en marca, variante y calidad; la aplicación no decide automáticamente que dos productos son equivalentes. Impuestos, promociones y condiciones comerciales deben revisarse en el documento antes de aprobar.
## Ejecutar
Requisitos: .NET SDK 9, Node.js 22 o posterior y una clave de Gemini con cuota disponible o de OpenAI con saldo para la lectura real. SQL Server es opcional: SQLite permite empezar sin instalar otro motor.
Desde la raíz del repositorio:
```powershell
npm ci
powershell -NoProfile -STA -ExecutionPolicy Bypass -File scripts/Configurar-Local.ps1
dotnet run --project services/api
```
En otra terminal:
```powershell
npm run dev:web
```
Abre http://localhost:3000 y usa el correo y la contraseña que elegiste. La API escucha en http://localhost:8080.
El formulario de Windows guarda clave y contraseña cifradas mediante DPAPI en `services/api/storage/local-secrets.json`, excluido de Git. Solo la misma cuenta de Windows puede descifrarlas. Elige Gemini (predeterminado) u OpenAI en el formulario. También se aceptan `GEMINI_API_KEY`, `OPENAI_API_KEY` y variables de configuración; consulta [arranque y configuración](docs/local-run.md). No hay contraseñas públicas predefinidas ni proveedores ficticios.
## Tecnología y estructura
- `apps/web`: Next.js 15, React 19 y TypeScript. Sesión real, formularios editables y errores visibles; sin respaldo de datos ficticios.
- `services/api`: ASP.NET Core 9, Entity Framework Core, autenticación por cookie y extracción configurable con Gemini GenerateContent u OpenAI Responses, con salida JSON estructurada.
- SQLite por defecto; soporte de SQL Server y PostgreSQL configurable.
- Cola persistente en la base de datos: los documentos en estado `Uploaded` se procesan por un trabajador de la API. Las llamadas interrumpidas no se repiten automáticamente para evitar consumos inesperados.
- `services/api.Tests`: pruebas de aprobación transaccional, unidades, monedas, duplicados, fallos y contratos HTTP de Gemini y OpenAI.
- `services/vision-rs` y `contracts/vision.proto`: experimento del prototipo anterior con OCR simulado; están conservados como antecedente y no intervienen en el flujo actual.
## Validación
```powershell
dotnet test SistemasPrecios.sln
npm run check:web
npm run build:web
npm exec --workspace web -- playwright install chromium
npm run test:web:e2e
```
Para ejecutar las pruebas relacionales en una instancia local de SQL Server:
```powershell
$env:PRECIOS_TEST_SQLSERVER='Server=localhost;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10'
dotnet test SistemasPrecios.sln
```
Cada prueba crea una base temporal `PreciosTests_<GUID>` y elimina exclusivamente esa base al finalizar. Las pruebas del navegador usan una base SQLite nueva y **deshabilitan las claves de Gemini y OpenAI**, por lo que no consumen saldo. Los contratos de Gemini y OpenAI se prueban con transporte controlado; validar reconocimiento real requiere ejecutar la lectura con una clave vigente y fotos representativas.
## Alcance actual
Versión funcional para **un negocio por instalación, con varias cuentas** y una única instancia de API. El administrador agrega integrantes desde **Equipo**: pueden usar correos de cualquier dominio, y todos comparten proveedores, documentos, precios e historial de esa instalación. No se agrupan cuentas por dominio del correo ni hay registro público. Una persona que trabaja sola usa su cuenta administradora; otro negocio necesita una instalación y base separadas.

| Rol | Permisos |
| --- | --- |
| Administrador | Consultar, registrar proveedores, subir y revisar documentos, crear cuentas y cambiar o deshabilitar el acceso de otros integrantes. |
| Analista | Consultar, registrar proveedores, subir, corregir y aprobar documentos. |
| Consulta | Ver proveedores, archivos, comparaciones e historial. |

Cada cuenta puede cambiar su contraseña desde **Mi cuenta**, verificando la actual; esto cierra todas sus sesiones previas. Deshabilitar una cuenta impide su acceso y elimina el uso de sus sesiones abiertas. Los cambios de rol se aplican a las solicitudes siguientes. El administrador no puede quitarse su propio acceso desde Equipo. Las páginas privadas redirigen al ingreso sin sesión, y la API verifica sesión y permisos incluso ante solicitudes directas.

No incluye despliegue público, gestión de múltiples organizaciones en una aplicación, recuperación de contraseña por correo, conversión de monedas, integración con sistemas de compras ni verificación automática de impuestos. Una cuenta administradora se crea al iniciar una base nueva; el formulario de configuración no cambia contraseñas de cuentas existentes.
El esquema v2 se crea en una base nueva mediante `EnsureCreated`; no se migra automáticamente una base del prototipo. SQLite usa `storage/precios-v2.db` para conservar el archivo anterior. Antes de futuras actualizaciones de esquema o uso con datos empresariales, se necesita una estrategia de migraciones versionadas y copias de seguridad. [Arquitectura y decisiones](docs/architecture.md).

## Materiales de demostración
[Datos ficticios y resultados esperados](docs/datos-prueba/README.md): dos cotizaciones PDF y una etiqueta para probar unidades e historial. Los archivos son ejemplos; no se insertan automáticamente en la base. Leerlos requiere una clave de API con cuota disponible.

[Presentación breve de proyectos en PDF](docs/presentacion-proyectos.pdf).

## Gemini para la demo
El modelo predeterminado es `gemini-3.1-flash-lite`. Google documenta un nivel gratuito sujeto a límites por proyecto; activar facturación cambia las condiciones de uso. El proyecto no puede verificar que tu cuenta esté en el nivel gratuito. Selecciona un proyecto gratuito en Google AI Studio y usa los documentos ficticios. No hay cambio automático de proveedor ni reintentos de solicitudes externas.

[Precios y condiciones de Gemini](https://ai.google.dev/gemini-api/docs/pricing) · [Crear una clave](https://ai.google.dev/gemini-api/docs/api-key).
