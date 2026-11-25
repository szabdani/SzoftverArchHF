import { expect, test } from '@playwright/test';
import { loginAs } from './auth';

/**
 * Admin és edző folyamatok (UC18, UC14): az admin létrehozza a demóadatokat, majd a demó edzőnél
 * megjelennek a futók kockázattal. A tesztek sorban futnak (workers: 1), így a demóadatok a következő
 * ellenőrzéshez rendelkezésre állnak.
 */
test.describe('Admin és edző', () => {
  test('az admin a felhasználókezelő oldalt látja, és létrehozza a demóadatokat', async ({ page }) => {
    await loginAs(page, 'admin@futoedzes.local', '/admin/users');

    await expect(page.getByRole('heading', { name: 'Felhasználók' })).toBeVisible();
    // Az admin a saját menüjét látja, a futóét nem.
    await expect(page.getByRole('link', { name: 'Szabályok' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Edzésnapló' })).toHaveCount(0);

    await page.getByRole('button', { name: 'Demóadatok létrehozása' }).click();

    // A létrehozás után megjelenik a szimulált futók listája (pl. a hirtelen terhelésugrásos).
    await expect(page.getByText('demo-spike@futoedzes.local')).toBeVisible({ timeout: 15_000 });
  });

  test('az edző Futóim listájában megjelennek a demó futók kockázattal', async ({ page }) => {
    await loginAs(page, 'coach@futoedzes.local', '/coach');

    await expect(page.getByRole('heading', { name: 'Futóim' })).toBeVisible();

    // A demó edzőhöz kapcsolt futók megjelennek; a terhelésugrásos futó magas kockázatú.
    const spikeRow = page.getByRole('row', { name: /demo-spike/ });
    await expect(spikeRow).toBeVisible({ timeout: 15_000 });
    await expect(spikeRow.getByText('MAGAS')).toBeVisible();
  });
});
