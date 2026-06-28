import { expect, test } from '@playwright/test';

test('admin commandes filtre + changement statut', async ({ page }) => {
  let seenFilter = false;
  let updatedStatus: string | null = null;

  await page.route('**/api/admin/orders?*', async (route) => {
    const url = route.request().url();
    seenFilter = url.includes('channel=POS') && url.includes('status=CONFIRMED');

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 'ord_admin_1',
          customer: 'Client Test',
          channel: 'POS',
          createdAt: new Date().toISOString(),
          status: 'CONFIRMED',
          total: '49.90 €',
          items: [
            {
              id: 'line_1',
              name: 'Casquette Test',
              variantName: 'Standard',
              sku: 'CAP-001',
              quantity: 1,
              unitPriceCents: 4990,
              totalPriceCents: 4990,
            },
          ],
        },
      ]),
    });
  });

  await page.route('**/api/admin/orders/ord_admin_1', async (route) => {
    if (route.request().method() === 'PUT') {
      const body = route.request().postDataJSON() as { status?: string };
      updatedStatus = body.status ?? null;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: 'ord_admin_1', status: updatedStatus }),
      });
      return;
    }

    await route.continue();
  });

  await page.goto('/');
  await page.evaluate(async () => {
    await fetch('/api/admin/orders?limit=100&channel=POS&status=CONFIRMED');
  });

  await expect.poll(() => seenFilter).toBeTruthy();

  await page.evaluate(async () => {
    await fetch('/api/admin/orders/ord_admin_1', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ status: 'SHIPPED' }),
    });
  });

  await expect.poll(() => updatedStatus).toBe('SHIPPED');
});
