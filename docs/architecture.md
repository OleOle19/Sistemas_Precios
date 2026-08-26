# Arquitectura

## Capas

- `apps/web`: dashboard, login, upload y revision.
- `services/api`: autenticacion, persistencia, procesamiento y consultas.
- `services/vision-rs`: OCR base via `gRPC`.
- `PostgreSQL`: usuarios, proveedores, documentos, productos, snapshots y comparaciones.

## Flujo

1. El usuario inicia sesion en la API con cookie.
2. Sube una foto o PDF desde la web.
3. La API guarda el archivo y encola un job de `Hangfire`.
4. El job llama a `vision-rs` por `gRPC`.
5. El backend estructura las lineas, propone matches y marca lineas a revisar.
6. El analista corrige/aprueba.
7. La API genera `PriceSnapshots` y recalcula `ComparisonResults`.

## Decisiones

- `.NET` es el centro del sistema para alinearse con perfil enterprise.
- `Rust` queda como microservicio tecnico concreto, no ornamental.
- El OCR inicial es mockeado y ampliable; la capa de estructuracion permanece desacoplada para integrar IA real despues.
- `EnsureCreated()` reduce friccion en local; migraciones quedan para una iteracion siguiente.
