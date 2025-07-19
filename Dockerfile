# Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage avec Node.js intégré
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS with-node
RUN apt-get update
RUN apt-get install curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs

FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
# Variables d'environnement pour Vite
ENV VITE_DOCKER=true
WORKDIR /src

# Copier les fichiers de projet
COPY ["MonEndoVue.Server/MonEndoVue.Server.csproj", "MonEndoVue.Server/"]
COPY ["monendovue.client/monendovue.client.esproj", "monendovue.client/"]

# Restore des dépendances (.NET + npm)
RUN dotnet restore "./MonEndoVue.Server/MonEndoVue.Server.csproj"

# Copier tout le code source
COPY . .

# Build
WORKDIR "/src/MonEndoVue.Server"
RUN dotnet build "./MonEndoVue.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MonEndoVue.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MonEndoVue.Server.dll"]