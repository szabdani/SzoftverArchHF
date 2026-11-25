# E2E tesztek (Playwright)

Böngészős végpont-végpont tesztek a **futó rendszer** ellen. A bejelentkezés token-injektálással történik
(`auth.ts`): a teszt a Keycloaktól jelszavas folyamattal tokent kér, és a `sessionStorage`-ba írja az
oidc-client-ts `User` alakjában, így nem kell a Keycloak űrlapján át belépni.

## Előfeltétel

A teljes rendszernek futnia kell (a repó gyökeréből):

```bash
docker compose up -d --build
```

Várd meg, amíg a Keycloak felimportálja a realmet és a gateway válaszol a `http://localhost` címen.

## Futtatás

A `frontend/` mappából:

```bash
npm ci
npx playwright install chromium   # első alkalommal
npm run test:e2e                  # chromium ellen, http://localhost
```

Hasznos opciók:

```bash
npx playwright test --headed      # látható böngészővel
npx playwright test --ui          # interaktív futtató
npx playwright show-report        # a legutóbbi riport
```

## Beállítások (környezeti változók)

| Változó | Alapérték | Mire való |
|---|---|---|
| `E2E_BASE_URL` | `http://localhost` | A rendszer címe |
| `E2E_OIDC_AUTHORITY` | `http://localhost/auth/realms/futoedzes` | Keycloak realm |
| `E2E_OIDC_CLIENT_ID` | `futoedzes-frontend` | A frontend kliens |
| `E2E_ALL_BROWSERS` | – | Ha beállított, a firefox és webkit (Safari-motor) projekt is fut |

## Lefedett folyamatok

- **Futó:** bejelentkezés, az áttekintő oldal és a menü; az edzésnapló megnyitása.
- **Edzésrögzítés:** érvényes edzés rögzítése és megjelenése; érvénytelen bevitel mezőhibája.
- **Admin + edző:** demóadatok létrehozása; a demó futók megjelenése az edző „Futóim” listájában kockázattal.

A tesztek sorban futnak (`workers: 1`), mert közös rendszerállapotot (demóadatok, kapcsolatok) módosítanak.
