# Se construye con contexto ./src (ver docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Services/Auth.Api/Auth.Api.csproj Services/Auth.Api/
RUN dotnet restore Services/Auth.Api/Auth.Api.csproj

COPY Services/Auth.Api/ Services/Auth.Api/
RUN dotnet publish Services/Auth.Api/Auth.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Usuario sin privilegios que ya trae la imagen de .NET
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
