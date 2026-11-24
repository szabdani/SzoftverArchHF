import { describe, expect, it } from 'vitest';
import { rolesFromAccessToken } from './roles';

/**
 * Teszt access token a megadott tartalommal (aláírás nélkül; a szerepkörökhöz csak a tartalom kell).
 */
function token(payload: object): string {
  const bytes = new TextEncoder().encode(JSON.stringify(payload));
  const base64 = btoa(String.fromCharCode(...bytes)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `fejlec.${base64}.alairas`;
}

/** Szerepkörök kiolvasása a Keycloak tokenből. */
describe('rolesFromAccessToken', () => {
  it('az alkalmazás szerepköreit adja, a többit (pl. offline_access) elhagyja', () => {
    const roles = rolesFromAccessToken(token({ name: 'Kovács Éva', realm_access: { roles: ['runner', 'offline_access', 'coach'] } }));

    expect(roles).toEqual(['runner', 'coach']);
  });

  it('hiányzó vagy hibás tokennél üres', () => {
    expect(rolesFromAccessToken(undefined)).toEqual([]);
    expect(rolesFromAccessToken('nem.token')).toEqual([]);
    expect(rolesFromAccessToken(token({ sub: 'x' }))).toEqual([]);
  });
});
