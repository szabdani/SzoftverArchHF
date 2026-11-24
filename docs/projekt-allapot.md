# Projektállapot – kontextus AI-asszisztenseknek és új csapattagoknak

> **Utoljára frissítve:** 2026-11-24.
>
> Ez a fájl leírja, hol tart a projekt, milyen döntések alapján készült, és mit kell még mindenképpen tesztelni. Ha a git history vagy a GitHub (PR-ok, merge-ek) mást mutat, **azok az irányadók**, és ezt a fájlt frissíteni kell.

## 1. Röviden

- **A feladat:** BME Szoftverarchitektúrák házi feladat, „Futóedzés-tervező és terhelésfigyelő”.
- **A követelmények:** [SzoftArchHF_Futoedzes_Specifikacio.pdf](SzoftArchHF_Futoedzes_Specifikacio.pdf), benne 20 use-case (UC01–UC20). A kódban a `spec 3.x` hivatkozások ennek a fejezeteire mutatnak.
- **A backend kész,** mind a 20 use-case-zel, és a `main`-ben van.
- **A frontend kész** mindhárom szerepkörre (futó, edző, admin), de **még nyitott, egymásra épülő PR-okban van** (lásd a 3. fejezetet).
- **Docker Compose:** egy paranccsal indul a teljes rendszer, a frontenddel együtt (ez is nyitott PR-ban van).
- **A legfontosabb hiány: a rendszer még egyszer sem futott együtt élőben** (Docker + böngésző + Keycloak). Minden ellenőrzés build, lint, egységteszt és CI volt. Ezért kiemelten fontos a 8. fejezet.

## 2. Mi alapján haladtunk

### Technológiák

A spec 4. fejezete szerint, kiegészítő könyvtárakkal.

| Terület | Választás |
|---|---|
| Backend | C# / .NET 10 minimal API-k, három mikroszolgáltatás, szolgáltatásonként külön PostgreSQL |
| ORM | EF Core 10 + Npgsql, snake_case nevek; a migrációk induláskor automatikusan lefutnak |
| Üzenetküldés | RabbitMQ + MassTransit 8, EF outbox/inbox (az esemény ugyanabban a tranzakcióban mentődik, mint az adat) |
| Hitelesítés | Keycloak (OIDC); a szerepkörök a token `realm_access.roles` mezőjében: `runner`, `coach`, `admin` |
| Gateway | Traefik; útvonalak: `/api/training`, `/api/identity`, `/api/analytics`, `/auth` (Keycloak), `/` (frontend) |
| Frontend | React 19 + TypeScript, Vite, Mantine 9 (UI és diagramok), FullCalendar 7 (naptár), TanStack Query, React Router, react-oidc-context (PKCE) |
| Kliens–szerver séma | OpenAPI-ból generált kliens és zod sémák (`@hey-api/openapi-ts`) |
| Tesztek | Backend: xUnit v3 (Microsoft Testing Platform); frontend: Vitest + Testing Library |
| CI | GitHub Actions: `backend.yml` (build + teszt), `frontend.yml` (lint, teszt, build, Docker image) |

### Spec-értelmezések és döntések

Ahol a spec nem egyértelmű, ezek a döntések születtek. Ne írd át őket egyeztetés nélkül.

- **Választott extra funkció:** szabályalapú (nem gépi tanulásos) kockázati pontszám, a spec 3.8 pontjai szerint.
- **ACWR** = akut terhelés (az utolsó 7 nap sRPE-összege) / krónikus terhelés (az utolsó 28 nap heti átlaga).
  - Csak akkor számolódik, ha a futó első edzése óta eltelt 28 nap; előtte nincs ACWR-pont.
  - Két tizedesre kerekítve.
