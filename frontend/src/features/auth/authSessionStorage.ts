import type { AuthSession } from './authModels'

const storageKey = 'study-organizer.auth-session'

export function saveAuthSession(
  session: AuthSession,
): void {
  sessionStorage.setItem(
    storageKey,
    JSON.stringify(session),
  )
}

export function loadAuthSession(): AuthSession | null {
  const storedValue = sessionStorage.getItem(storageKey)

  if (!storedValue) {
    return null
  }

  try {
    const session = JSON.parse(
      storedValue,
    ) as Partial<AuthSession>

    if (
      typeof session.email !== 'string'
      || typeof session.accessToken !== 'string'
      || typeof session.expiresAtUtc !== 'string'
      || !session.email
      || !session.accessToken
    ) {
      removeAuthSession()
      return null
    }

    const expiresAt = Date.parse(session.expiresAtUtc)

    if (
      !Number.isFinite(expiresAt)
      || expiresAt <= Date.now()
    ) {
      removeAuthSession()
      return null
    }

    return {
      email: session.email,
      accessToken: session.accessToken,
      expiresAtUtc: session.expiresAtUtc,
    }
  } catch {
    removeAuthSession()
    return null
  }
}

export function removeAuthSession(): void {
  sessionStorage.removeItem(storageKey)
}