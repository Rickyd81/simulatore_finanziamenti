# Immagine unica: il front-end Angular compilato viene servito dall'API ASP.NET Core.

FROM node:24-alpine AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npx ng build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/SimulatoreFinanziamenti.Api/SimulatoreFinanziamenti.Api.csproj SimulatoreFinanziamenti.Api/
RUN dotnet restore SimulatoreFinanziamenti.Api/SimulatoreFinanziamenti.Api.csproj
COPY backend/SimulatoreFinanziamenti.Api/ SimulatoreFinanziamenti.Api/
RUN dotnet publish SimulatoreFinanziamenti.Api/SimulatoreFinanziamenti.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /src/dist/frontend/browser ./wwwroot
# Il database SQLite sta in /app/data: montare qui un disco persistente per conservare i dati.
RUN mkdir -p /app/data && chown -R app:app /app/data
ENV ConnectionStrings__Simulatore="Data Source=/app/data/simulatore.db" \
    ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SimulatoreFinanziamenti.Api.dll"]
