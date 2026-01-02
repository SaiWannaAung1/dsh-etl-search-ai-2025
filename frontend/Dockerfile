# Stage 1: Build (Placeholder)
FROM node:20-alpine AS build
WORKDIR /app

# Stage 2: Serve
FROM node:20-alpine AS serve
WORKDIR /app

# For now, just keep the container alive to prove connectivity
CMD ["sh", "-c", "echo 'Frontend Placeholder Running' && tail -f /dev/null"]