import { isValidationProblem } from '../api/apiError';

/**
 * A szerver 400-as validációs hibáinak megjelenítése az űrlap mezőinél (a hibakulcsok a JSON mezőnevek,
 * így egyeznek az űrlap mezőneveivel).
 * @returns true, ha validációs hiba volt és megjelent; különben a hívó kezeli (pl. értesítéssel).
 */
export function applyServerErrors(form: { setErrors: (errors: Record<string, string>) => void }, error: unknown): boolean {
  if (!isValidationProblem(error)) {
    return false;
  }

  form.setErrors(Object.fromEntries(Object.entries(error.body.errors).map(([field, messages]) => [field, messages.join(' ')])));
  return true;
}
