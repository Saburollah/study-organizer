import { defineConfig, devices } from '@playwright/test'

const frontendPort = process.env.E2E_FRONTEND_PORT ?? '5174'
const apiPort = process.env.E2E_API_PORT ?? '5102'
const frontendUrl = `http://127.0.0.1:${frontendPort}`
const apiUrl = `http://127.0.0.1:${apiPort}`

export default defineConfig({
  testDir: './e2e',
  testMatch: 'course-import-golden-path.spec.ts',
  fullyParallel: false,
  workers: 1,
  timeout: 90_000,
  expect: {
    timeout: 10_000,
  },
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: frontendUrl,
    locale: 'de-DE',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../backend/src/Api --no-launch-profile --no-restore',
      url: `${apiUrl}/health`,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: apiUrl,
        Cors__AllowedOrigins__0: frontendUrl,
        'Logging__LogLevel__Microsoft.EntityFrameworkCore': 'Warning',
      },
      reuseExistingServer: false,
      timeout: 120_000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
    {
      command: `pnpm dev --host 127.0.0.1 --port ${frontendPort} --strictPort`,
      url: frontendUrl,
      env: {
        VITE_API_BASE_URL: apiUrl,
      },
      reuseExistingServer: false,
      timeout: 120_000,
      stdout: 'pipe',
      stderr: 'pipe',
    },
  ],
})
