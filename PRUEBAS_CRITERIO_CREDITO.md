# GlobalGo - Criterio de Decision y Casos de Prueba

Este documento resume el criterio actual del motor de decision de credito y deja casos de prueba listos para validar los tres estados: `Approved`, `Observed` y `Rejected`.

## 1) Criterio de decision actual

La decision se calcula con informacion simulada de Equifax, RENIEC y SBS.

### Orden de evaluacion

1. Rechazo por identidad (RENIEC)
- Si `identityValid = false` o `isDeceased = true` => `Rejected`
- Justificacion: `RENIEC indica identidad no válida o persona fallecida.`

2. Rechazo por riesgo alto
- Si `hasJudicialCollection = true` (SBS) => `Rejected`
- O si `debtToIncomeRatio > 0.70` (SBS) => `Rejected`
- O si `score < 500` (Equifax) => `Rejected`
- Justificacion: `Riesgo alto por score bajo, sobreendeudamiento o cobranza judicial.`

3. Aprobacion automatica (todas deben cumplirse)
- `score >= 700`
- `hasDelinquency = false`
- `debtToIncomeRatio <= 0.40`
- `activeCredits <= 1`
- `amountRequested / monthlyIncome <= 0.80`
- Resultado: `Approved`
- Justificacion: `Perfil de riesgo saludable para aprobación automática.`

4. Caso intermedio
- Si no cae en rechazo y tampoco cumple todas las condiciones de aprobacion automatica => `Observed`
- Justificacion: `Caso intermedio: requiere observación manual por riesgo moderado.`

## 2) Consideraciones importantes de prueba

- La simulacion es deterministica por DNI: el mismo DNI tiende a producir los mismos indicadores de bureaus.
- Cambiar solo monto/ingreso puede cambiar el resultado final, pero no cambia el perfil base asociado al DNI.
- Si se repite mismo DNI + mismo monto en ventana reciente, puede reusar evaluacion previa.
- Para pruebas por API usar campos PascalCase en JSON:
  - `Dni`
  - `FullName`
  - `AmountRequested`
  - `MonthlyIncome`

## 3) Casos validados en produccion

Dominio validado: `https://globalgo.gargurevich.dev`

### 3.0 Checklist QA rapido (resumen)

| Estado esperado | AmountRequested | MonthlyIncome | DNIs de prueba |
| --- | ---: | ---: | --- |
| Approved | 2400 | 4000 | 10000012, 10000013, 10000018, 10000019, 10000102, 10000103, 10000108, 10000109, 10000120, 10000121 |
| Observed | 3500 | 4000 | 10000000, 10000001, 10000002, 10000003, 10000004, 10000005, 10000006, 10000007, 10000009, 10000010 |
| Rejected | 2400 | 4000 | 10000008, 10000020, 10000021, 10000022, 10000023, 10000024, 10000025, 10000026, 10000044, 10000050 |

Notas rapidas:
- Los DNIs `10000008`, `10000026` y `10000044` rechazan por RENIEC.
- Los demas DNIs de `Rejected` rechazan por riesgo alto.

### 3.1 Casos APPROVED

Usar:
- `AmountRequested`: `2400`
- `MonthlyIncome`: `4000`

DNIs validados (todos retornan `Approved`):
- `10000012`
- `10000013`
- `10000018`
- `10000019`
- `10000102`
- `10000103`
- `10000108`
- `10000109`
- `10000120`
- `10000121`

### 3.2 Casos OBSERVED

Usar:
- `AmountRequested`: `3500`
- `MonthlyIncome`: `4000`

DNIs validados (todos retornan `Observed`):
- `10000000`
- `10000001`
- `10000002`
- `10000003`
- `10000004`
- `10000005`
- `10000006`
- `10000007`
- `10000009`
- `10000010`

### 3.3 Casos REJECTED

Usar:
- `AmountRequested`: `2400`
- `MonthlyIncome`: `4000`

DNIs validados (todos retornan `Rejected`):
- `10000008` (rechazo por RENIEC)
- `10000020` (rechazo por riesgo alto)
- `10000021` (rechazo por riesgo alto)
- `10000022` (rechazo por riesgo alto)
- `10000023` (rechazo por riesgo alto)
- `10000024` (rechazo por riesgo alto)
- `10000025` (rechazo por riesgo alto)
- `10000026` (rechazo por RENIEC)
- `10000044` (rechazo por RENIEC)
- `10000050` (rechazo por riesgo alto)

## 4) Payload de ejemplo

```json
{
  "Dni": "10000012",
  "FullName": "Demo Approved",
  "AmountRequested": 2400,
  "MonthlyIncome": 4000
}
```

```json
{
  "Dni": "10000000",
  "FullName": "Demo Observed",
  "AmountRequested": 3500,
  "MonthlyIncome": 4000
}
```

```json
{
  "Dni": "10000020",
  "FullName": "Demo Rejected",
  "AmountRequested": 2400,
  "MonthlyIncome": 4000
}
```

## 5) Curl de verificacion rapida

```bash
curl -sS -H 'Content-Type: application/json' \
  -d '{"Dni":"10000012","FullName":"Demo Approved","AmountRequested":2400,"MonthlyIncome":4000}' \
  https://globalgo.gargurevich.dev/api/credit-evaluations
```

```bash
curl -sS -H 'Content-Type: application/json' \
  -d '{"Dni":"10000000","FullName":"Demo Observed","AmountRequested":3500,"MonthlyIncome":4000}' \
  https://globalgo.gargurevich.dev/api/credit-evaluations
```

```bash
curl -sS -H 'Content-Type: application/json' \
  -d '{"Dni":"10000020","FullName":"Demo Rejected","AmountRequested":2400,"MonthlyIncome":4000}' \
  https://globalgo.gargurevich.dev/api/credit-evaluations
```
