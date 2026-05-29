# Pruebas UI

Fecha: 2026-05-29

## Entorno de prueba

- Frontend: <http://localhost:5174>
- Backend API: <http://localhost:5015>
- Swagger: <http://localhost:5015/swagger>
- Base de datos: PostgreSQL en Docker (globalgo_credit)

## Casos ejecutados

| ID | Caso | Pasos | Resultado esperado | Resultado observado | Estado |
| --- | --- | --- | --- | --- | --- |
| UI-01 | Carga inicial de la pantalla | Abrir frontend y revisar secciones de evaluacion, historial y riesgo | La UI carga sin errores y muestra formulario + paneles | Pantalla carga correctamente con todas las secciones visibles | OK |
| UI-02 | Evaluacion de credito (primera solicitud) | Click en Evaluar solicitud con datos por defecto (DNI 12345678, monto 5200) | Se registra evaluacion, muestra decision y justificacion | Se muestra decision Rejected y justificacion; historial y reporte se actualizan | OK |
| UI-03 | Historial de cliente | Luego de UI-02 revisar panel Historial cliente | Debe mostrar al menos 1 evaluacion del DNI | Se muestra 1 registro con estado Rejected, monto y fecha | OK |
| UI-04 | Reporte de riesgo de cartera | Luego de UI-02 revisar panel Riesgo cartera | Debe mostrar metricas agregadas y distribucion por estado | Se muestran total evaluaciones=1, monto total, ticket promedio y distribucion | OK |
| UI-05 | Solicitud repetida (idempotencia funcional) | Repetir click en Evaluar solicitud con mismo DNI y monto | Debe reutilizar evaluacion reciente (sin duplicar cartera) | Campo Reuso de solicitud reciente cambia a Si y cartera se mantiene en 1 evaluacion | OK |

## Hallazgos

### BUG-01: Campos de burós mostrados como undefined en Resultado

- Severidad: Media
- Evidencia en UI:
  - Equifax: `Score: undefined`
  - SBS: `Debt/Income: undefined` y `Creditos activos: undefined`
- Impacto:
  - La decision final se muestra correctamente, pero la desagregacion de datos de burós es inconsistente para usuario final.
- Causa probable:
  - Diferencia de nombres de propiedades entre modelo de API y lo que el frontend renderiza en App.jsx.
- Estado:
  - Resuelto.

## Re-test despues de correccion

| ID | Caso | Resultado esperado | Resultado observado | Estado |
| --- | --- | --- | --- | --- |
| UI-RT-01 | Render de detalle de burós tras evaluar solicitud | Debe mostrar valores numericos/booleanos reales (sin `undefined`) | Se visualiza `Score: 555`, `Debt/Income: 0.86`, `Creditos activos: 1`, `Identidad valida: Si` | OK |
| UI-RT-02 | Flujo completo despues del fix | Evaluacion + historial + reporte deben seguir operativos | Flujo completo operativo sin regresiones visibles en UI | OK |

## Ronda adicional de pruebas UI (continuacion)

| ID | Caso | Resultado esperado | Resultado observado | Estado |
| --- | --- | --- | --- | --- |
| UI-RT-03 | Nueva evaluacion con otro DNI/monto | Debe crear nueva evaluacion (no reuso) y mostrar detalle de burós sin `undefined` | DNI `87654321`, monto `4500`, reuso `No`, detalle visible (`Score: 414`, `Debt/Income: 0.53`, `Creditos activos: 3`) | OK |
| UI-RT-04 | Actualizacion de cartera tras nueva evaluacion | El reporte debe reflejar incremento de evaluaciones y montos | Total evaluaciones `2`, monto total `S/ 9700`, ticket promedio `S/ 4850` | OK |
| UI-RT-05 | Historial por DNI evaluado | Debe mostrar el ultimo registro del DNI consultado | Historial muestra registro con monto `S/ 4500` y estado `Rejected` | OK |

## Revalidacion en produccion

Fecha: 2026-05-29

Entorno validado:

