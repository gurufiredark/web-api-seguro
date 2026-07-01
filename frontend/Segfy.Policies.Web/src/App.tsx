import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  AlertCircle,
  CalendarClock,
  Edit3,
  ListFilter,
  Plus,
  RefreshCcw,
  Save,
  Trash2,
  X
} from "lucide-react";
import { policiesApi } from "./api/policiesApi";
import type { Policy, PolicyPayload, PolicyStatus } from "./types";

const initialForm: PolicyPayload = {
  insuredDocument: "",
  vehiclePlate: "",
  monthlyPremium: 0,
  startDate: "",
  endDate: "",
  status: "Ativa"
};

const statuses: PolicyStatus[] = ["Ativa", "Cancelada", "Expirada"];

const currencyFormatter = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL"
});

export function App() {
  const [policies, setPolicies] = useState<Policy[]>([]);
  const [form, setForm] = useState<PolicyPayload>(initialForm);
  const [editingPolicy, setEditingPolicy] = useState<Policy | null>(null);
  const [showExpiringOnly, setShowExpiringOnly] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const totalPremium = useMemo(
    () => policies.reduce((sum, policy) => sum + policy.monthlyPremium, 0),
    [policies]
  );

  const activeCount = useMemo(
    () => policies.filter((policy) => policy.status === "Ativa").length,
    [policies]
  );

  const documentDigits = onlyDigits(form.insuredDocument);
  const normalizedPlate = normalizePlate(form.vehiclePlate);
  const isDocumentValid = documentDigits.length === 11 || documentDigits.length === 14;
  const isPlateValid = normalizedPlate.length === 7;
  const isDateRangeValid = !form.startDate || !form.endDate || form.endDate >= form.startDate;

  async function loadPolicies(expiringOnly = showExpiringOnly) {
    setLoading(true);
    setError(null);

    try {
      const data = expiringOnly
        ? await policiesApi.listExpiringSoon()
        : await policiesApi.list();
      setPolicies(data);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Erro inesperado.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadPolicies(false);
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setError(null);

    if (!isDocumentValid) {
      setSaving(false);
      setError("Informe um CPF com 11 digitos ou CNPJ com 14 digitos.");
      return;
    }

    if (!isPlateValid) {
      setSaving(false);
      setError("Informe uma placa com 7 caracteres alfanumericos.");
      return;
    }

    if (!isDateRangeValid) {
      setSaving(false);
      setError("A data de termino deve ser maior ou igual a data de inicio.");
      return;
    }

    try {
      const payload = {
        ...form,
        vehiclePlate: normalizedPlate
      };

      if (editingPolicy) {
        await policiesApi.update(editingPolicy.id, payload);
      } else {
        await policiesApi.create(payload);
      }

      setForm(initialForm);
      setEditingPolicy(null);
      await loadPolicies(showExpiringOnly);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Erro inesperado.");
    } finally {
      setSaving(false);
    }
  }

  function startEditing(policy: Policy) {
    setEditingPolicy(policy);
    setForm({
      insuredDocument: formatDocument(policy.insuredDocument),
      vehiclePlate: normalizePlate(policy.vehiclePlate),
      monthlyPremium: policy.monthlyPremium,
      startDate: policy.startDate,
      endDate: policy.endDate,
      status: policy.status
    });
  }

  function cancelEditing() {
    setEditingPolicy(null);
    setForm(initialForm);
  }

  async function removePolicy(policy: Policy) {
    const shouldRemove = window.confirm(`Remover a apolice ${policy.policyNumber}?`);
    if (!shouldRemove) {
      return;
    }

    setError(null);

    try {
      await policiesApi.remove(policy.id);
      await loadPolicies(showExpiringOnly);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Erro inesperado.");
    }
  }

  async function toggleExpiringOnly() {
    const nextValue = !showExpiringOnly;
    setShowExpiringOnly(nextValue);
    await loadPolicies(nextValue);
  }

  return (
    <main className="app-shell">
      <section className="workspace">
        <header className="topbar">
          <div>
            <p className="eyebrow">Segfy Policies</p>
            <h1>Gestao de apolices</h1>
          </div>
          <button className="icon-button" type="button" onClick={() => loadPolicies()}>
            <RefreshCcw size={18} aria-hidden="true" />
            <span>Atualizar</span>
          </button>
        </header>

        <section className="summary-grid" aria-label="Resumo">
          <article>
            <span>Total</span>
            <strong>{policies.length}</strong>
          </article>
          <article>
            <span>Ativas</span>
            <strong>{activeCount}</strong>
          </article>
          <article>
            <span>Premios mensais</span>
            <strong>{currencyFormatter.format(totalPremium)}</strong>
          </article>
        </section>

        {error && (
          <div className="alert" role="alert">
            <AlertCircle size={18} aria-hidden="true" />
            <span>{error}</span>
          </div>
        )}

        <section className="content-grid">
          <form className="policy-form" onSubmit={handleSubmit}>
            <div className="form-heading">
              <div>
                <p className="eyebrow">{editingPolicy ? "Edicao" : "Cadastro"}</p>
                <h2>{editingPolicy ? editingPolicy.policyNumber : "Nova apolice"}</h2>
              </div>
              {editingPolicy && (
                <button className="ghost-button" type="button" onClick={cancelEditing}>
                  <X size={17} aria-hidden="true" />
                  <span>Cancelar</span>
                </button>
              )}
            </div>

            <label>
              CPF/CNPJ
              <input
                value={form.insuredDocument}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    insuredDocument: formatDocument(event.target.value)
                  }))
                }
                placeholder="123.456.789-01"
                inputMode="numeric"
                maxLength={18}
                aria-invalid={form.insuredDocument.length > 0 && !isDocumentValid}
                required
              />
              {form.insuredDocument.length > 0 && !isDocumentValid && (
                <span className="field-hint">Use 11 digitos para CPF ou 14 para CNPJ.</span>
              )}
            </label>

            <label>
              Placa
              <input
                value={form.vehiclePlate}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    vehiclePlate: normalizePlate(event.target.value)
                  }))
                }
                placeholder="ABC1D23"
                maxLength={7}
                aria-invalid={form.vehiclePlate.length > 0 && !isPlateValid}
                required
              />
              {form.vehiclePlate.length > 0 && !isPlateValid && (
                <span className="field-hint">A placa deve ter exatamente 7 caracteres.</span>
              )}
            </label>

            <label>
              Premio mensal
              <input
                type="number"
                min="0.01"
                step="0.01"
                value={form.monthlyPremium || ""}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    monthlyPremium: Number(event.target.value)
                  }))
                }
                required
              />
            </label>

            <div className="date-row">
              <label>
                Inicio
                <input
                  type="date"
                  value={form.startDate}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, startDate: event.target.value }))
                  }
                  required
                />
              </label>

              <label>
                Termino
                <input
                  type="date"
                  value={form.endDate}
                  min={form.startDate || undefined}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, endDate: event.target.value }))
                  }
                  aria-invalid={!isDateRangeValid}
                  required
                />
                {!isDateRangeValid && (
                  <span className="field-hint">
                    A data de termino deve ser maior ou igual a data de inicio.
                  </span>
                )}
              </label>
            </div>

            <label>
              Status
              <select
                value={form.status}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    status: event.target.value as PolicyStatus
                  }))
                }
              >
                {statuses.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </label>

            <button className="primary-button" type="submit" disabled={saving}>
              {editingPolicy ? <Save size={18} /> : <Plus size={18} />}
              <span>{saving ? "Salvando..." : editingPolicy ? "Salvar" : "Cadastrar"}</span>
            </button>
          </form>

          <section className="table-section">
            <div className="table-toolbar">
              <div>
                <p className="eyebrow">Consulta</p>
                <h2>{showExpiringOnly ? "Vencem em 30 dias" : "Todas as apolices"}</h2>
              </div>
              <button
                className={showExpiringOnly ? "filter-button active" : "filter-button"}
                type="button"
                onClick={toggleExpiringOnly}
              >
                {showExpiringOnly ? <ListFilter size={17} /> : <CalendarClock size={17} />}
                <span>{showExpiringOnly ? "Ver todas" : "30 dias"}</span>
              </button>
            </div>

            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Apolice</th>
                    <th>Segurado</th>
                    <th>Placa</th>
                    <th>Premio</th>
                    <th>Vigencia</th>
                    <th>Status</th>
                    <th aria-label="Acoes"></th>
                  </tr>
                </thead>
                <tbody>
                  {loading ? (
                    <tr>
                      <td colSpan={7} className="empty-state">
                        Carregando apolices...
                      </td>
                    </tr>
                  ) : policies.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="empty-state">
                        Nenhuma apolice encontrada.
                      </td>
                    </tr>
                  ) : (
                    policies.map((policy) => (
                      <tr key={policy.id}>
                        <td>
                          <strong>{policy.policyNumber}</strong>
                        </td>
                        <td>{policy.insuredDocument}</td>
                        <td>{policy.vehiclePlate}</td>
                        <td>{currencyFormatter.format(policy.monthlyPremium)}</td>
                        <td>
                          {formatDate(policy.startDate)} ate {formatDate(policy.endDate)}
                        </td>
                        <td>
                          <span className={`status-pill ${policy.status.toLowerCase()}`}>
                            {policy.status}
                          </span>
                        </td>
                        <td>
                          <div className="row-actions">
                            <button
                              className="icon-only"
                              type="button"
                              title="Editar"
                              onClick={() => startEditing(policy)}
                            >
                              <Edit3 size={16} aria-hidden="true" />
                            </button>
                            <button
                              className="icon-only danger"
                              type="button"
                              title="Excluir"
                              onClick={() => removePolicy(policy)}
                            >
                              <Trash2 size={16} aria-hidden="true" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </section>
      </section>
    </main>
  );
}

function formatDate(value: string) {
  if (!value) {
    return "-";
  }

  return new Intl.DateTimeFormat("pt-BR", {
    timeZone: "UTC"
  }).format(new Date(`${value}T00:00:00Z`));
}

function onlyDigits(value: string) {
  return value.replace(/\D/g, "").slice(0, 14);
}

function formatDocument(value: string) {
  const digits = onlyDigits(value);

  if (digits.length <= 11) {
    return digits
      .replace(/^(\d{3})(\d)/, "$1.$2")
      .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
      .replace(/^(\d{3})\.(\d{3})\.(\d{3})(\d)/, "$1.$2.$3-$4")
      .slice(0, 14);
  }

  return digits
    .replace(/^(\d{2})(\d)/, "$1.$2")
    .replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
    .replace(/^(\d{2})\.(\d{3})\.(\d{3})(\d)/, "$1.$2.$3/$4")
    .replace(/^(\d{2})\.(\d{3})\.(\d{3})\/(\d{4})(\d)/, "$1.$2.$3/$4-$5")
    .slice(0, 18);
}

function normalizePlate(value: string) {
  return value.replace(/[^a-zA-Z0-9]/g, "").toUpperCase().slice(0, 7);
}
