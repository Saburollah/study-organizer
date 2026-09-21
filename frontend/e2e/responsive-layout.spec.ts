import { expect, test, type Page } from '@playwright/test'

const moduleFixture = {
  id: 'layout-module',
  name: 'Mathematik und Software Engineering',
  code: 'SE-101',
  description: 'Ein Lernmodul mit Aufgaben für die nächste Woche.',
  color: '#0c66e4',
  createdAtUtc: '2026-01-01T00:00:00Z',
  isExternalCourseLinked: false,
}

const taskFixture = {
  id: 'layout-task',
  moduleId: 'layout-module',
  title: 'Architekturdiagramm vorbereiten und Aufgaben besprechen',
  description: null,
  dueDateUtc: '2099-01-01T18:00:00Z',
  status: 'Open',
  createdAtUtc: '2026-01-01T00:00:00Z',
  updatedAtUtc: null,
  externalSource: null,
}

async function preparePage(page: Page, locale: 'de' | 'en', authenticated: boolean) {
  await page.addInitScript(
    ({ locale, authenticated }) => {
      localStorage.setItem('study-organizer.locale', locale)
      if (authenticated) {
        sessionStorage.setItem(
          'study-organizer.auth-session',
          JSON.stringify({
            email: 'layout@example.test',
            accessToken: 'layout-test-token',
            expiresAtUtc: '2099-01-01T00:00:00Z',
          }),
        )
      }
    },
    { locale, authenticated },
  )

  // Only the external HTTP boundary is replaced; Vue, routing and CSS are real.
  await page.route('**/api/modules/', async (route) => {
    await route.fulfill({ json: [moduleFixture] })
  })
  await page.route('**/api/modules/layout-module/tasks/', async (route) => {
    await route.fulfill({ json: [taskFixture] })
  })
  await page.route('**/api/course-subscriptions', async (route) => {
    await route.fulfill({
      json: [
        {
          id: 'layout-subscription',
          moduleId: moduleFixture.id,
          courseName: 'Software Engineering: Architektur und Implementierung',
          providerKey: 'mock-moodle',
          externalCourseId: 'layout-course',
          lastScanStatus: 'Succeeded',
          lastSuccessfulScanAtUtc: '2026-01-01T00:00:00Z',
        },
      ],
    })
  })
  await page.route('**/api/course-subscriptions/layout-subscription/contents', async (route) => {
    await route.fulfill({
      json: [
        {
          id: 'layout-content',
          providerContentId: 'layout-assignment',
          title: 'Architekturdiagramm vorbereiten',
          description: null,
          sourceUrl: 'https://moodle.example.test/assignment/1',
          dueDateUtc: taskFixture.dueDateUtc,
          status: 'TaskCreated',
          reviewReason: null,
          taskId: taskFixture.id,
        },
        {
          id: 'layout-announcement',
          providerContentId: 'layout-announcement',
          title: 'Announcement 1',
          description: null,
          sourceUrl: 'https://moodle.example.test/announcement/1',
          dueDateUtc: null,
          status: 'ReviewRequired',
          reviewReason: 'NotAnAssignment',
          taskId: null,
        },
      ],
    })
  })
}

async function expectNoHorizontalOverflow(page: Page) {
  await expect
    .poll(() =>
      page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      ),
    )
    .toBeLessThanOrEqual(1)
}

