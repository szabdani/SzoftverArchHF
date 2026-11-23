/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** A Keycloak realm címe; alapból http://localhost/auth/realms/futoedzes. */
  readonly VITE_OIDC_AUTHORITY?: string;
  /** A frontend kliens azonosítója; alapból futoedzes-frontend. */
  readonly VITE_OIDC_CLIENT_ID?: string;
}
