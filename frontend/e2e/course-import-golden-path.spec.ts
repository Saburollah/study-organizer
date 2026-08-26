import { expect, test, type Page } from '@playwright/test'

const password = 'GoldenPath!Password123'
const canonicalCourseUrl = 'https://example.test/mock-moodle/course/software-engineering'
const equivalentCourseUrl =
  'https://EXAMPLE.test/mock-moodle/course/software-engineering/?subscriber=b#overview'

const initialTaskTitles = [
  'software engineering PDF',
  'software engineering reference',
  'software engineering activity',
] as const
const addedTaskTitle = 'software engineering project brief'

test('zwei Abonnenten erhalten neue Kursinhalte ohne Duplikate', async ({ page }) => {
  await registerAndSignIn(page, 'course-import-a@example.test')
  await createModule(page, 'Golden Path A')
  await connectCourse(page, 'Golden Path A', canonicalCourseUrl)
  await expectActiveSubscriptionWithTasks(page, initialTaskTitles)

  await signOut(page)
  await registerAndSignIn(page, 'course-import-b@example.test')
  await createModule(page, 'Golden Path B')
  await connectCourse(page, 'Golden Path B', equivalentCourseUrl)

  // Subscriber B must reuse A's current snapshot. If registration fetched the
  // adapter again, the deterministic mock would already expose the fourth item.
  await expectActiveSubscriptionWithTasks(page, initialTaskTitles)
  await expect(page.getByRole('heading', { name: addedTaskTitle })).toHaveCount(0)

  await signOut(page)
  await signIn(page, 'course-import-a@example.test')
  await openModuleTasks(page, 'Golden Path A')
  await startScanAndAwaitSummary(page, '1 neue Aufgaben · 1 neue Inhalte · 0 aktualisiert')
  await expectTaskTitles(page, [...initialTaskTitles, addedTaskTitle])

  await signOut(page)
  await signIn(page, 'course-import-b@example.test')
  await openModuleTasks(page, 'Golden Path B')
  await expectTaskTitles(page, [...initialTaskTitles, addedTaskTitle])

  await signOut(page)
  await signIn(page, 'course-import-a@example.test')
  await openModuleTasks(page, 'Golden Path A')

  const completePdfButton = page.getByRole('button', {
    name: `${initialTaskTitles[0]} als erledigt markieren`,
  })
  await completePdfButton.click()

  const reopenPdfButton = page.getByRole('button', {
    name: `${initialTaskTitles[0]} wieder öffnen`,
  })
  await expect(reopenPdfButton).toHaveAttribute('aria-pressed', 'true')

  await startScanAndAwaitSummary(page, '0 neue Aufgaben · 0 neue Inhalte · 0 aktualisiert')

  await expectTaskTitles(page, [...initialTaskTitles, addedTaskTitle])
  await expect(reopenPdfButton).toHaveAttribute('aria-pressed', 'true')
})

async function registerAndSignIn(page: Page, email: string): Promise<void> {
  await page.goto('/register')
  await page.getByLabel('E-Mail-Adresse').fill(email)
  await page.getByLabel('Passwort', { exact: true }).fill(password)
  await page.getByLabel('Passwort bestätigen').fill(password)
  await page.getByRole('button', { name: 'Konto erstellen' }).click()
  await expect(page.getByRole('status')).toContainText(`${email} wurde erfolgreich registriert.`)

  await page.getByRole('link', { name: 'Anmelden', exact: true }).click()
  await signIn(page, email)
}

async function signIn(page: Page, email: string): Promise<void> {
  await page.goto('/login')
  await page.getByLabel('E-Mail-Adresse').fill(email)
  await page.getByLabel('Passwort', { exact: true }).fill(password)
  await page.getByRole('button', { name: 'Anmelden', exact: true }).click()
  await expect(page).toHaveURL(/\/dashboard$/)
}

async function signOut(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Abmelden' }).click()
  await expect(page).toHaveURL(/\/login$/)
}

async function createModule(page: Page, moduleName: string): Promise<void> {
  await page.getByRole('link', { name: 'Lernmodule', exact: true }).click()
  await page.getByRole('button', { name: 'Neues Lernmodul' }).click()
  await page.getByLabel('Name *', { exact: true }).fill(moduleName)
  await page.getByRole('button', { name: 'Lernmodul speichern' }).click()
  await expect(
    page
      .getByRole('status')
      .filter({ hasText: 'Das Lernmodul wurde erfolgreich erstellt.' }),
  ).toBeVisible()
}

async function connectCourse(page: Page, moduleName: string, courseUrl: string): Promise<void> {
  await page.getByRole('button', { name: 'Mock-Kurs verbinden' }).click()
  await page.getByLabel('Kurslink').fill(courseUrl)
  await page.getByRole('button', { name: 'Link prüfen' }).click()
  await page.getByRole('radio', { name: new RegExp(moduleName) }).check()
  await page.getByRole('button', { name: 'Weiter' }).click()
  await page.getByRole('button', { name: 'Verbinden und Scan starten' }).click()
  await expect(page).toHaveURL(/\/modules\/[^/]+\/tasks$/)
}

async function openModuleTasks(page: Page, moduleName: string): Promise<void> {
  await page.getByRole('link', { name: 'Lernmodule', exact: true }).click()
  const moduleCard = page.locator('.module-card').filter({ hasText: moduleName })
  await moduleCard.getByRole('link', { name: 'Aufgaben' }).click()
  await expect(page.getByRole('heading', { name: moduleName })).toBeVisible()
}

async function expectActiveSubscriptionWithTasks(
  page: Page,
  taskTitles: readonly string[],
): Promise<void> {
  await expect(page.locator('.subscription-status')).toHaveText('Aktiv')
  await expectTaskTitles(page, taskTitles)
}

async function expectTaskTitles(page: Page, taskTitles: readonly string[]): Promise<void> {
  await expect(page.locator('.task-card')).toHaveCount(taskTitles.length)
  for (const title of taskTitles) {
    await expect(page.getByRole('heading', { name: title })).toBeVisible()
  }
}

async function startScanAndAwaitSummary(page: Page, summary: string): Promise<void> {
  const scanHistory = page.locator('.scan-history-item')
  const previousScanCount = await scanHistory.count()

  await page.getByRole('button', { name: 'Scan starten' }).click()

  await expect(scanHistory).toHaveCount(previousScanCount + 1)
  await expect(page.locator('.latest-scan-result')).toContainText(summary)
}