for (const width of [320, 375, 390, 768, 1024, 1025, 1200, 1201, 1440]) {
  for (const locale of ['de', 'en'] as const) {
    test(`authenticated pages fit ${width}px in ${locale}`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 })
      await preparePage(page, locale, true)
      for (const path of ['/', '/dashboard', '/modules', '/moodle-courses']) {
        await page.goto(path)
        await expect(page.locator('main')).toBeVisible()
        if (path === '/dashboard') {
          await expect(page.locator('.summary-card')).toHaveCount(4)
          await expect(page.locator('.task-row')).toContainText(taskFixture.title)
        } else if (path === '/modules') {
          await expect(page.locator('.module-card')).toContainText(moduleFixture.name)
        } else if (path === '/moodle-courses') {
          await expect(page.locator('.course-card')).toContainText(
            'Software Engineering: Architektur',
          )
          await expect(page.locator('.content-card').first()).toContainText(
            'Architekturdiagramm vorbereiten',
          )
          await expect(page.locator('.content-list')).not.toContainText(
            'externalCourses.reviewReasons.NotAnAssignment',
          )
          await expect(page.locator('.content-list')).toContainText(
            locale === 'de'
              ? 'Dieser Inhalt ist keine Aufgabe'
              : 'This content is not an assignment',
          )
        }
        await expectNoHorizontalOverflow(page)
      }
      const toggle = page.getByRole('button', {
        name: locale === 'de' ? 'Menü öffnen' : 'Open menu',
      })
      if (width <= 1200) {
        await expect
          .poll(() =>
            page.evaluate(() => {
              const header = document.querySelector('.app-header')
              const toggle = document.querySelector('.menu-toggle')
              if (!(header instanceof HTMLElement) || !(toggle instanceof HTMLElement)) {
                return Number.POSITIVE_INFINITY
              }

              return Math.round(
                header.getBoundingClientRect().right - toggle.getBoundingClientRect().right,
              )
            }),
          )
          .toBeLessThanOrEqual(17)
        await toggle.click()
        await expect(
          page.getByRole('button', { name: locale === 'de' ? 'Abmelden' : 'Sign out' }),
        ).toBeVisible()
        await expect(
          page.getByRole('link', {
            name: locale === 'de' ? 'Moodle-Kurse' : 'Moodle courses',
            exact: true,
          }),
        ).toBeVisible()
        await expectNoHorizontalOverflow(page)
      } else {
        await expect(toggle).not.toBeVisible()
        await expect(
          page.getByRole('link', { name: locale === 'de' ? 'Profil' : 'Profile', exact: true }),
        ).toBeVisible()
      }
    })

    test(`signed-out navigation fits ${width}px in ${locale}`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 })
      await preparePage(page, locale, false)
      await page.goto('/')
      await expectNoHorizontalOverflow(page)
      if (width <= 1200) {
        await page
          .getByRole('button', { name: locale === 'de' ? 'Menü öffnen' : 'Open menu' })
          .click()
      }
      await expect(
        page.getByRole('link', { name: locale === 'de' ? 'Anmelden' : 'Sign in', exact: true }),
      ).toBeVisible()
      await expect(
        page.getByRole('link', { name: locale === 'de' ? 'Registrieren' : 'Sign up', exact: true }),
      ).toBeVisible()
      await expectNoHorizontalOverflow(page)
    })
  }
}

test('compact menu is keyboard accessible and closes after navigation', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 900 })
  await preparePage(page, 'de', true)
  await page.goto('/')
  const toggle = page.getByRole('button', { name: 'Menü öffnen' })
  await expect(toggle).toBeVisible()
  await toggle.focus()
  await page.keyboard.press('Enter')
  await expect(page.getByRole('button', { name: 'Menü schließen' })).toHaveAttribute(
    'aria-expanded',
    'true',
  )
  await expect(page.getByRole('link', { name: 'Lernmodule', exact: true })).toBeVisible()
  await expectNoHorizontalOverflow(page)
  await page.keyboard.press('Escape')
  await expect(toggle).toBeFocused()
  await expect(toggle).toHaveAttribute('aria-expanded', 'false')
  await expect(page.getByRole('link', { name: 'Lernmodule', exact: true })).not.toBeVisible()
  await toggle.click()
  await page.getByRole('link', { name: 'Lernmodule', exact: true }).click()
  await expect(page).toHaveURL(/\/modules$/)
  await expect(toggle).toHaveAttribute('aria-expanded', 'false')
})

test('language switching and sign out work in the compact menu', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 900 })
  await preparePage(page, 'de', true)
  await page.goto('/')
  await page.getByRole('button', { name: 'Menü öffnen' }).click()
  await page.getByRole('button', { name: 'Englisch', exact: true }).click()
  await expect(page.locator('html')).toHaveAttribute('lang', 'en')
  await expect(page.getByRole('button', { name: 'Close menu' })).toBeVisible()
  await expectNoHorizontalOverflow(page)
  await page.getByRole('button', { name: 'Sign out', exact: true }).click()
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('button', { name: 'Open menu' })).toHaveAttribute(
    'aria-expanded',
    'false',
  )
  await expect(page.getByRole('button', { name: 'Sign out', exact: true })).toHaveCount(0)
})

test('resizing to desktop does not reopen a previous compact menu', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 900 })
  await preparePage(page, 'en', true)
  await page.goto('/')
  await page.getByRole('button', { name: 'Open menu' }).click()
  await page.setViewportSize({ width: 1440, height: 900 })
  await expect(page.locator('.menu-toggle')).toHaveAttribute('aria-expanded', 'false')
  await expect(page.getByRole('button', { name: 'Close menu' })).not.toBeVisible()
  await expect(page.getByRole('button', { name: 'Sign out' })).toBeVisible()
  await page.setViewportSize({ width: 375, height: 900 })
  await expect(page.getByRole('button', { name: 'Open menu' })).toHaveAttribute(
    'aria-expanded',
    'false',
  )
  await expect(page.getByRole('button', { name: 'Sign out' })).not.toBeVisible()
})

test('German course actions fit 320px with larger action text', async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 900 })
  await preparePage(page, 'de', true)
  await page.goto('/moodle-courses')
  await expect(page.locator('.course-card')).toContainText('Software Engineering: Architektur')
  await page.addStyleTag({ content: '.course-actions { font-size: 18px; }' })
  await expectNoHorizontalOverflow(page)
})
