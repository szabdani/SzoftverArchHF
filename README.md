# Futóedzés-tervező és terhelésfigyelő

Szoftverarchitektúrák házi feladat.

- `backend/` – ASP.NET Core mikroszolgáltatások (.NET 10)
- `frontend/` – React + TypeScript (Vite)
- `infra/` – Keycloak realm és egyéb infrastruktúra-konfiguráció
- `docs/` – specifikáció

## Indítás

A teljes rendszer egy paranccsal indul a repó gyökeréből (Docker Desktop kell hozzá):

```bash
docker compose up --build
```

| Cím | Mi ez |
|---|---|
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

Az admin a `POST /api/identity/admin/demo` hívással négy szimulált futót kapcsol a `coach@futoedzes.local` edzőhöz, és mindegyiknek 12 hét edzésadatot, edzéstervet és célt generál. A `DELETE` ugyanezen a címen törli őket. Mind a négy be is tud lépni (`Demo1234`):

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

## Backend fejlesztés

```bash
dotnet build backend/FutoedzesTervezo.slnx
dotnet test backend/FutoedzesTervezo.slnx
```

Egy szolgáltatás a compose-on kívül is futtatható (`dotnet run`), ha az infrastruktúra (Keycloak, RabbitMQ, a szolgáltatás adatbázisa) a compose-ból fut.
