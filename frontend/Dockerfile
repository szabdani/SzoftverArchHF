# Build context: frontend/
FROM node:24.22.3-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

# A kész statikus fájlokat nginx szolgálja ki; az /api és az /auth kéréseket a Traefik küldi tovább.
FROM nginx:1.30.8-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
