# Validación local - 8 de octubre de 2026

- 38 pruebas del servidor correctas con SQLite y 38 con SQL Server local.
- Comprobación TypeScript, compilación web de producción y prueba de navegador con API real correctas.
- La prueba del navegador deshabilita ambas claves y comprueba sesión, proveedor, archivo, error sin clave sin precios ficticios y cierre de sesión.
- Gemini gemini-3.1-flash-lite leyó realmente la etiqueta PNG de prueba: 500 G a PEN 16.
- Gemini leyó realmente la cotización Norte en PDF: 5 KG a PEN 125, 10 M a PEN 180 y 5 L a PEN 45. No confundió cantidad solicitada, importe de línea ni total con el precio de una presentación.
- Las dos lecturas reales se ejecutaron directamente con el adaptador, sin modificar la base del usuario. No se insertan los ejemplos automáticamente.

Los documentos son ficticios y claros. Dos resultados correctos no constituyen una evaluación general de precisión con fotos borrosas, promociones o diseños variados; siempre se requiere revisión humana.

El sistema no cambia automáticamente a otro proveedor, no activa facturación y no reintenta solicitudes externas. OpenAI permanece disponible; su prueba anterior respondió 429. PostgreSQL/Docker no se ejecutaron en esta validación.
