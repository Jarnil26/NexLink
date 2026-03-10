# Build Stage
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files and install
COPY package*.json ./
RUN npm install

# Copy all files and build for production
COPY . .
RUN npm run build -- --configuration production

# Production Stage
FROM nginx:alpine
# Copy custom nginx config if we had one, but default is often fine for simple SPAs
# COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# Expose port (Nginx default is 80)
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
