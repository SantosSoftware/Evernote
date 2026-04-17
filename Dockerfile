# ── Etapa 1: Build ──────────────────────────────────────────────────────────
# cache-bust: 3
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura dependências antes de copiar o restante (cache de camadas)
COPY Parch.csproj ./
RUN dotnet restore Parch.csproj

# Copia o restante e publica em modo Release
COPY . .
RUN dotnet publish Parch.csproj -c Release -o /app/out

# ── Etapa 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Cria o diretório de dados (será sobrescrito pelo volume do Fly.io)
RUN mkdir -p /data/uploads

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DATABASE_PATH=/data/parch.db
ENV UPLOADS_PATH=/data/uploads

EXPOSE 8080

ENTRYPOINT ["dotnet", "Parch.dll"]
