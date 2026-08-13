import { createI18n } from 'vue-i18n'

import de from './locales/de'
import en from './locales/en'

export const supportedLocales = ['de', 'en'] as const

export type SupportedLocale =
  (typeof supportedLocales)[number]

const localeStorageKey = 'study-organizer.locale'

function isSupportedLocale(
  locale: string | null | undefined,
): locale is SupportedLocale {
  return supportedLocales.includes(
    locale as SupportedLocale,
  )
}

function detectInitialLocale(): SupportedLocale {
  const storedLocale = localStorage.getItem(localeStorageKey)

  if (isSupportedLocale(storedLocale)) {
    return storedLocale
  }

  const browserLocale =
    navigator.languages
      .map((language) => language.split('-')[0]?.toLowerCase())
      .find(isSupportedLocale)

  return browserLocale ?? 'de'
}

const initialLocale = detectInitialLocale()

document.documentElement.lang = initialLocale

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  fallbackLocale: 'de',
  messages: {
    de,
    en,
  },
})

export function setLocale(
  locale: SupportedLocale,
): void {
  i18n.global.locale.value = locale
  localStorage.setItem(localeStorageKey, locale)
  document.documentElement.lang = locale
}
