FROM node:22-bookworm-slim AS deps
WORKDIR /app

COPY package.json package-lock.json ./
COPY apps/web/package.json ./apps/web/package.json
RUN npm ci

FROM node:22-bookworm-slim AS build
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY package.json ./
COPY apps/web ./apps/web

WORKDIR /app/apps/web
RUN npm run build

FROM node:22-bookworm-slim AS runtime
WORKDIR /app/apps/web

ENV NODE_ENV=production
COPY --from=build /app/apps/web ./
COPY --from=build /app/node_modules /app/node_modules

EXPOSE 3000

CMD ["npm", "run", "start"]
