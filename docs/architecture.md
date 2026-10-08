# Arquitectura v2
```text
Navegador → Next.js (proxy del mismo origen) → ASP.NET Core → base relacional
                                                       → archivos originales
Documento Uploaded → trabajador único → Gemini / OpenAI → NeedsReview
Revisión humana → transacción y control de concurrencia → Approved + PriceSnapshots
PriceSnapshots → comparación por producto / moneda / unidad / último proveedor
```
## Extracción
El proveedor seleccionado (Gemini u OpenAI) recibe los bytes del archivo real. `IPriceExtractor` permite cambiar de proveedor sin cambiar las reglas de aprobación o comparación; ambos comparten instrucciones y contrato JSON. La selección es explícita y no hay sustitución automática por otro proveedor. El esquema JSON estricto pide evidencia, producto, contenido, unidad, precio de una presentación y moneda. Las instrucciones dentro del documento se tratan como datos no confiables. Una respuesta rechazada, incompleta o inválida produce un error visible; no hay OCR simulado ni texto lateral como respaldo en producción. Los valores ausentes quedan vacíos. Los candidatos de producto usan coincidencia de nombres/alias como sugerencia, sin aprobación automática ni una falsa probabilidad de exactitud de la IA.
## Aprobación
Se requieren todas las filas exactamente una vez. Se pueden excluir filas ajenas. Se valida precio/contenido positivos, moneda admitida y unidad compatible con el producto. La transacción guarda producto, alias, revisión y observación conjuntamente. Una revisión repetida no duplica precios; el campo `Revision` del documento controla aprobaciones concurrentes. Un documento aprobado es inmutable en el flujo actual. Una observación nueva requiere un archivo nuevo.
## Comparación
El precio de la presentación se divide por su contenido normalizado. Las agrupaciones mantienen producto, moneda y unidad; el último precio de cada proveedor se elige por fecha observada y después por fecha registrada. No se convierten monedas ni unidades de dimensiones distintas. No se decide equivalencia entre marcas/variantes. La aplicación conserva documentos e historial, pero impuestos y condiciones de compra deben verificarse manualmente.
Las comparaciones se calculan desde observaciones aprobadas al consultar; no dependen de una tabla de resultados que pueda quedar desactualizada. Para gran volumen se necesitarán consultas/materialización y paginación adicionales.
## Sesión y configuración
Cookie HttpOnly con SameSite Lax. Next.js reenvía la sesión en consultas del servidor y actúa como proxy de origen único para formularios. Las solicitudes POST con Origin no autorizado se rechazan. Las páginas no muestran datos ficticios si el servicio falla. La configuración local de Windows usa DPAPI; variables del servidor y secretos de usuario son alternativas.

Una instalación y base representan un solo negocio. Los usuarios son integrantes agregados explícitamente por un administrador; el correo sirve para iniciar sesión, no para determinar a qué negocio pertenecen. No hay filtrado por usuario para datos comerciales: se comparten dentro de esa base. `Workspace:Name` es el nombre mostrado, no un identificador de aislamiento entre organizaciones.

Las páginas privadas comparten un grupo de rutas con sesión validada en servidor; el middleware solo adelanta la redirección si no hay cookie. Cada endpoint vuelve a exigir autenticación; las políticas `EditPrices` (Admin/Analyst) y `ManageTeam` (Admin) protegen escrituras y gestión de cuentas. Viewer solo consulta. Los controles de interfaz acompañan estas políticas, pero no sustituyen la verificación de la API.

En cada petición autenticada se consulta el usuario vigente. Deshabilitar una cuenta o cambiar su contraseña invalida sus cookies anteriores; los permisos se actualizan desde la base. Las cookies caducan a las ocho horas. El cambio de contraseña requiere la anterior. Los hashes no aparecen en respuestas de equipo. Se conserva el esquema actual: `Disabled=0`, `Admin=1`, `Analyst=2`, `Viewer=3` se guardan en la columna de rol ya existente. No se eliminan cuentas ni datos de negocio al retirar accesos. El cambio de rol comprueba al administrador dentro de una transacción serializable y rechaza modificar la cuenta propia.

Los roles expresan permisos, no cargos ni jerarquías laborales. Admin y Analyst pueden aprobar su propia carga; no hay una regla de doble aprobación ni una auditoría individual completa. Para una instalación pública se necesitan controles adicionales de operación, recuperación de acceso, límites de intentos y una estrategia de migraciones y copias.
## Persistencia y ejecución
SQLite local, SQL Server o PostgreSQL mediante EF Core. Esta versión crea un esquema nuevo, no migra la base del prototipo. Los documentos `Uploaded` actúan como cola persistente. Un único trabajador procesa secuencialmente; al reiniciar se marcan fallidas las llamadas previamente en proceso para no repetir consumos de API. Se requiere una sola instancia de API: para escalar hay que introducir reclamos distribuidos, reintentos acotados y migraciones versionadas.
Los contratos de Gemini y OpenAI se prueban con transporte controlado y las reglas de negocio con bases relacionales reales. La prueba del navegador inicia una API real con SQLite y sin claves de Gemini ni OpenAI. El reconocimiento real necesita fotos representativas y una clave vigente; una compilación o prueba simulada no demuestra calidad de lectura.
El servicio Rust/gRPC se conserva únicamente como experimento del prototipo. No es dependencia de ejecución.
