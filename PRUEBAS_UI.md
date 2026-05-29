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

## Observaciones tecnicas durante prueba

- Inicialmente hubo bloqueo CORS al ejecutar frontend en puerto 5174.
- Se ajusto el backend para permitir origen localhost (<http://localhost> y 127.0.0.1 en puertos de desarrollo), permitiendo continuar pruebas UI.

## Estado general

- Flujo principal de negocio desde UI: Operativo
- Integracion UI -> API -> PostgreSQL: Operativa
- Bug de mapeo de campos de burós: Corregido y validado
- Ronda adicional de pruebas UI: Exitosa
