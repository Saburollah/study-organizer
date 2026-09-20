import { expect, test, type Page } from '@playwright/test'

const password = 'GoldenPath!Password123'
const courseUrl = 'https://mock-moodle.local/course/view.php?id=se-2026'

test('Superpowers: zwei Konten registrieren, scannen und behalten eigene Aufgaben', async ({ page }) => {
  await registerAndSignIn(page, 'superpowers-a@example.test')
  await registerAndScan(page)
  await expect(page.locator('.course-card')).toHaveCount(1)
  await expect(page.locator('.content-card')).toHaveCount(2)
  await expect(page.locator('.content-card').filter({ hasText: 'Exercise 1' })).toContainText(
    'Aufgabe erstellt',
  )
  await expectTask(page, 'Exercise 1')

  await page.goto('/moodle-courses')
  await page.getByRole('button', { name: 'Jetzt scannen' }).click()
  await expect(page.locator('.scan-summary')).toBeVisible()
  await expect(page.locator('.content-card')).toHaveCount(2)
  await expectTask(page, 'Exercise 1')

  await signOut(page)
  await registerAndSignIn(page, 'superpowers-b@example.test')
  await registerAndScan(page)
  await expect(page.locator('.course-card')).toHaveCount(1)
  await expectTask(page, 'Exercise 1')

  await signOut(page)
  await signIn(page, 'superpowers-a@example.test')
  await page.goto('/moodle-courses')
  await expect(page.locator('.course-card')).toHaveCount(1)
  await expectTask(page, 'Exercise 1')
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

async function registerAndScan(page: Page): Promise<void> {
  await page.goto('/moodle-courses')
  await page.getByLabel('Moodle-Kurslink').fill(courseUrl)
  await page.getByRole('button', { name: 'Kurs registrieren' }).click()
  await expect(page.locator('.course-card')).toContainText('Software Engineering')
  await page.getByRole('button', { name: 'Jetzt scannen' }).click()
  await expect(page.locator('.scan-summary')).toBeVisible()
  await expect(page.locator('.content-card')).toHaveCount(2)
}

async function expectTask(page: Page, title: string): Promise<void> {
  await page.goto('/moodle-courses')
  await page.getByRole('link', { name: 'Persönliches Modul öffnen' }).click()
  await expect(page.locator('.task-card')).toContainText(title)
  await expect(page.locator('.task-card')).toHaveCount(1)
}
