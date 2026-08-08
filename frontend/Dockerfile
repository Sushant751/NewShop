# ==============================================================================
# Dockerfile for Billing Frontend (React + Vite + TypeScript)
# Multi-stage build: build static assets → serve with nginx
# ==============================================================================

# ---------- Stage 1: Build the React app with Node ----------
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files first for better layer caching
COPY frontend/package.json frontend/package-lock.json* ./

# Install dependencies
RUN npm install

# Copy the rest of the frontend source
COPY frontend/ ./

# Build the production bundle
RUN npm run build

# ---------- Stage 2: Serve with nginx ----------
FROM nginx:alpine AS runtime

# Remove default nginx static assets
RUN rm -rf /usr/share/nginx/html/*

# Copy the custom nginx configuration
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Copy the built static assets from the build stage
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