- Frontend productivo: <https://globalgo.gargurevich.dev>
- Swagger productivo: <https://globalgo.gargurevich.dev/swagger/index.html>
- Docs publicadas: <https://globalgo.gargurevich.dev/docs/README.md>

| ID | Caso | Resultado esperado | Resultado observado | Estado |
| --- | --- | --- | --- | --- |
| UI-PROD-01 | Disponibilidad de frontend productivo | La home debe responder sin error | `GET /` responde `200` | OK |
| UI-PROD-02 | Disponibilidad de Swagger en produccion | Swagger debe abrir en el mismo dominio | `GET /swagger/index.html` responde `200` | OK |
| UI-PROD-03 | Nueva evaluacion en produccion | La solicitud debe registrar evaluacion y devolver decision + detalle de burós | DNI `10000992`, monto `3600`, ingreso `4200`; respuesta `200` con decision `Observed`, Equifax `637`, SBS `0.48 / 2`, RENIEC valido | OK |
| UI-PROD-04 | Historial por DNI en produccion | El historial debe devolver la evaluacion creada con detalle completo | `GET /api/customers/10000992/history` responde `200` con 1 item y bloques `equifax`, `reniec`, `sbs` | OK |
| UI-PROD-05 | Reporte de cartera en produccion | El reporte debe reflejar el incremento tras nueva evaluacion | `GET /api/portfolio-risk/report` responde `200`; total evaluaciones `54`, observadas `21`, rechazadas `22`, aprobadas `11` | OK |
| UI-PROD-06 | Publicacion del README con diagrama | El README publicado debe incluir la seccion del diagrama | `GET /docs/README.md` contiene `## Diagrama de arquitectura` y el bloque `flowchart LR` | OK |

## Re-test del render Mermaid

Fecha: 2026-05-29

Entorno validado:

- Preview local del build: <http://127.0.0.1:4174>

| ID | Caso | Resultado esperado | Resultado observado | Estado |
| --- | --- | --- | --- | --- |
| UI-RT-06 | Render del diagrama Mermaid en visor de documentacion | El bloque `mermaid` debe renderizarse como diagrama SVG dentro de la pestana Documentacion | En `README`, la seccion `Diagrama de arquitectura` renderiza SVG; el arbol de accesibilidad expone nodos `Usuario`, `Frontend React + Vite + Tailwind`, `ASP.NET Core API .NET 8` y `Simulated Bureau API Gateway` | OK |

## Hallazgos adicionales

### BUG-02: El visor de documentacion no renderiza Mermaid como diagrama

- Severidad: Baja
- Evidencia tecnica:
  - El archivo publicado en produccion sí contiene la seccion `Diagrama de arquitectura`.
  - Tras integrar Mermaid en `MarkdownViewer`, el preview del build renderiza el bloque como SVG dentro de la UI.
- Impacto:
  - Antes del fix, el diagrama existia en el markdown, pero en la UI no se renderizaba como diagrama.
- Causa probable:
  - Faltaba soporte Mermaid en el visor markdown del frontend.
- Estado:
  - Resuelto en codigo y validado en preview local. Pendiente propagacion a produccion cuando Coolify complete el redeploy del commit `9353ea6`.

## Observaciones tecnicas durante prueba

- Inicialmente hubo bloqueo CORS al ejecutar frontend en puerto 5174.
- Se ajusto el backend para permitir origen localhost (<http://localhost> y 127.0.0.1 en puertos de desarrollo), permitiendo continuar pruebas UI.
- El redeploy de Coolify no termino de propagar el commit `9353ea6` durante esta ronda; la validacion visual final del render Mermaid se hizo sobre `vite preview` del build generado.

## Estado general

- Flujo principal de negocio desde UI: Operativo
- Integracion UI -> API -> PostgreSQL: Operativa
- Bug de mapeo de campos de burós: Corregido y validado
- Ronda adicional de pruebas UI: Exitosa
- Revalidacion productiva: Exitosa
- Publicacion del README con diagrama: Validada en origen publicado
- Render Mermaid dentro del visor de documentacion: Corregido y validado en preview local