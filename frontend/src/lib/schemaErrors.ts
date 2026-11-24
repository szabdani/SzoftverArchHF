import type { z } from 'zod';

/**
 * Egy kérés ellenőrzése a szerverrel közös (generált) zod sémával. Mezőnként az első hiba számít; az
 * üzenet a felület magyar szövege az adott mezőre (a szabály maga a sémából jön), ha nincs ilyen, a zodé.
 */
export function schemaErrors(schema: z.ZodType, candidate: unknown, messages: Record<string, string>): Record<string, string> {
  const result = schema.safeParse(candidate);
  const errors: Record<string, string> = {};

  if (!result.success) {
    for (const issue of result.error.issues) {
      const field = String(issue.path[0]);
      errors[field] ??= messages[field] ?? issue.message;
    }
  }

  return errors;
}
