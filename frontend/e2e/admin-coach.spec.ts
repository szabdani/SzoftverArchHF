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

    // A létrehozás után a „Demóadatok” kártya kilistázza a szimulált futókat a mintájukkal. A teljes,
    // mintával együtt megjelenő sorra illesztünk (pl. „… · hirtelen terhelésugrás”), mert a puszta e-mail
    // cím a felhasználótáblában is szerepel – ott csak a cím, minta nélkül.
    await expect(page.getByText('demo-spike@futoedzes.local · hirtelen terhelésugrás')).toBeVisible({ timeout: 30_000 });
  });

  test('az edző Futóim listájában megjelennek a demó futók kockázattal', async ({ page }) => {
    await loginAs(page, 'coach@futoedzes.local', '/coach');

    await expect(page.getByRole('heading', { name: 'Futóim' })).toBeVisible();

    // A demó edzőhöz kapcsolt futók megjelennek; a terhelésugrásos futó magas kockázatú.
    const spikeRow = page.getByRole('row', { name: /demo-spike/ });
    await expect(spikeRow).toBeVisible({ timeout: 30_000 });

    // A kockázati kategóriát az Analytics eseményvezérelten, aszinkron számolja (Training → RabbitMQ →
    // Analytics), ezért a demóadatok létrejötte után nem jelenik meg azonnal. A kártya a riskScore-t és a
    // kategória magyar címkéjét írja ki egy badge-ben (pl. „18 · Magas”); erre pollozunk a lap frissítésével,
    // amíg az Analytics végez. A badge CSS-sel nagybetűs, de a DOM-szöveg a „Magas” címke.
    await expect(async () => {
      await page.reload();
      await expect(page.getByRole('row', { name: /demo-spike/ }).getByText('Magas')).toBeVisible({ timeout: 5_000 });
    }).toPass({ timeout: 120_000, intervals: [5_000] });
  });
});
