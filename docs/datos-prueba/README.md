# Datos ficticios para probar Sistemas_Precios

Todos los nombres y precios son inventados. Los correos example.com son ejemplos y no se deben usar para contactar a nadie. No se insertan datos automáticamente en tu base.

## 1. Registrar proveedores

| Nombre | Correo opcional |
|---|---|
| Textiles Demo Norte | norte@example.com |
| Textiles Demo Centro | centro@example.com |

## 2. Cargar documentos

1. 01-Cotizacion-Norte.pdf: proveedor Norte, tipo Cotización, fecha 07/10/2026.
2. 02-Cotizacion-Centro.pdf: proveedor Centro, tipo Cotización, fecha 07/10/2026.
3. 03-Etiqueta-Centro.png: proveedor Centro, tipo Etiqueta, fecha 08/10/2026. Se incluye también su versión PDF.

Cada carga o relectura utiliza la API. Si continúa el error 429, puedes probar sesión y registro de proveedores; la lectura, revisión y comparación requerirán resolver cuota o límites. No hay un modo de OCR simulado ni una pantalla de alta manual de precios.

## 3. Revisar y aprobar

En Norte, usa respectivamente contenido 5 KG / precio 125, contenido 10 M / precio 180 y contenido 5 L / precio 45.
En Centro, usa 500 G / precio 15, 1 M / precio 20 y 1000 ML / precio 11.
En la etiqueta, usa 500 G / precio 16. Todos en PEN.

Contenido es lo que trae UNA presentación; no es la cantidad de unidades solicitadas en la cotización. Precio es el de UNA presentación; no el importe de la línea ni el total. Descarta cualquier fila de totales.

Usa exactamente estos nombres finales en ambas cotizaciones y en la etiqueta correspondiente:
- Hilo algodón 100% blanco - TextilDemo
- Tela drill azul - TelaDemo
- Detergente neutro - LimpiaDemo

El contenido ya tiene campos separados. Escribe el mismo nombre final en el campo Producto equivalente para que se reutilice el producto correspondiente. No apruebes presentaciones del mismo producto como si fueran productos diferentes.

## 4. Resultado esperado después de las dos cotizaciones

| Producto | Norte | Centro | Menor precio |
|---|---|---|---|
| Hilo | S/ 25 / KG | S/ 30 / KG | Norte |
| Tela | S/ 18 / M | S/ 20 / M | Norte |
| Detergente | S/ 9 / L | S/ 11 / L | Norte |

Después de aprobar la etiqueta del 08/10, el hilo de Centro debe pasar a S/ 32 / KG. Su observación anterior fue S/ 30 / KG: el historial refleja un aumento aproximado del 6.67%. Norte sigue siendo la opción de menor precio.

Si subes varias veces el mismo documento crearás observaciones adicionales: la interfaz no deduplica archivos por contenido. Usa una copia de prueba si quieres repetir el ejercicio sin afectar tu historial.
