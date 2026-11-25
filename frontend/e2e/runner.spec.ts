import { expect, test } from '@playwright/test';
import { loginAs } from './auth';

/**
 * Futó alapfolyamatok (UC01, UC06): a token-injektálásos bejelentkezés működik, és a futó a saját
 * nézeteit látja (menü, áttekintő oldal).
 */
test.describe('Futó', () => {
  test('bejelentkezés után betölt az áttekintő oldal a futó menüjével', async ({ page }) => {
    await loginAs(page, 'runner@futoedzes.local');

    // A fejlécben a bejelentkezett felhasználó, a főoldal címe és a futó menüpontjai.
    await expect(page.getByText('runner@futoedzes.local')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Áttekintés' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Edzésnapló' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Edzőim' })).toBeVisible();

    // A futó nem látja az admin menüt.
    await expect(page.getByRole('link', { name: 'Felhasználók' })).toHaveCount(0);
  });

  test('az edzésnapló oldal megnyílik és van rajta „Új edzés” gomb', async ({ page }) => {
    await loginAs(page, 'runner@futoedzes.local', '/workouts');

    await expect(page.getByRole('heading', { name: 'Edzésnapló' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Új edzés' })).toBeVisible();
  });
});
