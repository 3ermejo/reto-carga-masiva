# Se construye con contexto ./src (ver docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BuildingBlocks/Shared.Messaging/Shared.Messaging.csproj BuildingBlocks/Shared.Messaging/
COPY Services/Notification.Worker/Notification.Worker.csproj Services/Notification.Worker/
RUN dotnet restore Services/Notification.Worker/Notification.Worker.csproj

COPY BuildingBlocks/Shared.Messaging/ BuildingBlocks/Shared.Messaging/
COPY Services/Notification.Worker/ Services/Notification.Worker/
RUN dotnet publish Services/Notification.Worker/Notification.Worker.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Notification.Worker.dll"]
