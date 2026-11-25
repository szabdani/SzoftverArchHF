import { type Page, request } from '@playwright/test';

/** A fejlesztői/demó felhasználók jelszava (README). */
const PASSWORD = 'Demo1234';

/** A Keycloak realm és a frontend kliens (egyezik a frontend oidcConfig alapértelmezésével). */
const AUTHORITY = process.env.E2E_OIDC_AUTHORITY ?? 'http://localhost/auth/realms/futoedzes';
const CLIENT_ID = process.env.E2E_OIDC_CLIENT_ID ?? 'futoedzes-frontend';

/** A Keycloak jelszavas token-válaszának szükséges mezői. */
type TokenResponse = {
  access_token: string;
  id_token: string;
  refresh_token: string;
  token_type: string;
  scope: string;
  expires_in: number;
};

/**
 * Tokent kér a Keycloaktól jelszavas (ROPC) folyamattal, ahogy a fejlesztői curl-parancsok is.
 * Így a bejelentkezést nem a Keycloak űrlapján át végezzük, hanem a kész tokent injektáljuk.
 */
async function fetchToken(username: string): Promise<TokenResponse> {
  const ctx = await request.newContext();
  const response = await ctx.post(`${AUTHORITY}/protocol/openid-connect/token`, {
    form: {
      grant_type: 'password',
      client_id: CLIENT_ID,
      username,
      password: PASSWORD,
      scope: 'openid profile email',
    },
  });

  if (!response.ok()) {
    throw new Error(`Token-kérés sikertelen (${username}): ${response.status()} ${await response.text()}`);
  }

  const token = (await response.json()) as TokenResponse;
  await ctx.dispose();
  return token;
}

/** Egy JWT payload (profile) dekódolása a sessionStorage-ba injektáláshoz. */
function decodeJwtPayload(jwt: string): Record<string, unknown> {
  const base64 = jwt.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
  return JSON.parse(Buffer.from(base64, 'base64').toString('utf-8')) as Record<string, unknown>;
}

/**
 * Bejelentkezteti a megadott felhasználót a token-injektálásos módszerrel: tokent kér a Keycloaktól,
 * majd a betöltés előtt a sessionStorage-ba írja az oidc-client-ts `User` alakjában, a frontend által
 * várt `oidc.user:<authority>:<clientId>` kulccsal. Ezután a megadott útvonalra navigál.
 * @param page a Playwright lap.
 * @param username a bejelentkező felhasználó e-mail címe.
 * @param path a kezdő útvonal (alapból a főoldal).
 */
export async function loginAs(page: Page, username: string, path = '/'): Promise<void> {
  const token = await fetchToken(username);
  const profile = decodeJwtPayload(token.id_token);
  const user = {
    id_token: token.id_token,
    session_state: null,
    access_token: token.access_token,
    refresh_token: token.refresh_token,
    token_type: token.token_type,
    scope: token.scope,
    profile,
    expires_at: Math.floor(Date.now() / 1000) + token.expires_in,
  };
  const storageKey = `oidc.user:${AUTHORITY}:${CLIENT_ID}`;

  // A token a React (és az oidc-context) betöltése előtt kerüljön a sessionStorage-ba.
  await page.addInitScript(
    ([key, value]) => window.sessionStorage.setItem(key, value),
    [storageKey, JSON.stringify(user)] as const,
  );

  await page.goto(path);
}
