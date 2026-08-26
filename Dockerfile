FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

COPY .config/dotnet-tools.json .config/
RUN dotnet tool restore

COPY backend/src/Api/StudyOrganizer.Api.csproj backend/src/Api/
COPY backend/src/Application/StudyOrganizer.Application.csproj backend/src/Application/
COPY backend/src/Domain/StudyOrganizer.Domain.csproj backend/src/Domain/
COPY backend/src/Infrastructure/StudyOrganizer.Infrastructure.csproj backend/src/Infrastructure/

RUN dotnet restore backend/src/Api/StudyOrganizer.Api.csproj

COPY backend/src/ backend/src/

WORKDIR /source/backend/src/Api

RUN dotnet publish StudyOrganizer.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

WORKDIR /source

RUN ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection="Host=localhost;Database=bundle_build;Username=bundle_build;Password=bundle_build" \
    Jwt__SigningKey="bundle-build-only-signing-key-with-at-least-32-characters" \
    dotnet tool run dotnet-ef -- migrations bundle \
    --project backend/src/Infrastructure \
    --startup-project backend/src/Api \
    --configuration Release \
    --no-build \
    --output /app/publish/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .
COPY scripts/start-api.sh /app/start-api.sh

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["/app/start-api.sh"]
