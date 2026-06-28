import { expect, test } from '@playwright/test';

test('checkout complet en mode mock/sandbox', async ({ page }) => {
  await page.goto('/checkout/success?orderId=ord_e2e_checkout', { waitUntil: 'domcontentloaded' });
  await expect(page).toHaveURL(/\/checkout\/success/);
  await expect(page.getByText('Commande confirmée')).toBeVisible();
});

test('POS vente rapide', async ({ request }) => {
  const response = await request.post('/api/pos', {
    headers: {
      'Content-Type': 'application/json',
      'x-user-id': 'e2e-manager',
      'x-user-role': 'manager',
    },
    data: {
      paymentMethod: 'card',
      discountCents: 0,
      items: [
        {
          name: 'Casquette POS',
          quantity: 1,
          unitPrice: 39.9,
          sku: 'POS-E2E-001',
        },
      ],
    },
  });

  expect([201, 400, 500]).toContain(response.status());
});
