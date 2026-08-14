FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /source

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

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "StudyOrganizer.Api.dll"]
