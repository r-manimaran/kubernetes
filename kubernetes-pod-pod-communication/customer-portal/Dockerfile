# ===============================
# Stage 1: Build
# ===============================
FROM node:22-alpine AS builder

WORKDIR /app

# Copy package.json and package-lock.json first to leverage Docker cache
COPY package.json package-lock.json ./

# Install dependencies without running scripts (like postinstall)
RUN npm ci --legacy-peer-deps --ignore-scripts

# Copy the rest of the application code
COPY . .

# Build the Angular application for production
RUN npm run build -- --configuration production

# ===============================
# Stage 2: Serve
# ===============================
FROM nginx:1.27-alpine AS runtime

# Remove default nginx config and static files
RUN rm -rf /usr/share/nginx/html/* /etc/nginx/conf.d/default.conf

COPY --from=builder /app/dist/customer-portal/browser /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
