# DECISIONES

## Qué prioricé

1. Flujo de negocio principal completo: evaluación de crédito, historial y reporte de riesgo.
2. Clean Architecture por proyectos (`Api`, `Application`, `Domain`, `Infrastructure`) para separar responsabilidades.
3. Consultas a burós simulados en paralelo y patrón extensible para agregar nuevas fuentes.
4. Persistencia real en PostgreSQL y ejecución local simple con Docker + comando único.
5. Pruebas unitarias del servicio de evaluación y pruebas funcionales desde UI documentadas.

## Reglas de decisión adoptadas

- Rechazado: identidad inválida/fallecido (RENIEC), score bajo, sobreendeudamiento alto o cobranza judicial.
- Aprobado: score alto, sin mora relevante, deuda/ingreso bajo y baja carga crediticia.
- Observado: casos intermedios.

## Solicitudes repetidas (mismo cliente)

Si mismo DNI solicita el mismo monto dentro de 24 horas, se reutiliza la última evaluación (idempotencia operativa).

## Arquitectura y patrón de fuentes externas

- `Api`: controllers, contratos HTTP y Swagger.
- `Application`: casos de uso, DTOs, interfaces.
- `Domain`: entidades y enums de negocio.
- `Infrastructure`: proveedores de burós y persistencia.
- Patrón aplicado: Strategy + Provider Pipeline con `ICreditSourceProvider` para incorporar nuevas fuentes sin cambiar la orquestación principal.
- Simulación de integración externa más realista: latencia variable, fallos transitorios y reintentos configurables en infraestructura.

## Persistencia

PostgreSQL con EF Core (`PostgresCreditEvaluationRepository`). El esquema se inicializa al arrancar la API (`EnsureCreated`). Entorno local con Docker (`localhost:5433`).

## Métricas del reporte de riesgo

- Total evaluaciones, monto total, ticket promedio.
- Ratio deuda/ingreso promedio.
- Tasa de rechazo y observación.
- Distribución por estado (cantidad, monto, porcentaje).

## Qué cambiaría con más tiempo

- Migrations EF Core versionadas (en lugar de `EnsureCreated`).
- Motor de reglas configurable/versionado.
- Circuit breaker y políticas por fuente (la simulación ya incluye retries y latencia).
- Seguridad y auditoría (autenticación, autorización, trazabilidad).
- Tests de integración y contract tests.
- Pipeline CI/CD con contenedores.

## Qué agregaría para producción real

- Observabilidad completa (logs, métricas, trazas).
- Gestión segura de datos sensibles (masking/encriptación/control de acceso).
- Feature flags para reglas de riesgo.
- Gobierno y monitoreo de performance del modelo de decisión.

## Herramientas utilizadas

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core + Npgsql
- xUnit
- Swagger
- React + Tailwind CSS + Lucide React (frontend en carpeta `frontend`)
- Docker Compose (PostgreSQL)
- IA: GitHub Copilot (GPT-5.3-Codex) para acelerar scaffolding y documentación
