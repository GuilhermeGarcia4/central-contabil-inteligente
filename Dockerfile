FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Restore separately so dependency downloads remain cached when source files change.
COPY ["NuGet.Config", "./"]
COPY ["backend/src/CentralContabil.Api/CentralContabil.Api.csproj", "backend/src/CentralContabil.Api/"]
RUN dotnet restore "backend/src/CentralContabil.Api/CentralContabil.Api.csproj"

COPY ["backend/src/CentralContabil.Api/", "backend/src/CentralContabil.Api/"]
RUN dotnet publish "backend/src/CentralContabil.Api/CentralContabil.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    PORT=8080

EXPOSE 8080

COPY --from=build /app/publish .

USER app
ENTRYPOINT ["sh", "-c", "exec dotnet CentralContabil.Api.dll --urls http://0.0.0.0:${PORT}"]
