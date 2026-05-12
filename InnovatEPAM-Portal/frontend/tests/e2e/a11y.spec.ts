import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

// T111 — accessibility smoke pass against the four primary screens.
// Fails on any Serious or Critical violations. Run with the dev server up:
//   npm run test:e2e -- a11y.spec.ts

const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@innovatepam.test';
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'Adm1n!Pass';
const SEVERITIES = new Set(['serious', 'critical']);

async function login(page) {
  await page.goto('/login');
  await page.getByLabel(/email/i).fill(ADMIN_EMAIL);
  await page.getByLabel(/password/i).fill(ADMIN_PASSWORD);
  await page.getByRole('button', { name: /sign in/i }).click();
  await expect(page).toHaveURL(/\/ideas/);
}

async function assertAccessible(page, label: string) {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa'])
    .analyze();
  const blocking = results.violations.filter((v) => SEVERITIES.has(v.impact ?? ''));
  if (blocking.length > 0) {
    console.error(`a11y violations on ${label}:`, JSON.stringify(blocking, null, 2));
  }
  expect(blocking, `Serious/Critical a11y violations on ${label}`).toEqual([]);
}

test.describe('Accessibility (T111)', () => {
  test('login page has no Serious/Critical violations', async ({ page }) => {
    await page.goto('/login');
    await assertAccessible(page, '/login');
  });

  test('ideas list has no Serious/Critical violations', async ({ page }) => {
    await login(page);
    await page.goto('/ideas');
    await assertAccessible(page, '/ideas');
  });

  test('new idea form has no Serious/Critical violations', async ({ page }) => {
    await login(page);
    await page.goto('/ideas/new');
    await assertAccessible(page, '/ideas/new');
  });

  test('idea detail has no Serious/Critical violations', async ({ page }) => {
    await login(page);
    await page.goto('/ideas');
    const firstLink = page.getByRole('link', { name: /idea/i }).first();
    if (await firstLink.count()) {
      await firstLink.click();
      await assertAccessible(page, '/ideas/:id');
    } else {
      test.skip(true, 'No ideas seeded — detail page cannot be exercised.');
    }
  });
});
