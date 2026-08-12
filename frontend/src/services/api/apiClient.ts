import { environment } from '@/config/environment'

export interface ApiProblem {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem?: ApiProblem,
  ) {
    super(
      problem?.detail
        ?? problem?.title
        ?? `Die Anfrage ist fehlgeschlagen (${status}).`,
    )

    this.name = 'ApiError'
  }
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const baseUrl =
    environment.apiBaseUrl.replace(/\/+$/, '')

  const normalizedPath =
    path.startsWith('/') ? path : `/${path}`

  const headers = new Headers(options.headers)

  if (options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(
    `${baseUrl}${normalizedPath}`,
    {
      ...options,
      headers,
    },
  )

  if (!response.ok) {
    throw new ApiError(
      response.status,
      await readProblem(response),
    )
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

async function readProblem(
  response: Response,
): Promise<ApiProblem | undefined> {
  const contentType =
    response.headers.get('Content-Type')

  if (!contentType?.includes('json')) {
    return undefined
  }

  try {
    return await response.json() as ApiProblem
  } catch {
    return undefined
  }
}