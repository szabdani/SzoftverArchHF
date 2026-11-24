import { client as analyticsClient } from './analytics/client.gen';
import { ApiError } from './apiError';
import { client as identityClient } from './identity/client.gen';
import { client as trainingClient } from './training/client.gen';

/**
 * A három generált API-kliens beállítása: ugyanarra az originre küldenek (fejlesztéskor a Vite proxy,
 * élesben a gateway továbbít), minden kérésre ráteszik a Bearer tokent, és hibánál `ApiError`-t dobnak
 * a HTTP státusszal, így a TanStack Query és az űrlapok egységesen kezelhetik.
 */
export function configureApiClients(getToken: () => string | undefined): void {
  for (const client of [trainingClient, identityClient, analyticsClient]) {
    client.setConfig({ baseUrl: '', throwOnError: true });

    client.interceptors.request.use((request) => {
      const token = getToken();
      if (token) {
        request.headers.set('Authorization', `Bearer ${token}`);
      }

      return request;
    });

    client.interceptors.error.use((error, response) => new ApiError(response?.status ?? 0, error));
  }
}