- **A „mai nap”** mindenhol a felhasználók időzónájában értendő (`Europe/Budapest`, lásd `backend/src/BuildingBlocks/Time/AppTime.cs`). Ide tartozik a „nem lehet jövőbeli” szabály, a kihagyott edzések és a napi pontszám.
- **„Naponta” futó ellenőrzés (UC19):** a `MissedWorkoutJob` (Training) és a `DailyRiskJob` (Analytics) **óránként** fut. Mindkettő idempotens; így egy újraindítás sem hagy ki napot.
- **A figyelmeztetések deduplikációja:** ugyanaz a szabály ugyanarra az ISO hétre csak egyszer generál figyelmeztetést.
- **Az edző a hozzárendelt futó figyelmeztetéseit nyugtázhatja is** (a spec 3.1 táblázata szerint: „megtekintése, nyugtázása – hozzárendelt futóké”).
- **Meghívás (UC12):** az alkalmazáson belül működik, **valódi e-mail nem megy ki**. A meghívó az e-mail címhez kötődik; a futó az „Edzőim” oldalon látja, ha ezzel a címmel lép be.
  - Ugyanarra a címre egyszerre egy függő meghívó vagy élő kapcsolat lehet; ilyenkor 409 a válasz.
- **Admin:**
  - a saját szerepkörét nem módosíthatja, és magát nem tilthatja le, így az utolsó admin nem zárhatja ki magát;
  - adminná csak admin nevezhet ki;
  - a futók edzésadatait nem látja.
- **Regisztráció (UC01):** Keycloak önregisztrációval; az új felhasználó automatikusan `runner` szerepkört kap.
- **Demóadatok (UC18):**
  - négy szimulált futó jön létre, a `coach@futoedzes.local` edzőhöz kapcsolva, 12 hét adattal, a spec négy mintájával;
  - az újragenerálás lecseréli a korábbiakat;
  - törléskor egy `RunnerDataReset` esemény megy ki, hogy az Analytics ne adjon hamis figyelmeztetéseket.
- **Az Analytics** a Training eseményeit sorban dolgozza fel (`ConcurrentMessageLimit = 1`), így az újraszámolások nem versenyeznek.
- **Hozzáférés:** idegen vagy nem látható adatra 404 a válasz (nem 403), hogy az adat létezése se derüljön ki. Az edző hozzáférését minden szolgáltatás szerveroldalon ellenőrzi (`CanViewRunnerAsync`).

## 3. Hol van a kód (ágak, PR-ok)

