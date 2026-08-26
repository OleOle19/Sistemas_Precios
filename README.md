# Sistema de Precios Comparativos con Fotos + IA

Monorepo de portafolio orientado a negocio real y arquitectura enterprise:

- `apps/web`: `Next.js` con dashboard, upload, revision y comparaciones.
- `services/api`: `ASP.NET Core` como backend principal, auth, dominio y orquestacion.
- `services/vision-rs`: microservicio `Rust` con OCR base simulado via `gRPC`.
- `infra`: `docker-compose` y configuracion local para `PostgreSQL`.

## Stack

- `Next.js` + `TypeScript`
- `ASP.NET Core 9`
- `Rust` + `tonic`
- `PostgreSQL`
- `Hangfire`
- `gRPC`

## Flujos v1

- Login con roles `admin` y `analyst`
- Alta de proveedores
- Subida de fotos o PDFs
- Extraccion OCR via `vision-rs`
- Estructuracion y matching automatico con revision humana
- Comparacion actual de precios por producto
- Historial y variacion porcentual

## Credenciales seed

- `admin@precios.local` / `Admin123!`
- `analyst@precios.local` / `Analyst123!`

## Arranque esperado

### Opcion Docker

1. Instalar Docker Desktop.
2. Ejecutar `docker compose -f infra/docker-compose.yml up --build`.
3. Abrir:
   - web: `http://localhost:3000`
   - api: `http://localhost:8080`
   - hangfire: `http://localhost:8080/hangfire`
   - vision-rs gRPC: `http://localhost:50051`

### Opcion local sin Docker

La experiencia local ya no depende de PostgreSQL. En desarrollo, la API usa `SQLite`.

1. `npm.cmd install --workspace web`
2. `dotnet restore SistemasPrecios.sln`
3. `cargo build --manifest-path services/vision-rs/Cargo.toml`
4. Levantar `vision-rs`
5. Levantar la API `.NET`
6. Levantar `Next.js`

Comandos:

```powershell
cargo run --manifest-path services/vision-rs/Cargo.toml
dotnet run --project services/api
npm.cmd --workspace web run dev
```

Archivos locales generados:

- base local: `services/api/storage/sistemas-precios.dev.db`
- archivos subidos: `services/api/storage/`

## Notas

- `vision-rs` soporta sidecars `.txt`: si subes `lista.jpg` y existe `lista.txt` junto al archivo, usara esas lineas como OCR de prueba.
- El backend usa `EnsureCreated()` para una primera experiencia local simple. Para una fase siguiente conviene migrar a `EF Core Migrations`.
