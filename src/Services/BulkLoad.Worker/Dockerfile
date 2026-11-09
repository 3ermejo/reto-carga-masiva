# Se construye con contexto ./src (ver docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BuildingBlocks/Shared.Messaging/Shared.Messaging.csproj BuildingBlocks/Shared.Messaging/
COPY Services/BulkLoad.Worker/BulkLoad.Worker.csproj Services/BulkLoad.Worker/
RUN dotnet restore Services/BulkLoad.Worker/BulkLoad.Worker.csproj

COPY BuildingBlocks/Shared.Messaging/ BuildingBlocks/Shared.Messaging/
COPY Services/BulkLoad.Worker/ Services/BulkLoad.Worker/
RUN dotnet publish Services/BulkLoad.Worker/BulkLoad.Worker.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Es un worker sin HTTP, por eso basta la imagen runtime (sin ASP.NET Core)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BulkLoad.Worker.dll"]
