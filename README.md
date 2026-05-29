# Prueba Tecnica GlobalGo

Backend y frontend para el proceso de evaluacion crediticia de GlobalGo.

## Estructura del proyecto

- `backend/`
  - `GlobalGo.Api`: entrada HTTP (controllers, modelos API, Swagger)
  - `GlobalGo.Application`: casos de uso, contratos y DTOs
  - `GlobalGo.Domain`: entidades y reglas de dominio
  - `GlobalGo.Infrastructure`: persistencia PostgreSQL + proveedores de fuentes externas
  - `GlobalGo.Tests`: tests unitarios
  - `GlobalGo.sln`
- `frontend/`
  - React + Vite + Tailwind + Lucide React
- `docker-compose.yml`
  - PostgreSQL para entorno local
- `DECISIONES.md`
  - resumen de decisiones tecnicas
- `PRUEBAS_UI.md`
  - ejecucion de pruebas funcionales desde la interfaz y hallazgos

## Stack tecnico

- .NET 8 (ASP.NET Core Web API)
- Clean Architecture (Api, Application, Domain, Infrastructure)
- Entity Framework Core + Npgsql (PostgreSQL)
- xUnit
- React + Vite + Tailwind + Lucide React
- Docker Compose

## Ejecucion rapida (un solo comando)

Desde la raiz:

```bash
npm install
npm run dev
```

Este comando:

- levanta PostgreSQL en Docker
- levanta backend .NET
- levanta frontend Vite

URLs:

- Frontend: `http://localhost:5173`
- Backend API: `http://localhost:5015`
- Swagger: `http://localhost:5015/swagger`

Para apagar servicios de Docker:

```bash
npm run down
```

## Ejecucion por separado

### 1) Base de datos

```bash
docker compose up -d
```

PostgreSQL queda en `localhost:5433` con:

- database: `globalgo_credit`
- user: `globalgo`
- password: `globalgo`

### 2) Backend

```bash
cd backend
dotnet restore
dotnet test
dotnet run --project GlobalGo.Api
```

### 3) Frontend

```bash
cd frontend
npm install
npm run dev
```

## Endpoints principales

- `POST /api/credit-evaluations`
- `GET /api/customers/{dni}/history`
- `GET /api/portfolio-risk/report`

## Notas de integracion

- La API tiene CORS habilitado para `http://localhost:5173`.
- Los enums de backend se serializan como texto (`Approved`, `Observed`, `Rejected`).
- El esquema de BD se inicializa al arrancar la API usando `EnsureCreated`.
- Las fuentes externas estan simuladas y se consultan en paralelo.
- La simulacion de burós incluye latencia variable, fallos transitorios controlados y reintentos automáticos.
- Parametros configurables en `appsettings`: `BureauApiSimulation:MinLatencyMs`, `MaxLatencyMs`, `MaxRetries`, `RetryBaseDelayMs`.

## Despliegue en VPS con Coolify + Gitea

### Estado actual del repositorio para deploy

- Backend containerizable: `backend/GlobalGo.Api/Dockerfile`
- Frontend containerizable: `frontend/Dockerfile`
- Proxy frontend -> API: `frontend/nginx.conf`
- Stack para Coolify: `docker-compose.coolify.yml`
- Variables ejemplo: `.env.coolify.example`

### Checklist en tu VPS

1. Coolify instalado y operativo en tu VPS.
2. Gitea accesible desde Coolify (misma red o acceso por HTTPS).
3. Repositorio subido a Gitea con estos archivos nuevos.
4. Dominio/subdominio configurado en Coolify para servicio `frontend`.
5. Variables de entorno definidas en Coolify (usar base de `.env.coolify.example`).

### Pasos en Coolify

1. Crear un recurso de tipo Docker Compose.
2. Seleccionar el repo en Gitea y rama `main`.
3. Indicar `docker-compose.coolify.yml` como compose file.
4. Configurar variables `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`.
5. Desplegar y validar:
  - Frontend cargando.
  - API respondiendo por `/api/credit-evaluations`.

### CORS en producción

- Si frontend y API van en dominios distintos, configura `CORS_ALLOWED_ORIGIN`.
- Si frontend usa el proxy nginx del mismo dominio (`/api`), CORS puede dejarse vacío.

### Seguridad

- Si el archivo `.env` contiene tokens reales, rotar credenciales antes del despliegue.
