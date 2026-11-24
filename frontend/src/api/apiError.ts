/**
 * Sikertelen API-hívás: a HTTP státusz és a válasz törzse (pl. a 400-as ValidationProblem).
 */
export class ApiError extends Error {
  readonly status: number;
  readonly body: unknown;

  /**
   * Új hiba a státusszal és a már feldolgozott (JSON vagy szöveges) törzzsel.
   */
  constructor(status: number, body: unknown) {
    super(`API hiba (${status})`);
    this.status = status;
    this.body = body;
  }
}

/** A szerver 400-as validációs hibája: mezőnként (JSON mezőnév) a magyar hibaüzenetek. */
export type ValidationProblem = { errors: Record<string, string[]> };

/**
 * Validációs hiba-e (400, mezőnkénti hibákkal).
 */
export function isValidationProblem(error: unknown): error is ApiError & { body: ValidationProblem } {
  return (
    error instanceof ApiError &&
    error.status === 400 &&
    typeof error.body === 'object' &&
    error.body !== null &&
    'errors' in error.body
  );
}

/**
 * Felhasználónak szóló magyar hibaüzenet egy sikertelen hívásról (értesítéshez).
 */
export function errorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'Váratlan hiba történt.';
  }

  const title = typeof error.body === 'object' && error.body !== null && 'title' in error.body ? String(error.body.title) : null;
  switch (error.status) {
    case 400:
      return 'Hibás adatok, ellenőrizd a mezőket.';
    case 401:
      return 'Lejárt a bejelentkezés, jelentkezz be újra.';
    case 403:
      return 'Ehhez nincs jogosultságod.';
    case 404:
      return 'A keresett adat nem található.';
    case 409:
      return title ?? 'Ütközés: a művelet nem hajtható végre.';
    default:
      return 'A szerver nem érhető el vagy hibát jelzett.';
  }
}
