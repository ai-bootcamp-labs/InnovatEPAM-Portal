/**
 * Lightweight typed fetch wrapper used by every feature hook (T070).
 *
 * - Reads the API base URL from `import.meta.env.VITE_API_BASE_URL`
 *   (defaults to `http://localhost:5000/api/v1`).
 * - Lazily attaches the bearer token from `localStorage` so the client doesn't
 *   need to import the AuthProvider (avoids a render dependency).
 * - Surfaces RFC 7807 ProblemDetails responses as typed `ApiError`s.
 */

const DEFAULT_BASE_URL = 'http://localhost:5000/api/v1';
const TOKEN_STORAGE_KEY = 'innovatepam.auth.v1';

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;
  constructor(status: number, problem: ProblemDetails) {
    super(problem.title ?? problem.detail ?? `Request failed (${status})`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

function readStoredToken(): string | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = window.localStorage.getItem(TOKEN_STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as { token?: string } | null;
    return parsed?.token ?? null;
  } catch {
    return null;
  }
}

function baseUrl(): string {
  const fromEnv = (import.meta as unknown as { env?: Record<string, string> }).env?.VITE_API_BASE_URL;
  return (fromEnv && fromEnv.trim().length > 0 ? fromEnv : DEFAULT_BASE_URL).replace(/\/+$/, '');
}

function buildUrl(path: string): string {
  const cleanPath = path.startsWith('/') ? path : `/${path}`;
  return `${baseUrl()}${cleanPath}`;
}

async function parseError(response: Response): Promise<ApiError> {
  let body: ProblemDetails = { status: response.status, title: response.statusText };
  try {
    const text = await response.text();
    if (text) {
      const parsed = JSON.parse(text);
      body = { status: response.status, ...parsed };
    }
  } catch {
    // ignore — keep the default body
  }
  return new ApiError(response.status, body);
}

async function request<T>(
  method: string,
  path: string,
  options: { body?: unknown; signal?: AbortSignal; headers?: Record<string, string> } = {},
): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json', ...(options.headers ?? {}) };
  const token = readStoredToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  let body: BodyInit | undefined;
  if (options.body instanceof FormData) {
    body = options.body;
  } else if (options.body !== undefined) {
    headers['Content-Type'] = headers['Content-Type'] ?? 'application/json';
    body = JSON.stringify(options.body);
  }

  const response = await fetch(buildUrl(path), { method, headers, body, signal: options.signal });
  if (!response.ok) throw await parseError(response);
  if (response.status === 204) return undefined as T;
  const ct = response.headers.get('Content-Type') ?? '';
  if (ct.includes('application/json')) return (await response.json()) as T;
  return (await response.text()) as unknown as T;
}

export const apiClient = {
  get: <T>(path: string, signal?: AbortSignal) => request<T>('GET', path, { signal }),
  post: <T>(path: string, body?: unknown, signal?: AbortSignal) => request<T>('POST', path, { body, signal }),
  put: <T>(path: string, body?: unknown, signal?: AbortSignal) => request<T>('PUT', path, { body, signal }),
  del: <T>(path: string, signal?: AbortSignal) => request<T>('DELETE', path, { signal }),
  /** Streams a binary file (used for attachment downloads). */
  fetchBlob: async (path: string, signal?: AbortSignal): Promise<{ blob: Blob; fileName: string | null }> => {
    const headers: Record<string, string> = {};
    const token = readStoredToken();
    if (token) headers.Authorization = `Bearer ${token}`;
    const response = await fetch(buildUrl(path), { method: 'GET', headers, signal });
    if (!response.ok) throw await parseError(response);
    const fileName = parseFileName(response.headers.get('Content-Disposition'));
    return { blob: await response.blob(), fileName };
  },
};

function parseFileName(disposition: string | null): string | null {
  if (!disposition) return null;
  const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(disposition);
  if (!match) return null;
  return decodeURIComponent(match[1] ?? match[2] ?? '').trim() || null;
}

export const API_BASE_URL = baseUrl();
