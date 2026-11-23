# Futóedzés-tervező – frontend

React + TypeScript (Vite), Mantine UI, bejelentkezés Keycloakkal (OIDC, PKCE).

## Indítás fejlesztéshez

A backend a repó gyökeréből: `docker compose up --build` (Keycloak, gateway és API-k a `http://localhost`-on). Utána:

```bash
npm install
npm run dev   # http://localhost:5173
```

A Vite az `/api` kéréseket a gatewayre (`http://localhost`) továbbítja. Belépés pl. `runner@futoedzes.local` / `Demo1234` (a többi felhasználó a gyökér README-ben).

| Változó | Alapérték |
|---|---|
| `VITE_OIDC_AUTHORITY` | `http://localhost/auth/realms/futoedzes` |
| `VITE_OIDC_CLIENT_ID` | `futoedzes-frontend` |

## Parancsok

| Parancs | Mit csinál |
|---|---|
| `npm run generate:api` | API-kliens, típusok és zod sémák a `../docs/openapi/*.json`-ból (`src/api/{training,identity,analytics}`) |
| `npm run lint` | ESLint |
| `npm test` | Vitest egységtesztek |
| `npm run build` | típusellenőrzés + éles build |

A zod sémák ugyanazokat a validációs szabályokat tartalmazzák, mint a szerver (közös séma, spec 3.2). Backend-változás után előbb a sémát kell újraexportálni (`dotnet build backend/FutoedzesTervezo.slnx -p:OpenApiGenerateDocuments=true`), utána `npm run generate:api`.
