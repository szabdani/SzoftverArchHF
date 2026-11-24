import { defineConfig } from '@hey-api/openapi-ts';

/**
 * API-kliens generálása a backend OpenAPI leírásaiból (`npm run generate:api`).
 * Szolgáltatásonként típusok, zod sémák (a szerverrel közös validációs szabályok) és fetch SDK készül.
 * A leírásokat a backend exportálja: `dotnet build backend/FutoedzesTervezo.slnx -p:OpenApiGenerateDocuments=true`.
 */
const services = [
  { name: 'training', file: 'Training.Api.json' },
  { name: 'identity', file: 'Identity.Api.json' },
  { name: 'analytics', file: 'Analytics.Api.json' },
];

export default defineConfig(
  services.map((service) => ({
    input: `../docs/openapi/${service.file}`,
    output: `src/api/${service.name}`,
    plugins: ['@hey-api/client-fetch', '@hey-api/typescript', '@hey-api/sdk', 'zod'],
  })),
);
