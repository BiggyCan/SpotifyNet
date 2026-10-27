# Compilacion multi-etapa de SpotifyNet (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SpotifyNet.csproj ./
RUN dotnet restore SpotifyNet.csproj
COPY . .
RUN dotnet publish SpotifyNet.csproj -c Release -o /app/publish --no-restore

# Imagen final, sin SDK ni el codigo fuente completo
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# SQLite y los audios viven en volumenes persistentes
RUN mkdir -p /app/data /app/App_Data/audio /app/App_Data/covers && \
    chown -R $APP_UID:$APP_UID /app/data /app/App_Data

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SpotifyNet.dll"]
