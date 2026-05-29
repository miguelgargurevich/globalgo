import { useEffect, useMemo, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import {
  AlertTriangle,
  Bike,
  BarChart3,
  CircleX,
  ClipboardList,
  Filter,
  History,
  Loader2,
  Search,
  ShieldCheck,
  SquareLibrary,
  Users,
} from 'lucide-react'

const decisionStyles = {
  Approved: 'bg-emerald-100 text-emerald-800 border-emerald-200',
  Observed: 'bg-amber-100 text-amber-800 border-amber-200',
  Rejected: 'bg-rose-100 text-rose-800 border-rose-200',
}

const decisionChartPalette = {
  Approved: '#10b981',
  Observed: '#f59e0b',
  Rejected: '#f43f5e',
}

const defaultForm = {
  dni: '12345678',
  fullName: 'Juan Perez',
  amountRequested: '5200',
  monthlyIncome: '3000',
}

const docsFiles = [
  { label: 'README', path: '/docs/README.md' },
  { label: 'Decisiones', path: '/docs/DECISIONES.md' },
  { label: 'Pruebas Criterio', path: '/docs/PRUEBAS_CRITERIO_CREDITO.md' },
]

let mermaidInitialized = false
let mermaidModulePromise = null

async function getMermaid() {
  if (!mermaidModulePromise) {
    mermaidModulePromise = import('mermaid').then(({ default: mermaid }) => {
      if (!mermaidInitialized) {
        mermaid.initialize({
          startOnLoad: false,
          securityLevel: 'loose',
          theme: 'neutral',
        })

        mermaidInitialized = true
      }

      return mermaid
    })
  }

  return mermaidModulePromise
}

function App() {
  const [activeTab, setActiveTab] = useState('evaluation')
  const [apiBase, setApiBase] = useState(
    import.meta.env.VITE_API_BASE_URL ?? window.location.origin,
  )
  const [form, setForm] = useState(defaultForm)
  const [loadingEvaluation, setLoadingEvaluation] = useState(false)
  const [loadingHistory, setLoadingHistory] = useState(false)
  const [loadingReport, setLoadingReport] = useState(false)
  const [error, setError] = useState('')
  const [evaluation, setEvaluation] = useState(null)
  const [history, setHistory] = useState([])
  const [generalHistory, setGeneralHistory] = useState([])
  const [loadingGeneralHistory, setLoadingGeneralHistory] = useState(false)
  const [report, setReport] = useState(null)
  const [loadingCustomerProfile, setLoadingCustomerProfile] = useState(false)
  const [dniKnownCustomer, setDniKnownCustomer] = useState(null)
  const [selectedDoc, setSelectedDoc] = useState(docsFiles[0])
  const [docContent, setDocContent] = useState('')
  const [loadingDoc, setLoadingDoc] = useState(false)
  const [docError, setDocError] = useState('')
  const [historyFilters, setHistoryFilters] = useState({
    dni: '',
    decision: '',
    fromDate: '',
    toDate: '',
    minAmount: '',
    maxAmount: '',
  })

  const statusPillClass = useMemo(() => {
    if (!evaluation?.decision) {
      return 'bg-slate-100 text-slate-700 border-slate-200'
    }

    return decisionStyles[evaluation.decision] ?? 'bg-slate-100 text-slate-700 border-slate-200'
  }, [evaluation])

  async function callApi(path, options = {}) {
    const response = await fetch(`${apiBase}${path}`, options)
    if (!response.ok) {
      throw new Error(`Error ${response.status}: no se pudo completar la operación.`)
    }

    return response.json()
  }

  async function onEvaluate(event) {
    event.preventDefault()
    setError('')
    setLoadingEvaluation(true)

    try {
      await lookupCustomerProfile(form.dni)

      const payload = {
        dni: form.dni,
        fullName: form.fullName,
        amountRequested: Number(form.amountRequested),
        monthlyIncome: Number(form.monthlyIncome),
      }

      const data = await callApi('/api/credit-evaluations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })

      setEvaluation(data)

      // Refresca tarjetas dependientes luego de evaluar.
      await Promise.allSettled([
        loadHistory(data.dni ?? form.dni),
        loadReport(),
      ])
    } catch (apiError) {
      setError(apiError.message)
    } finally {
      setLoadingEvaluation(false)
    }
  }

  async function lookupCustomerProfile(dniValue = form.dni) {
    const normalizedDni = String(dniValue ?? '').trim()

    if (normalizedDni.length !== 8) {
      setDniKnownCustomer(null)
      return null
    }

    setLoadingCustomerProfile(true)

    try {
      const profile = await callApi(`/api/customers/${normalizedDni}/profile`)

      if (profile.exists && profile.fullName) {
        setForm((current) => ({ ...current, dni: normalizedDni, fullName: profile.fullName }))
        setDniKnownCustomer(profile)
      } else {
        setDniKnownCustomer({ ...profile, fullName: null })
      }

      return profile
    } catch (apiError) {
      setError(apiError.message)
      return null
    } finally {
      setLoadingCustomerProfile(false)
    }
  }

  function onDniKeyDown(event) {
    if (event.key === 'Enter') {
      event.preventDefault()
      lookupCustomerProfile(form.dni)
    }
  }

  async function loadHistory(dni = form.dni) {
    setLoadingHistory(true)
    try {
      const data = await callApi(`/api/customers/${dni}/history`)
      setHistory(data)
    } catch (apiError) {
      setError(apiError.message)
    } finally {
      setLoadingHistory(false)
    }
  }

  async function loadReport() {
    setLoadingReport(true)
    try {
      const data = await callApi('/api/portfolio-risk/report')
      setReport(data)
    } catch (apiError) {
      setError(apiError.message)
    } finally {
      setLoadingReport(false)
    }
  }

  function buildHistoryQueryParams() {
    const params = new URLSearchParams()

    if (historyFilters.dni.trim()) {
      params.set('dni', historyFilters.dni.trim())
    }

    if (historyFilters.decision) {
      params.set('decision', historyFilters.decision)
    }

    if (historyFilters.fromDate) {
      params.set('fromUtc', `${historyFilters.fromDate}T00:00:00Z`)
    }

    if (historyFilters.toDate) {
      params.set('toUtc', `${historyFilters.toDate}T23:59:59Z`)
    }

    if (historyFilters.minAmount) {
      params.set('minAmount', historyFilters.minAmount)
    }

    if (historyFilters.maxAmount) {
      params.set('maxAmount', historyFilters.maxAmount)
    }

    return params.toString()
  }

  async function loadGeneralHistory() {
    setError('')
    setLoadingGeneralHistory(true)

    try {
      const query = buildHistoryQueryParams()
      const path = query
        ? `/api/credit-evaluations/history?${query}`
        : '/api/credit-evaluations/history'

      const data = await callApi(path)
      setGeneralHistory(data)
    } catch (apiError) {
      setError(apiError.message)
    } finally {
      setLoadingGeneralHistory(false)
    }
  }

  async function loadDoc(doc = selectedDoc) {
    setDocError('')
    setLoadingDoc(true)

    try {
      const response = await fetch(doc.path)
      if (!response.ok) {
        throw new Error(`No se pudo cargar ${doc.label}.`)
      }

      const content = await response.text()
      setDocContent(content)
    } catch (loadError) {
      setDocError(loadError.message)
      setDocContent('')
    } finally {
      setLoadingDoc(false)
    }
  }

  useEffect(() => {
    if (activeTab === 'history') {
      loadGeneralHistory()
    }
    if (activeTab === 'docs') {
      loadDoc()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeTab])

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top_left,_#fff6da_0%,_#fefefe_42%,_#e8f6f3_100%)] px-4 pb-16 pt-8 text-slate-800">
      <div className="mx-auto w-full max-w-6xl">
        <header className="mb-8 overflow-hidden rounded-3xl border border-slate-200 bg-white/90 p-6 shadow-xl shadow-slate-200/40 backdrop-blur sm:p-8">
          <div className="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="mb-2 inline-flex items-center gap-2 rounded-full bg-teal-100 px-3 py-1 text-xs font-semibold uppercase tracking-[0.2em] text-teal-800">
                <Bike size={14} />
                GlobalGo Risk Desk
              </p>
              <h1 className="font-display text-3xl text-slate-900 sm:text-5xl">
                Evaluacion de credito para motos
              </h1>
              <p className="mt-2 max-w-2xl text-sm text-slate-600 sm:text-base">
                Consola operativa para evaluar solicitudes, revisar historial por cliente y
                monitorear riesgo agregado de cartera.
              </p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm">
              <label className="mb-1 block font-semibold text-slate-700">API base URL</label>
              <input
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none ring-teal-500 transition focus:ring"
                value={apiBase}
                onChange={(event) => setApiBase(event.target.value)}
                placeholder="http://localhost:5015"
              />
            </div>
          </div>
        </header>

        {error && (
          <div className="mb-6 flex items-center gap-2 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
            <CircleX size={16} />
            {error}
          </div>
        )}

        <section className="mb-6 rounded-2xl border border-slate-200 bg-white p-3 shadow-sm">
          <div className="grid gap-2 sm:grid-cols-3">
            <TabButton
              active={activeTab === 'evaluation'}
              icon={ShieldCheck}
              title="Evaluacion"
              subtitle="Nueva solicitud, historial por DNI y riesgo"
              onClick={() => setActiveTab('evaluation')}
            />
            <TabButton
              active={activeTab === 'history'}
              icon={History}
              title="Historial total"
              subtitle="Todas las evaluaciones con filtros"
              onClick={() => setActiveTab('history')}
            />
            <TabButton
              active={activeTab === 'docs'}
              icon={SquareLibrary}
              title="Documentacion"
              subtitle="Visor de markdown interno"
              onClick={() => setActiveTab('docs')}
            />
          </div>
        </section>

        {activeTab === 'evaluation' && (
          <>
            <section className="grid gap-6 lg:grid-cols-2">
          <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-200/40">
            <h2 className="mb-4 flex items-center gap-2 font-display text-2xl text-slate-900">
              <ShieldCheck className="text-teal-700" />
              Nueva evaluacion
            </h2>
            <form className="grid gap-3" onSubmit={onEvaluate}>
              <Field
                label="DNI"
                value={form.dni}
                onChange={(value) => {
                  setForm((current) => ({ ...current, dni: value }))
                  setDniKnownCustomer(null)
                }}
                onBlur={() => lookupCustomerProfile(form.dni)}
                onKeyDown={onDniKeyDown}
                placeholder="12345678"
              />
              <Field
                label="Nombre completo"
                value={form.fullName}
                onChange={(value) => setForm((current) => ({ ...current, fullName: value }))}
                placeholder="Nombre y apellido"
                disabled={Boolean(dniKnownCustomer?.exists)}
              />
              <p className="text-xs text-slate-500">
                {loadingCustomerProfile && 'Buscando DNI en la base...'}
                {!loadingCustomerProfile && dniKnownCustomer?.exists && (
                  <>
                    DNI registrado: se usara el nombre existente (<strong>{dniKnownCustomer.fullName}</strong>).
                  </>
                )}
                {!loadingCustomerProfile && dniKnownCustomer && !dniKnownCustomer.exists && (
                  <>DNI nuevo: puedes ingresar el nombre para registrarlo.</>
                )}
                {!loadingCustomerProfile && dniKnownCustomer === null && (
                  <>Presiona Enter en DNI o sal del campo para validar y autocompletar.</>
                )}
              </p>
              <Field
                label="Monto solicitado"
                value={form.amountRequested}
                onChange={(value) => setForm((current) => ({ ...current, amountRequested: value }))}
                placeholder="5200"
              />
              <Field
                label="Ingreso mensual"
                value={form.monthlyIncome}
                onChange={(value) => setForm((current) => ({ ...current, monthlyIncome: value }))}
                placeholder="3000"
              />
              <button
                type="submit"
                disabled={loadingEvaluation}
                className="mt-2 inline-flex items-center justify-center gap-2 rounded-xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:bg-slate-400"
              >
                {loadingEvaluation ? <Loader2 size={16} className="animate-spin" /> : <ClipboardList size={16} />}
                Evaluar solicitud
              </button>
              <p className="text-xs text-slate-500">
                Si cambias el monto, se registra una nueva evaluacion aunque la decision final pueda coincidir.
              </p>
            </form>
          </article>

          <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-200/40">
            <div className="mb-3 flex items-center justify-between gap-3">
              <h2 className="font-display text-2xl text-slate-900">Resultado</h2>
              {evaluation?.decision && (
                <span className={`rounded-full border px-3 py-1 text-xs font-semibold uppercase tracking-wide ${statusPillClass}`}>
                  {evaluation.decision}
                </span>
              )}
            </div>

            {!evaluation && (
              <p className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm text-slate-600">
                Todavia no hay evaluacion. Completa el formulario para obtener una decision.
              </p>
            )}

            {evaluation && (
              <div className="space-y-3 text-sm text-slate-700">
                {(() => {
                  const equifax = evaluation.equifax ?? evaluation.equifaxReport
                  const reniec = evaluation.reniec ?? evaluation.reniecReport
                  const sbs = evaluation.sbs ?? evaluation.sbsReport

                  return (
                    <>
                <p>
                  <strong>Justificacion:</strong> {evaluation.justification}
                </p>
                <p>
                  <strong>Monto evaluado:</strong> S/ {evaluation.amountRequested}
                </p>
                <p>
                  <strong>Ingreso evaluado:</strong> S/ {evaluation.monthlyIncome}
                </p>
                <p>
                  <strong>Fecha de evaluacion:</strong>{' '}
                  {new Date(evaluation.createdAtUtc).toLocaleString()}
                </p>
                <p>
                  <strong>ID de evaluacion:</strong> {evaluation.evaluationId}
                </p>
                <p>
                  <strong>Reuso de solicitud reciente:</strong>{' '}
                  {evaluation.reusedRecentEvaluation ? 'Si' : 'No'}
                </p>
                <div className="grid gap-3 sm:grid-cols-3">
                  <BureauCard
                    title="Equifax"
                    lines={[
                      `Score: ${equifax?.score}`,
                      `Mora: ${equifax?.hasDelinquency ? 'Si' : 'No'}`,
                    ]}
                  />
                  <BureauCard
                    title="RENIEC"
                    lines={[
                      `Identidad valida: ${reniec?.identityValid ? 'Si' : 'No'}`,
                      `Fallecido: ${reniec?.isDeceased ? 'Si' : 'No'}`,
                    ]}
                  />
                  <BureauCard
                    title="SBS"
                    lines={[
                      `Debt/Income: ${sbs?.debtToIncomeRatio}`,
                      `Creditos activos: ${sbs?.activeCredits}`,
                    ]}
                  />
                </div>
                    </>
                  )
                })()}
              </div>
            )}
          </article>
            </section>

            <section className="mt-6 grid gap-6 lg:grid-cols-2">
          <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-200/40">
            <div className="mb-4 flex items-center justify-between">
              <h2 className="flex items-center gap-2 font-display text-2xl text-slate-900">
                <Users className="text-teal-700" />
                Historial cliente
              </h2>
              <button
                type="button"
                onClick={() => loadHistory()}
                disabled={loadingHistory}
                className="inline-flex items-center gap-2 rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loadingHistory ? <Loader2 size={14} className="animate-spin" /> : <Search size={14} />}
                Buscar
              </button>
            </div>

            <div className="space-y-2">
              {history.length === 0 && (
                <p className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm text-slate-600">
                  Sin evaluaciones para este DNI.
                </p>
              )}
              {history.map((item) => {
                const equifax = item.equifax ?? item.equifaxReport
                const reniec = item.reniec ?? item.reniecReport
                const sbs = item.sbs ?? item.sbsReport

                return (
                  <div key={item.evaluationId} className="rounded-xl border border-slate-200 p-3 text-sm">
                    <p className="font-semibold text-slate-900">
                      {item.decision} {item.reusedRecentEvaluation ? '(Reusada)' : '(Nueva)'}
                    </p>
                    <p className="text-slate-600">Monto: S/ {item.amountRequested}</p>
                    <p className="text-slate-600">Fecha: {new Date(item.createdAtUtc).toLocaleString()}</p>

                    <details className="mt-3 rounded-xl border border-slate-200 bg-slate-50 p-2">
                      <summary className="cursor-pointer select-none text-xs font-semibold uppercase tracking-wide text-slate-600">
                        Ver detalle de buros
                      </summary>
                      <div className="mt-3 grid gap-2 sm:grid-cols-3">
                        <BureauCard
                          title="Equifax"
                          lines={[
                            `Score: ${equifax?.score}`,
                            `Mora: ${equifax?.hasDelinquency ? 'Si' : 'No'}`,
                          ]}
                        />
                        <BureauCard
                          title="RENIEC"
                          lines={[
                            `Identidad valida: ${reniec?.identityValid ? 'Si' : 'No'}`,
                            `Fallecido: ${reniec?.isDeceased ? 'Si' : 'No'}`,
                          ]}
                        />
                        <BureauCard
                          title="SBS"
                          lines={[
                            `Debt/Income: ${sbs?.debtToIncomeRatio}`,
                            `Creditos activos: ${sbs?.activeCredits}`,
                          ]}
                        />
                      </div>
                    </details>
                  </div>
                )
              })}
            </div>
          </article>

          <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-200/40">
            <div className="mb-4 flex items-center justify-between">
              <h2 className="flex items-center gap-2 font-display text-2xl text-slate-900">
                <BarChart3 className="text-teal-700" />
                Riesgo cartera
              </h2>
              <button
                type="button"
                onClick={loadReport}
                disabled={loadingReport}
                className="inline-flex items-center gap-2 rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loadingReport ? <Loader2 size={14} className="animate-spin" /> : <AlertTriangle size={14} />}
                Refrescar
              </button>
            </div>

            {!report && (
              <p className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm text-slate-600">
                Carga el reporte para ver metricas de cartera.
              </p>
            )}

            {report && (
              <div className="space-y-3 text-sm">
                <Metric label="Total evaluaciones" value={report.totalEvaluations} />
                <Metric label="Monto total" value={formatCurrency(report.totalAmountEvaluated)} />
                <Metric label="Ticket promedio" value={formatCurrency(report.averageAmount)} />
                <Metric label="Tasa rechazo" value={`${report.rejectionRate}%`} />
                <DistributionChart items={report.distributionByState ?? []} />
              </div>
            )}
          </article>
            </section>
          </>
        )}

        {activeTab === 'history' && (
          <section className="grid gap-6">
            <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-200/40">
              <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <h2 className="flex items-center gap-2 font-display text-2xl text-slate-900">
                  <History className="text-teal-700" />
                  Historial total de evaluaciones
                </h2>
                <button
                  type="button"
                  onClick={loadGeneralHistory}
                  disabled={loadingGeneralHistory}
                  className="inline-flex items-center gap-2 rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {loadingGeneralHistory ? <Loader2 size={14} className="animate-spin" /> : <Filter size={14} />}
                  Aplicar filtros
                </button>
              </div>

              <div className="mb-5 grid gap-3 md:grid-cols-3 lg:grid-cols-6">
                <Field
                  label="DNI"
                  value={historyFilters.dni}
                  onChange={(value) => setHistoryFilters((current) => ({ ...current, dni: value }))}
                  placeholder="12345678"
                />

                <label className="text-sm font-semibold text-slate-700">
                  Decision
                  <select
                    className="mt-1 block w-full rounded-xl border border-slate-300 bg-slate-50 px-3 py-2 font-normal outline-none ring-teal-500 transition focus:bg-white focus:ring"
                    value={historyFilters.decision}
                    onChange={(event) =>
                      setHistoryFilters((current) => ({ ...current, decision: event.target.value }))
                    }
                  >
                    <option value="">Todas</option>
                    <option value="Approved">Approved</option>
                    <option value="Observed">Observed</option>
                    <option value="Rejected">Rejected</option>
                  </select>
                </label>

                <label className="text-sm font-semibold text-slate-700">
                  Desde
                  <input
                    type="date"
                    className="mt-1 block w-full rounded-xl border border-slate-300 bg-slate-50 px-3 py-2 font-normal outline-none ring-teal-500 transition focus:bg-white focus:ring"
                    value={historyFilters.fromDate}
                    onChange={(event) =>
                      setHistoryFilters((current) => ({ ...current, fromDate: event.target.value }))
                    }
                  />
                </label>

                <label className="text-sm font-semibold text-slate-700">
                  Hasta
                  <input
                    type="date"
                    className="mt-1 block w-full rounded-xl border border-slate-300 bg-slate-50 px-3 py-2 font-normal outline-none ring-teal-500 transition focus:bg-white focus:ring"
                    value={historyFilters.toDate}
                    onChange={(event) =>
                      setHistoryFilters((current) => ({ ...current, toDate: event.target.value }))
                    }
                  />
                </label>

                <Field
                  label="Monto min"
                  value={historyFilters.minAmount}
                  onChange={(value) => setHistoryFilters((current) => ({ ...current, minAmount: value }))}
                  placeholder="500"
                />

                <Field
                  label="Monto max"
                  value={historyFilters.maxAmount}
                  onChange={(value) => setHistoryFilters((current) => ({ ...current, maxAmount: value }))}
                  placeholder="15000"
                />
              </div>

              <div className="mb-3 rounded-xl border border-teal-200 bg-teal-50 px-3 py-2 text-sm text-teal-900">
                Total resultados: <strong>{generalHistory.length}</strong>
              </div>

              <div className="space-y-2">
                {generalHistory.length === 0 && (
                  <p className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm text-slate-600">
                    No se encontraron evaluaciones con esos filtros.
                  </p>
                )}

                {generalHistory.map((item) => {
                  const statusClass = decisionStyles[item.decision] ?? 'bg-slate-100 text-slate-700 border-slate-200'

                  return (
                    <div key={item.evaluationId} className="rounded-xl border border-slate-200 p-4 text-sm">
                      <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                        <p className="font-semibold text-slate-900">
                          {item.fullName} - DNI {item.dni}
                        </p>
                        <span className={`rounded-full border px-3 py-1 text-xs font-semibold uppercase tracking-wide ${statusClass}`}>
                          {item.decision}
                        </span>
                      </div>
                      <div className="grid gap-1 text-slate-600 sm:grid-cols-2 lg:grid-cols-3">
                        <p>Monto solicitado: S/ {item.amountRequested}</p>
                        <p>Ingreso mensual: S/ {item.monthlyIncome}</p>
                        <p>Fecha: {new Date(item.createdAtUtc).toLocaleString()}</p>
                        <p className="sm:col-span-2 lg:col-span-3">
                          Justificacion: {item.justification}
                        </p>
                      </div>
                    </div>
                  )
                })}
              </div>
            </article>
          </section>
        )}

        {activeTab === 'docs' && (
          <section className="grid gap-6">
            <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-200/40">
              <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <h2 className="flex items-center gap-2 font-display text-2xl text-slate-900">
                  <SquareLibrary className="text-teal-700" />
                  Documentacion del proyecto
                </h2>
                <div className="flex flex-wrap gap-2">
                  {docsFiles.map((doc) => (
                    <button
                      key={doc.path}
                      type="button"
                      onClick={() => {
                        setSelectedDoc(doc)
                        loadDoc(doc)
                      }}
                      className={`rounded-xl border px-3 py-2 text-xs font-semibold transition ${
                        selectedDoc.path === doc.path
                          ? 'border-teal-300 bg-teal-50 text-teal-900'
                          : 'border-slate-300 text-slate-700 hover:bg-slate-50'
                      }`}
                    >
                      {doc.label}
                    </button>
                  ))}
                  <a
                    href={`${apiBase}/swagger/index.html`}
                    target="_blank"
                    rel="noreferrer"
                    className="rounded-xl border border-slate-300 px-3 py-2 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
                  >
                    Swagger
                  </a>
                </div>
              </div>

              {loadingDoc && (
                <div className="flex items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                  <Loader2 size={16} className="animate-spin" />
                  Cargando documento...
                </div>
              )}

              {docError && (
                <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
                  {docError}
                </div>
              )}

              {!loadingDoc && !docError && docContent && <MarkdownViewer content={docContent} />}
            </article>
          </section>
        )}
      </div>
    </main>
  )
}

function TabButton({ active, icon: Icon, title, subtitle, onClick }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-xl border px-4 py-3 text-left transition ${
        active
          ? 'border-teal-300 bg-teal-50 text-teal-900'
          : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50'
      }`}
    >
      <div className="mb-1 flex items-center gap-2 font-semibold">
        <Icon size={16} />
        {title}
      </div>
      <p className="text-xs opacity-80">{subtitle}</p>
    </button>
  )
}

function Field({ label, value, onChange, placeholder, disabled = false, onBlur, onKeyDown }) {
  return (
    <label className="text-sm font-semibold text-slate-700">
      {label}
      <input
        className="mt-1 block w-full rounded-xl border border-slate-300 bg-slate-50 px-3 py-2 font-normal outline-none ring-teal-500 transition focus:bg-white focus:ring"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        onBlur={onBlur}
        onKeyDown={onKeyDown}
        placeholder={placeholder}
        disabled={disabled}
      />
    </label>
  )
}

function BureauCard({ title, lines }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-slate-50 p-3">
      <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">{title}</p>
      {lines.map((line) => (
        <p key={line} className="text-sm text-slate-700">
          {line}
        </p>
      ))}
    </div>
  )
}

function DistributionChart({ items }) {
  const statusOrder = ['Approved', 'Observed', 'Rejected']
  const [mode, setMode] = useState('cases')

  const baseItems = statusOrder.map((status) => {
    const source = items.find((item) => item.status === status)
    return {
      status,
      count: source?.count ?? 0,
      totalAmount: source?.totalAmount ?? 0,
      casePercentage: Number(source?.percentage ?? 0),
      color: decisionChartPalette[status],
    }
  })

  const totalCases = baseItems.reduce((acc, item) => acc + item.count, 0)
  const totalAmount = baseItems.reduce((acc, item) => acc + item.totalAmount, 0)

  const normalized = baseItems.map((item) => ({
    ...item,
    amountPercentage: totalAmount === 0 ? 0 : (item.totalAmount * 100) / totalAmount,
  }))

  const getPercentage = (item) => (mode === 'cases' ? item.casePercentage : item.amountPercentage)

  let cumulative = 0
  const segments = normalized
    .map((item) => {
      const start = cumulative
      const end = start + getPercentage(item)
      cumulative = end
      return `${item.color} ${start}% ${end}%`
    })
    .join(', ')

  const pieBackground = (mode === 'cases' ? totalCases : totalAmount)
    ? `conic-gradient(${segments})`
    : 'conic-gradient(#e2e8f0 0% 100%)'

  return (
    <div className="rounded-2xl border border-slate-200 bg-slate-50/70 p-4">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <p className="font-semibold text-slate-700">Distribucion por estado</p>
        <div className="inline-flex rounded-lg border border-slate-200 bg-white p-1 text-xs font-semibold">
          <button
            type="button"
            onClick={() => setMode('cases')}
            className={`rounded-md px-2.5 py-1 transition ${
              mode === 'cases' ? 'bg-slate-900 text-white' : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            Por casos
          </button>
          <button
            type="button"
            onClick={() => setMode('amount')}
            className={`rounded-md px-2.5 py-1 transition ${
              mode === 'amount' ? 'bg-slate-900 text-white' : 'text-slate-600 hover:bg-slate-100'
            }`}
          >
            Por monto
          </button>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-[180px_1fr] md:items-center">
        <div className="mx-auto flex h-40 w-40 items-center justify-center rounded-full" style={{ background: pieBackground }}>
          <div className="flex h-24 w-24 flex-col items-center justify-center rounded-full bg-white text-center shadow-sm">
            <span className="text-[10px] font-semibold uppercase tracking-wide text-slate-500">Total</span>
            <span className="text-xl font-bold text-slate-900">
              {mode === 'cases' ? totalCases : formatCurrency(totalAmount)}
            </span>
            <span className="text-[10px] text-slate-500">{mode === 'cases' ? 'casos' : 'monto'}</span>
          </div>
        </div>

        <div className="space-y-3">
          {normalized.map((item) => (
            <div key={item.status} className="rounded-xl border border-slate-200 bg-white p-3">
              <div className="mb-2 flex items-center justify-between gap-2">
                <p className="flex items-center gap-2 font-semibold text-slate-900">
                  <span className="inline-block h-2.5 w-2.5 rounded-full" style={{ backgroundColor: item.color }} />
                  {item.status}
                </p>
                <p className="text-xs font-semibold text-slate-600">{getPercentage(item).toFixed(2)}%</p>
              </div>

              <div className="mb-2 h-2 rounded-full bg-slate-100">
                <div
                  className="h-2 rounded-full transition-all"
                  style={{ width: `${Math.max(0, Math.min(100, getPercentage(item)))}%`, backgroundColor: item.color }}
                />
              </div>

              <p className="text-xs text-slate-600">
                {item.count} casos - {formatCurrency(item.totalAmount)}
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

function formatCurrency(value) {
  const numeric = Number(value ?? 0)
  return `S/ ${new Intl.NumberFormat('es-PE', { maximumFractionDigits: 2 }).format(numeric)}`
}

function Metric({ label, value }) {
  return (
    <div className="flex items-center justify-between rounded-xl border border-slate-200 px-3 py-2">
      <span className="text-slate-600">{label}</span>
      <strong className="text-slate-900">{value}</strong>
    </div>
  )
}

function MermaidDiagram({ chart }) {
  const containerRef = useRef(null)
  const [renderError, setRenderError] = useState('')

  useEffect(() => {
    let isActive = true

    async function renderDiagram() {
      if (!containerRef.current) {
        return
      }

      setRenderError('')

      try {
        const mermaid = await getMermaid()
        const renderId = `mermaid-${Math.random().toString(36).slice(2)}`
        const { svg } = await mermaid.render(renderId, chart)

        if (isActive && containerRef.current) {
          containerRef.current.innerHTML = svg
        }
      } catch (error) {
        if (!isActive) {
          return
        }

        containerRef.current.innerHTML = ''
        setRenderError('No se pudo renderizar el diagrama Mermaid.')
      }
    }

    renderDiagram()

    return () => {
      isActive = false
      if (containerRef.current) {
        containerRef.current.innerHTML = ''
      }
    }
  }, [chart])

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
      {renderError && (
        <p className="mb-3 text-sm font-semibold text-rose-700">{renderError}</p>
      )}
      <div ref={containerRef} className="overflow-x-auto text-center" />
    </div>
  )
}

function MarkdownViewer({ content }) {
  return (
    <div className="max-h-[70vh] overflow-auto rounded-2xl border border-slate-200 bg-slate-50 p-5 text-sm">
      <div className="space-y-4 text-slate-800">
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          components={{
            h1: ({ children }) => <h1 className="text-3xl font-bold text-slate-900">{children}</h1>,
            h2: ({ children }) => <h2 className="pt-2 text-2xl font-semibold text-slate-900">{children}</h2>,
            h3: ({ children }) => <h3 className="pt-1 text-xl font-semibold text-slate-900">{children}</h3>,
            p: ({ children }) => <p className="leading-relaxed text-slate-700">{children}</p>,
            ul: ({ children }) => <ul className="list-disc space-y-1 pl-5 text-slate-700">{children}</ul>,
            ol: ({ children }) => <ol className="list-decimal space-y-1 pl-5 text-slate-700">{children}</ol>,
            li: ({ children }) => <li>{children}</li>,
            code: ({ inline, className, children }) => {
              const language = className?.replace('language-', '')
              const value = String(children).replace(/\n$/, '')

              if (!inline && language === 'mermaid') {
                return <MermaidDiagram chart={value} />
              }

              if (inline) {
                return <code className="rounded bg-slate-200 px-1.5 py-0.5 text-xs text-slate-900">{children}</code>
              }

              return (
                <pre className="overflow-x-auto rounded-xl bg-slate-900 p-4 text-xs text-slate-100">
                  <code>{children}</code>
                </pre>
              )
            },
            pre: ({ children }) => <>{children}</>,
            table: ({ children }) => (
              <div className="overflow-x-auto">
                <table className="w-full border-collapse border border-slate-300 text-left text-xs">{children}</table>
              </div>
            ),
            th: ({ children }) => <th className="border border-slate-300 bg-slate-200 px-2 py-1.5">{children}</th>,
            td: ({ children }) => <td className="border border-slate-300 bg-white px-2 py-1.5">{children}</td>,
            blockquote: ({ children }) => (
              <blockquote className="border-l-4 border-teal-400 bg-teal-50 px-3 py-2 text-slate-700">{children}</blockquote>
            ),
            a: ({ href, children }) => (
              <a href={href} target="_blank" rel="noreferrer" className="font-semibold text-teal-700 underline">
                {children}
              </a>
            ),
          }}
        >
          {content}
        </ReactMarkdown>
      </div>
    </div>
  )
}

export default App
