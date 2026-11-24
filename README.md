# Futóedzés-tervező és terhelésfigyelő

Szoftverarchitektúrák házi feladat.

- `backend/` – ASP.NET Core mikroszolgáltatások (.NET 10)
- `frontend/` – React + TypeScript (Vite)
- `infra/` – Keycloak realm és egyéb infrastruktúra-konfiguráció
- `docs/` – specifikáció, OpenAPI leírások és a [projektállapot](docs/projekt-allapot.md): hol tartunk, milyen döntések alapján, mit kell még tesztelni (AI-asszisztenseknek is)

## Indítás

A teljes rendszer egy paranccsal indul a repó gyökeréből (Docker Desktop kell hozzá):

```bash
docker compose up --build
```

| Cím | Mi ez |
|---|---|
| http://localhost | Webes felület (belépés a lenti felhasználókkal) |
| http://localhost/api/analytics | Analytics API (terhelés, kockázati pontszám) |
| http://localhost/api/training | Training API (edzések) |
| http://localhost/api/identity | Identity API (edzői meghívások, kapcsolatok) |
| http://localhost/auth | Keycloak (admin konzol: `/auth/admin`, `admin` / `admin`) |
| http://localhost:8090 | Traefik dashboard |
| http://localhost:15672 | RabbitMQ management (`futoedzes` / `futoedzes`) |

### Fejlesztői felhasználók

A `futoedzes` realm induláskor importálódik ([infra/keycloak/futoedzes-realm.json](infra/keycloak/futoedzes-realm.json)). Minden felhasználó jelszava `Demo1234`.

| Felhasználó | Szerepkör |
|---|---|
| `admin@futoedzes.local` | admin |
| `coach@futoedzes.local` | coach |
| `runner@futoedzes.local` | runner |
| `runner2@futoedzes.local` | runner |

Új regisztráció automatikusan `runner` szerepkört kap.

### Demóadatok (UC18)

Az admin a felületen (Felhasználók → Demóadatok) vagy a `POST /api/identity/admin/demo` hívással négy szimulált futót kapcsol a `coach@futoedzes.local` edzőhöz, és mindegyiknek 12 hét edzésadatot, edzéstervet és célt generál. A `DELETE` ugyanezen a címen törli őket. Mind a négy be is tud lépni (`Demo1234`):

| Felhasználó | Minta |
|---|---|
| `demo-steady@futoedzes.local` | egyenletes építkezés |
| `demo-spike@futoedzes.local` | hirtelen terhelésugrás (magas kockázat) |
| `demo-skipper@futoedzes.local` | gyakori kihagyás |
| `demo-intense@futoedzes.local` | túl sok intenzív nap |

### Token kérése (fejlesztéshez)

```bash
curl -s http://localhost/auth/realms/futoedzes/protocol/openid-connect/token \
  -d grant_type=password -d client_id=futoedzes-frontend \
  -d username=runner@futoedzes.local -d password=Demo1234
```

A backend szolgáltatások `.http` fájljai is ezt használják.

## Közös validációs séma (kliens és szerver)

A kérések mezőnkénti szabályai (kötelező, min/max, max. hossz, minták, felsorolások) a backend kérés-DTO-in vannak attribútumként, magyar hibaüzenettel. A szerver ezekkel validál, és ugyanezek kerülnek az OpenAPI leírásba. A frontend ebből generálja a saját sémáját (pl. zod), így a kliens és a szerver ugyanazzal a sémával validál.

- **Sémafájlok** (OpenAPI 3.1): [docs/openapi/](docs/openapi/): `Training.Api.json`, `Identity.Api.json`, `Analytics.Api.json`
- **Frissítés** a backend módosítása után (futó backend nem kell hozzá):
  ```bash
  dotnet build backend/FutoedzesTervezo.slnx -p:OpenApiGenerateDocuments=true
  ```
- **Futó rendszeren**: `http://localhost/api/{training|identity|analytics}/openapi/v1.json`
- **Frontend**: egy OpenAPI 3.1-et ismerő generátorral, pl. `@hey-api/openapi-ts` zod pluginnal.

A szerver hibaválasza `400 ValidationProblem`, a hibák mezőnként a JSON mezőnevekkel (pl. `errors.distanceKm`). A táv két tizedesre kerekítődik, a szám mezők csak JSON számot fogadnak el.

### Csak szerveroldali szabályok

Ezek a mai naptól, több mezőtől vagy az adatbázistól függnek, ezért nincsenek a sémában. A frontend ugyanilyen mezőhibaként kapja vissza őket.

| Kérés | Szabály |
|---|---|
| Edzés | a dátum nem lehet jövőbeli |
| Cél | a céldátum nem lehet múltbeli; egyéni célnál kötelező a táv |
| Terv | a záródátum nem korábbi a kezdőnél, legfeljebb egy év |
| Tervezett edzés | a dátum a terv időszakába esik; tervezett táv vagy időtartam közül legalább az egyik kell |
| Meghívás | puszta e-mail cím (nem „Név &lt;cím&gt;”); saját magát nem hívhatja meg az edző |
| Szabálybeállítások | az emelkedő ACWR küszöb kisebb a kritikusnál |

## Backend fejlesztés

```bash
dotnet build backend/FutoedzesTervezo.slnx
dotnet test backend/FutoedzesTervezo.slnx
```

Egy szolgáltatás a compose-on kívül is futtatható (`dotnet run`), ha az infrastruktúra (Keycloak, RabbitMQ, a szolgáltatás adatbázisa) a compose-ból fut.
