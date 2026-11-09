# Se construye con contexto ./src (ver docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BuildingBlocks/Shared.Messaging/Shared.Messaging.csproj BuildingBlocks/Shared.Messaging/
COPY Services/Control.Api/Control.Api.csproj Services/Control.Api/
RUN dotnet restore Services/Control.Api/Control.Api.csproj

COPY BuildingBlocks/Shared.Messaging/ BuildingBlocks/Shared.Messaging/
COPY Services/Control.Api/ Services/Control.Api/
RUN dotnet publish Services/Control.Api/Control.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Control.Api.dll"]
