import { User } from 'oidc-client-ts';
import type { AuthProviderProps } from 'react-oidc-context';

/** A Keycloak realm címe (a gatewayen át); fejlesztéskor felülírható a `VITE_OIDC_AUTHORITY` változóval. */
const authority = import.meta.env.VITE_OIDC_AUTHORITY ?? 'http://localhost/auth/realms/futoedzes';

/** A frontend nyilvános kliense a Keycloakban (PKCE-vel). */
const clientId = import.meta.env.VITE_OIDC_CLIENT_ID ?? 'futoedzes-frontend';

/**
 * Az OIDC bejelentkezés beállításai. Bejelentkezés után a visszatérési paramétereket eltávolítjuk az
 * URL-ből, különben a token csendes frissítése nem működne.
 */
export const oidcConfig: AuthProviderProps = {
  authority,
  client_id: clientId,
  redirect_uri: `${window.location.origin}/`,
  post_logout_redirect_uri: `${window.location.origin}/`,
  scope: 'openid profile email',
  onSigninCallback: () => window.history.replaceState({}, document.title, window.location.pathname),
};

/**
 * Az aktuális access token a React fán kívül is (az API-kliens ezzel hitelesít). Az oidc-client-ts
 * alapból a sessionStorage-ban tárolja a bejelentkezett felhasználót.
 */
export function getAccessToken(): string | undefined {
  const stored = sessionStorage.getItem(`oidc.user:${authority}:${clientId}`);
  return stored ? User.fromStorageString(stored).access_token : undefined;
}
