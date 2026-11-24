import { useAuth } from 'react-oidc-context';

/** Az alkalmazás szerepkörei (Keycloak realm szerepkörök). */
export type Role = 'runner' | 'coach' | 'admin';

/**
 * A Keycloak access token `realm_access.roles` mezőjéből az alkalmazás szerepkörei. Hibás vagy hiányzó
 * tokennél üres lista. A token aláírását a backend ellenőrzi; itt csak a menühöz olvassuk.
 */
export function rolesFromAccessToken(token: string | undefined): Role[] {
  const payload = token?.split('.')[1];
  if (!payload) {
    return [];
  }

  try {
    // A JWT base64url kódolású, UTF-8 tartalommal.
    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const json = new TextDecoder().decode(Uint8Array.from(atob(base64), (c) => c.charCodeAt(0)));
    const roles: unknown = JSON.parse(json)?.realm_access?.roles;
    return Array.isArray(roles) ? roles.filter((role): role is Role => ['runner', 'coach', 'admin'].includes(role)) : [];
  } catch {
    return [];
  }
}

/**
 * A bejelentkezett felhasználó szerepkörei.
 */
export function useRoles(): Role[] {
  const auth = useAuth();
  return rolesFromAccessToken(auth.user?.access_token);
}
