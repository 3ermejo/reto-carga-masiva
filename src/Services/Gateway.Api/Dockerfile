# Se construye con contexto ./src (ver docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Services/Gateway.Api/Gateway.Api.csproj Services/Gateway.Api/
RUN dotnet restore Services/Gateway.Api/Gateway.Api.csproj

COPY Services/Gateway.Api/ Services/Gateway.Api/
RUN dotnet publish Services/Gateway.Api/Gateway.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
