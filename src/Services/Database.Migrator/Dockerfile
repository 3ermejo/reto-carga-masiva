# Se construye con contexto ./src (ver docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Services/Database.Migrator/Database.Migrator.csproj Services/Database.Migrator/
RUN dotnet restore Services/Database.Migrator/Database.Migrator.csproj

COPY Services/Database.Migrator/ Services/Database.Migrator/
RUN dotnet publish Services/Database.Migrator/Database.Migrator.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Database.Migrator.dll"]
