import type { Policy, PolicyPayload } from "../types";

const baseUrl = "/api/policies";

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: {
      "Content-Type": "application/json",
      ...options?.headers
    },
    ...options
  });

  if (!response.ok) {
    const message = await readError(response);
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

async function readError(response: Response): Promise<string> {
  const fallback = "Nao foi possivel concluir a operacao.";

  try {
    const body = await response.json();
    if (body?.errors) {
      return Object.values(body.errors).flat().join(" ");
    }

    if (body?.title) {
      return body.title;
    }
  } catch {
    return fallback;
  }

  return fallback;
}

export const policiesApi = {
  list: () => request<Policy[]>(baseUrl),
  listExpiringSoon: () => request<Policy[]>(`${baseUrl}/expiring-soon`),
  create: (payload: PolicyPayload) =>
    request<Policy>(baseUrl, {
      method: "POST",
      body: JSON.stringify(payload)
    }),
  update: (id: string, payload: PolicyPayload) =>
    request<void>(`${baseUrl}/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload)
    }),
  remove: (id: string) =>
    request<void>(`${baseUrl}/${id}`, {
      method: "DELETE"
    })
};
