# ============================================================
# BUILD STAGE
# Compile et publie l'API avec le SDK .NET 9
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copier toute la solution afin d'inclure tous les projets référencés.
COPY . .

# Trouver le projet API, restaurer les packages et publier.
RUN API_PROJECT="$(find /src -name 'Inventory.Api.csproj' -print -quit)" \
    && test -n "$API_PROJECT" \
    && echo "API project found: $API_PROJECT" \
    && dotnet restore "$API_PROJECT" \
    && dotnet publish "$API_PROJECT" \
        --configuration Release \
        --output /app/publish \
        --no-restore \
        /p:UseAppHost=false


# ============================================================
# RUNTIME STAGE
# Contient uniquement le runtime ASP.NET Core
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

# L'API écoute en HTTP sur le port interne 8080.
ENV ASPNETCORE_HTTP_PORTS=8080

# Désactive la redirection HTTPS à l'intérieur du conteneur.
ENV UseHttpsRedirection=false

# Le port est documenté par l'image.
EXPOSE 8080

# Copier uniquement les fichiers publiés.
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]