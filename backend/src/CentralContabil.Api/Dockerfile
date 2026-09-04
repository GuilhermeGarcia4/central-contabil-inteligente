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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:10000

EXPOSE 10000

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "CentralContabil.Api.dll"]
