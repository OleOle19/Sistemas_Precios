# Validación local - 8 de octubre de 2026

- 40 pruebas del servidor correctas con SQLite y 40 con SQL Server local, incluidas protección del administrador y cambio de contraseña sin alterar otras cuentas.
- Comprobación TypeScript y compilación web de producción correctas.
- Cuatro pruebas de navegador con API real: acceso anónimo/cookie falsa, negocio compartido y permisos por rol, invalidación de sesiones por cambio de contraseña, y carga de archivo sin clave sin precios ficticios.
- Se comprobó que correos de dominios diferentes comparten proveedores; Consulta no puede escribir y Analista no puede administrar cuentas. Cambiar el rol o deshabilitar una cuenta limita sus sesiones ya abiertas. La interfaz permite agregar integrantes y cambiar la contraseña propia.
- El navegador deshabilita ambas claves y usa una base SQLite independiente; no consume cuota externa ni modifica las cuentas locales del usuario.
- Gemini gemini-3.1-flash-lite leyó realmente la etiqueta PNG de prueba: 500 G a PEN 16.
- Gemini leyó realmente la cotización Norte en PDF: 5 KG a PEN 125, 10 M a PEN 180 y 5 L a PEN 45. No confundió cantidad solicitada, importe de línea ni total con el precio de una presentación.
- Las dos lecturas reales se ejecutaron directamente con el adaptador, sin modificar la base del usuario. No se insertan los ejemplos automáticamente.

Los documentos son ficticios y claros. Dos resultados correctos no constituyen una evaluación general de precisión con fotos borrosas, promociones o diseños variados; siempre se requiere revisión humana.

El sistema no cambia automáticamente a otro proveedor, no activa facturación y no reintenta solicitudes externas. OpenAI permanece disponible; su prueba anterior respondió 429. PostgreSQL/Docker no se ejecutaron en esta validación.