- **`main`:** a teljes backend, az infrastruktúra és a CI (PR #1–#13, merge-elve).
- **Nyitott, egymásra épülő PR-ok.** Mindegyik az előző ágára épül, ezért **sorrendben kell merge-elni**. Ha egy ág merge után törlődik, a GitHub a következő PR bázisát magától átállítja.

| Sorrend | PR | Ág | Tartalom |
|---|---|---|---|
| 1 | #14 | `feature/api-response-types` | Backend: válaszsémák az OpenAPI-ban (a generált frontend kliensnek kell) |
| 2 | #15 | `feature/frontend-foundation` | Frontend alapok: Mantine, Keycloak-belépés, generált API-kliens, menü, CI |
| 3 | #16 | `feature/frontend-workouts` | Edzésnapló (UC02–UC04, UC08) |
| 4 | #17 | `feature/frontend-dashboard` | Dashboard, diagramok, kockázat, javaslat, figyelmeztetések (UC06, UC09, UC10, UC15) |
| 5 | #18 | `feature/frontend-runner-plans` | Célok, naptár húzással, edzéstervek, „Edzőim” (UC05, UC07, UC11, UC13) |
| 6 | #19 | `feature/frontend-coach` | Edzői nézetek (UC12, UC14 + a futó adatai) |
| 7 | #20 | `feature/frontend-admin` | Admin nézetek (UC16–UC18) |
| 8 | #21 | `feature/frontend-container` | Frontend konténer (nginx), Traefik a `/`-en, Docker image a CI-ban |

**A legfrissebb, teljes kód a `feature/frontend-container` ágon van**, ez tartalmazza az összes alatta lévőt. Ha a PR-ok még nincsenek merge-elve, kipróbáláshoz ezt az ágat kell kivenni.

### Use-case-ek lefedése

A frontend-útvonalak a `frontend/src/pages/` alatt vannak.

| UC | Backend | Frontend |
|---|---|---|
| UC01 Regisztráció, bejelentkezés | Keycloak realm: `infra/keycloak/futoedzes-realm.json` | `frontend/src/auth/`, `frontend/src/app/RequireAuth.tsx` |
| UC02–UC04 Edzések | `Training.Api/Workouts` | `workouts/` |
| UC05 Célok | `Training.Api/Goals` | `goals/` |
| UC06 Dashboard | `Analytics.Api/Dashboard` | `dashboard/` |
| UC07 Naptár | `Training.Api/Plans` | `calendar/` |
| UC08 Tervezett edzés teljesítése | `Training.Api/Workouts` (`PlannedWorkoutId`) | `workouts/WorkoutFormModal.tsx` |
| UC09 Figyelmeztetések | `Analytics.Api/Alerts` | `alerts/`, kártya a dashboardon |
| UC10 Javaslat | `Analytics.Api/Recommendations` | `dashboard/RecommendationCard.tsx` |
| UC11 Edzésterv | `Training.Api/Plans` | `calendar/PlanFormModal.tsx`, `calendar/PlannedWorkoutFormModal.tsx` |
| UC12 Futó meghívása | `Identity.Api/Invitations` | `coach/CoachOverviewPage.tsx` |
| UC13 Kapcsolat elfogadása, megszüntetése | `Identity.Api/Invitations` | `coaches/` |
| UC14 Futók áttekintése | `Analytics.Api/Dashboard` (edzői összesítő) | `coach/` (a futó oldala: `coach/CoachRunnerPage.tsx`) |
| UC15 Kockázati pontszám | `Analytics.Api/Risk` | `dashboard/RiskCard.tsx` |
| UC16 Felhasználók | `Identity.Api/Admin` (Keycloak Admin API-n át, service accounttal) | `admin/AdminUsersPage.tsx` |
| UC17 Küszöbök | `Analytics.Api/Rules` | `admin/AdminRulesPage.tsx` |
| UC18 Demóadatok | `Identity.Api/Demo`, `Training.Api/Demo` | `admin/DemoDataCard.tsx` |
| UC19 Napi ellenőrzés | `Training.Api/Plans/MissedWorkoutJob.cs`, `Analytics.Api/Risk/DailyRiskJob.cs` | – |
| UC20 Újraszámítás | `Analytics.Api/ReadModels/TrainingEventsConsumer.cs` | – |

## 4. Architektúra dióhéjban

- **Training.Api:** edzések, célok, edzéstervek, tervezett edzések, demóadatok generálása.
- **Identity.Api:** edzői meghívások és kapcsolatok, felhasználókezelés (Keycloak Admin API), a demó indítása.
- **Analytics.Api:** a Training eseményeiből saját olvasási modellt épít. Ebből számolja a terhelést, a kockázati pontszámot, a figyelmeztetéseket, a javaslatot és a dashboardot, és itt vannak a szabálybeállítások is.
- **BuildingBlocks:** közös auth (policy-k: `Runner`, `Coach`, `Admin`, `RunnerOrCoach`), messaging, perzisztencia, validáció, idő, edzői hozzáférés.
- **Contracts:** az integrációs események.

| Esemény | Küldi | Fogadja |
|---|---|---|
| `WorkoutRecorded/Updated/Deleted`, `PlannedWorkoutChanged/Removed`, `RunnerGoalChanged` | Training | Analytics |
| `CoachRunnerLinked/Unlinked` | Identity | Training, Analytics (mindkettő saját másolatot tart a kapcsolatokról) |
| `DemoDataRequested/Removed` | Identity | Training |
| `RunnerDataReset` | Training | Analytics |

## 5. Konvenciók (a további munkához is kötelezők)

- **A lehető legegyszerűbb működő megoldás.** Nincs felesleges réteg, wrapper vagy „ServiceResult”: a service-ek nullable értéket vagy `bool`-t adnak vissza, és a végpont alakítja HTTP-válasszá.
- **C#:**
  - interface + service minta;
  - aszinkron metódus `Async` végződéssel;
  - a privát metódusok a fájl végén, egy blokkban;
  - minden metódus fölött magyar `/// <summary>`;
  - a bonyolultabb szűrésnél magyarázó komment a metóduson belül.
- **TypeScript:** magyar JSDoc minden exportált függvény és komponens fölött.
- **Nyelv:** a felület, a kommentek, a commit üzenetek és a PR-leírások magyarul.
- **Git:** minden feature külön `feature/...` ágon, PR-ral; a PR-leírásban „Mit tartalmaz” és „Ellenőrzés” rész.
- **Validáció (spec 3.2, közös séma):**
  1. A mezőnkénti szabályok a kérés-DTO-k attribútumai, magyar hibaüzenettel. A szerver ezekkel validál, és ugyanezek kerülnek az OpenAPI-ba.
  2. A dátumtól, több mezőtől vagy adatbázistól függő szabályok a `*Validator.cs` fájlokban vannak, csak szerveroldalon. A kliens a 400-as `ValidationProblem` mezőhibáit a mezőknél mutatja (`frontend/src/lib/serverErrors.ts`).
  3. DTO-változás után a sémát újra kell exportálni, majd újra kell generálni a klienst (lásd a 6. fejezetet).
- **Generált fájlok, kézzel ne szerkeszd őket:** `docs/openapi/*.json` és `frontend/src/api/{training,identity,analytics}/**/*.gen.ts`.
- **Frontend minták:**
  - SDK-hívás `throwOnError: true`-val;
  - mutáció után `queryClient.invalidateQueries()`;
  - az oldalak `runnerId`-t is kaphatnak (így használja őket az edzői nézet).
  - Tesztekben: az SDK `vi.mock`-kal mockolva, renderelés a `renderWithProviders`-szel (`frontend/src/test/render.tsx`, Mantine `env="test"`).

## 6. Parancsok

A teljes rendszer indítása, a felhasználók és a portok: lásd a [README](../README.md)-t. Minden demó felhasználó jelszava `Demo1234`.

```bash
# Backend
dotnet build backend/FutoedzesTervezo.slnx
dotnet test --solution backend/FutoedzesTervezo.slnx
# A közös séma újraexportálása DTO-változás után (docs/openapi/*.json)
dotnet build backend/FutoedzesTervezo.slnx -p:OpenApiGenerateDocuments=true
# Új migráció (dotnet-ef tool kell), pl. a Training szolgáltatásban:
dotnet ef migrations add <Nev> --project backend/src/Training.Api --output-dir Persistence/Migrations

# Frontend (a frontend/ mappában)
npm ci
npm run generate:api   # kliens + zod a docs/openapi-ból
npm run lint && npm test && npm run build
npm run dev            # http://localhost:5173; az /api-t a gatewayre (http://localhost) proxyzza

# Teljes rendszer (a repó gyökeréből)
docker compose up --build   # felület: http://localhost
```

**Tesztek a mostani állapotban:**
- backend: 195 teszteset;
- frontend: 53 teszt;
- mind zöld, a CI is.

## 7. Ami nincs kész, vagy ismert hiány

1. **Nincs élő próba:** a rendszer még nem futott együtt Dockerben és böngészőben (lásd a 8. fejezetet).
2. **Nincsenek integrációs és E2E tesztek.**
   - A backend tesztjei egységtesztek, adatbázis és broker nélkül.
   - A frontend tesztjei mockolt API-val futnak.
   - A szolgáltatások együttműködését (RabbitMQ, PostgreSQL, Keycloak) semmi nem teszteli automatikusan.
   - Javaslat: Testcontainers a backendhez (pl. edzés mentése → esemény → az Analytics újraszámol), Playwright a fő felhasználói folyamatokhoz.
3. **Valódi e-mail nem megy ki** a meghívásnál (lásd a 2. fejezetet). Ha a csapat igényli: Keycloak SMTP, vagy MailKit + egy Mailpit konténer.
4. **Reszponzivitás és böngészők:** a spec szerint Chrome, Firefox, Safari és Edge, asztalon és mobilon. Ez nincs ellenőrizve.
5. **Kisebb hiányok:**
   - az edzőnek nincs összesített figyelmeztetés-listája az összes futóról, csak futónként (a futó oldalán);
   - a frontend egyetlen nagy JS-csomag (a build figyelmeztet az 500 kB fölötti méretre), kódfelosztás nincs.

## 8. Mit kell mindenképpen tesztelni

Élőben, `docker compose up --build` után, a `http://localhost` címen. A felhasználók a README-ben vannak.

### Indulás és infrastruktúra

- [ ] Minden konténer elindul. Az API-k lefuttatják a migrációkat, a Keycloak importálja a `futoedzes` realmet.
- [ ] Az `Europe/Budapest` időzóna elérhető az API-konténerekben. Ha hiányzik, az első dátumfüggő hívás kivétellel elszáll (pl. edzés mentése).
- [ ] A Traefik jól irányít: `/` → frontend, `/api/*` → a megfelelő API, `/auth` → Keycloak.
- [ ] Mély link frissítése is működik (pl. `/coach/runners/<id>` újratöltése): ezt az nginx SPA fallbackje oldja meg.

### Bejelentkezés (UC01)

- [ ] A belépés és a kilépés működik a `http://localhost` címen (konténer) és a `http://localhost:5173` címen is (`npm run dev`).
- [ ] Regisztráció: új felhasználó → `runner` szerepkör → a futói menü jelenik meg.
- [ ] 5 percnél hosszabb munkamenet: a Keycloak access tokenje alapból kb. 5 percig él, a frontend csendben frissíti. Az API-hívások ezután is működjenek, kidobás nélkül.
- [ ] Szerepkörváltás után (admin → edző lesz valaki) mikor látszik az új menü: a token frissítése után vagy csak újrabelépéskor?

### Eseményvezérelt frissülés (a legkockázatosabb pont)

- [ ] Edzés mentése, módosítása és törlése után frissül a dashboard, a kockázati pontszám, a javaslat és a figyelmeztetések.
  - Az Analytics az eseményt aszinkron dolgozza fel, a felület viszont a mentés után azonnal újrakérdez, így elsőre régi adatot láthat.
  - Ha ez előjön, a felületen kell javítani (pl. rövid késleltetés vagy újrapróbálás).
- [ ] Tervezett edzés teljesítése (UC08): az edzés-űrlapon az aznapi tervezett edzéshez kötve az teljesítettre vált a naptárban és a terv–tény diagramon.

### Futó

- [ ] Edzésrögzítés validációja: kliens és szerver ugyanazt utasítja el (jövőbeli dátum, `10:00:01` időtartam, 0 km, 501 karakteres megjegyzés). A szerverhibák a mezőknél jelennek meg.
- [ ] Célok: egy aktív cél lehet, az újtól a régi archiválódik; látszik a hátralévő hetek száma.
- [ ] Naptár: havi és heti nézet; a tervezett, teljesített és kihagyott edzés eltérő jelöléssel.
  - **Húzás:** a terv időszakán belül mentődik, kívül hibaüzenettel visszaugrik.
  - Mobilon érintéssel is kipróbálandó.
- [ ] Kihagyott edzés (UC19): egy múltbeli, nem teljesített tervezett edzés legkésőbb egy órán belül „kihagyott” lesz; hetente legalább 2 esetén figyelmeztetés jön.
- [ ] Figyelmeztetés nyugtázása; ugyanaz a szabály ugyanazon a héten nem jön újra.

### Edző

- [ ] A meghívás a futó e-mail címére → a futó az „Edzőim” oldalon elfogadja → a futó megjelenik az edző „Futóim” listájában.
- [ ] A „Futóim” lista kockázat szerint rendezhető; a futó oldalán látszik a dashboard, az edzésnapló és a célok (csak olvasható), a naptárban pedig az edző tervet készíthet.
- [ ] Ha a futó megszünteti a kapcsolatot, az edző azonnal elveszíti a hozzáférést (a futó oldala és az API is elutasítja).

### Admin

- [ ] Felhasználó keresése, szerepkör módosítása, letiltás. A letiltott felhasználó nem tud belépni. A saját sor nem módosítható.
- [ ] Szabályok: egy küszöb módosítása után a pontszámok azonnal frissülnek; a kikapcsolt szabály nem ad pontot, és nem küld figyelmeztetést.
- [ ] Demóadatok:
  - a létrehozás után a `coach@futoedzes.local` „Futóim” listájában négy futó jelenik meg, eltérő kockázattal (a `demo-spike` magas);
  - törlés után eltűnnek, és nem marad utánuk hamis figyelmeztetés;
  - a demó futók be is tudnak lépni.

### Jogosultság (közvetlen API-hívásokkal is)

Token kérése: lásd a README-t.

- [ ] Az edző nem hozzárendelt futó adatait nem kapja meg (404).
- [ ] Az edző nem módosíthatja a futó edzéseit.
- [ ] Az admin nem látja a futók edzés- és pulzusadatait.
- [ ] A futó nem fér hozzá más futó adataihoz.

### Felület

- [ ] Chrome, Firefox, Safari és Edge, asztalon és mobil szélességben (a spec 4.1 szerint).
