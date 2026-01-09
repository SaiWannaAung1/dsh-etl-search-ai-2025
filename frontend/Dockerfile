# ---------- Build Stage ----------
FROM node:24-alpine AS builder
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---------- Runtime Stage ----------
FROM node:24-alpine AS runner
WORKDIR /app

COPY --from=builder /app/build ./build
COPY --from=builder /app/package.json ./

ENV NODE_ENV=production
ENV PORT=3000

EXPOSE 3000

CMD ["node", "build"]
