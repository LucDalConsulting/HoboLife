# Minimal image for the HoboLife presence server. Runs the TypeScript entry
# directly with tsx — no build step needed.
FROM node:20-alpine
WORKDIR /app
COPY package.json ./
RUN npm install --omit=dev
COPY . .
ENV PORT=2567
EXPOSE 2567
CMD ["npm", "start"]
