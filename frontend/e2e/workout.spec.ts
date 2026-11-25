import { expect, test } from '@playwright/test';
import { loginAs } from './auth';

/**
 * Edzésrögzítés és validáció (UC02): a futó rögzít egy edzést, majd az megjelenik az edzésnaplóban;
 * az érvénytelen bevitel a szerverrel közös sémából jövő mezőhibát ad.
 */
test.describe('Edzésrögzítés', () => {
  test('érvényes edzés rögzítése megjelenik az edzésnaplóban', async ({ page }) => {
    await loginAs(page, 'runner@futoedzes.local', '/workouts');

    await page.getByRole('button', { name: 'Új edzés' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();

    // Típus: a Mantine Select megnyitása és egy elem választása.
    await dialog.getByLabel('Típus').click();
    await page.getByRole('option', { name: 'Könnyű' }).click();

    await dialog.getByLabel('Táv (km)').fill('7');

    // Időtartam: a három külön mező (óra / perc / másodperc).
    await dialog.getByLabel('Perc', { exact: true }).fill('42');

    await dialog.getByLabel('RPE (érzett terhelés, 1–10)').fill('5');

    await dialog.getByRole('button', { name: 'Mentés' }).click();

    // A mentés sikeres: az ablak bezárul, és az új edzés (7 km) megjelenik a listában.
    await expect(dialog).toBeHidden();
    await expect(page.getByText('7 km').first()).toBeVisible();
  });

  test('érvénytelen edzés (0 km, jövőbeli dátum) mezőhibát ad', async ({ page }) => {
    await loginAs(page, 'runner@futoedzes.local', '/workouts');

    await page.getByRole('button', { name: 'Új edzés' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();

    await dialog.getByLabel('Típus').click();
    await page.getByRole('option', { name: 'Könnyű' }).click();
    // 0 km és hiányzó időtartam: a mentés nem mehet át, mezőhibának kell megjelennie.
    await dialog.getByLabel('Táv (km)').fill('0');
    await dialog.getByLabel('RPE (érzett terhelés, 1–10)').fill('5');

    await dialog.getByRole('button', { name: 'Mentés' }).click();

    // Az ablak nyitva marad, és megjelenik a táv-mező hibaüzenete (a közös sémából).
    await expect(dialog).toBeVisible();
    await expect(dialog.getByText(/A táv 0,1 és 100 km között lehet/)).toBeVisible();
  });
});
