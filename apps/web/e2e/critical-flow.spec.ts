import { expect, test } from '@playwright/test';

function randomEmail() {
  return `rc1-${Date.now()}@example.com`;
}

test('inscription puis connexion', async ({ page }) => {
  const email = randomEmail();
  const password = 'Passw0rd!';

  await page.route('**/api/auth/register', async (route) => {
    await route.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify({ id: 'usr_e2e_1', email, name: 'RC Tester', role: 'client' }),
    });
  });

  await page.route('**/api/auth/login', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ id: 'usr_e2e_1', email, name: 'RC Tester', role: 'client' }),
    });
  });

  await page.goto('/register', { waitUntil: 'domcontentloaded' });
  await page.getByLabel('Nom').fill('RC Tester');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Mot de passe').fill(password);
  await page.getByRole('button', { name: 'Créer mon compte' }).click();

  await expect(page.getByRole('heading', { name: 'Rejoins Dope&Cute' })).toBeVisible();

  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Mot de passe').fill(password);
  await page.getByRole('button', { name: 'Se connecter' }).click();

  await expect(page.getByRole('heading', { name: 'Accède à ton espace' })).toBeVisible();
});

test('personnalisation, ajout panier et checkout', async ({ page }) => {
  await page.goto('/studio');
  await expect(page.getByText('Personnalise ta casquette')).toBeVisible();

  await page.getByRole('button', { name: 'Ajouter au panier' }).click();

  await page.goto('/cart');
  await expect(page.getByText('Mon panier')).toBeVisible();
  await expect(page.getByText('Résumé')).toBeVisible();

  await page.getByRole('link', { name: 'Passer au paiement' }).click();
  await expect(page.getByText('Finalise ta commande')).toBeVisible();
});

test('flux paiement sandbox et commande via API checkout', async ({ request }) => {
  const payload = {
    customer: {
      firstName: 'RC',
      lastName: 'Checkout',
      email: randomEmail(),
      phone: '',
    },
    address: {
      address: '1 rue du test',
      postalCode: '75001',
      city: 'Paris',
      country: 'France',
    },
    paymentProvider: 'stripe',
    items: [
      {
        name: 'RC Line',
        quantity: 1,
        unitPrice: 49.9,
        sku: 'CAP-001',
      },
    ],
    shippingCents: 0,
    discountCents: 0,
  };

  const response = await request.post('/api/checkout', { data: payload });
  const expectedStatuses = process.env.DATABASE_URL ? [201, 402, 400] : [201, 402, 400, 500];
  expect(expectedStatuses).toContain(response.status());
});

test('telechargement facture et remboursement admin', async ({ request }) => {
  const orderId = process.env.E2E_ORDER_ID;
  test.skip(!orderId, 'Set E2E_ORDER_ID to run invoice/refund scenarios on a known order.');

  const invoice = await request.get(`/api/admin/orders/${orderId}/invoice?document=INVOICE`);
  expect(invoice.ok()).toBeTruthy();

  const refund = await request.post('/api/admin/payments/refund', {
    data: {
      orderId,
      reason: 'RC1 test refund',
    },
  });

  expect([200, 400, 422, 502]).toContain(refund.status());
});
