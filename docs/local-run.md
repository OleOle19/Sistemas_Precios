# Ejecutar sin Docker

## Servicios

Abre 3 terminales desde la raiz del repo.

### 1. vision-rs

```powershell
cargo run --manifest-path services/vision-rs/Cargo.toml
```

Queda escuchando en `http://localhost:50051`.

### 2. API .NET

```powershell
dotnet run --project services/api
```

En modo local usa `SQLite` automaticamente y crea:

- `services/api/storage/sistemas-precios.dev.db`
- `services/api/storage/` para uploads

Queda en `http://localhost:8080`.

### 3. Frontend

```powershell
npm.cmd install --workspace web
npm.cmd --workspace web run dev
```

Queda en `http://localhost:3000`.

## Login demo

- `admin@precios.local` / `Admin123!`
- `analyst@precios.local` / `Analyst123!`

## Notas

- Si el upload te responde `401`, primero vuelve a entrar por la pantalla `Login`.
- Si quieres una prueba controlada, pega el contenido de `docs/demo-price-list-lines.txt` en el campo `Texto de apoyo OCR`.
- Docker sigue siendo util para demo y portafolio, pero ya no es requisito para desarrollar.
