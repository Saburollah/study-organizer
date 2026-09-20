import { defineConfig, devices } from '@playwright/test'

const frontendUrl = 'http://127.0.0.1:5176'

export default defineConfig({
  testDir: './e2e',
  testMatch: 'responsive-layout.spec.ts',
  fullyParallel: true,
  workers: 2,
  timeout: 30_000,
  expect: { timeout: 5_000 },
  reporter: 'list',
  use: {
    baseURL: frontendUrl,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'pnpm dev --host 127.0.0.1 --port 5176 --strictPort',
    url: frontendUrl,
    env: { VITE_API_BASE_URL: frontendUrl },
    reuseExistingServer: false,
  },
})
