# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/BuildingBlocks/BuildingBlocks.csproj src/BuildingBlocks/
COPY src/Analytics.Api/Analytics.Api.csproj src/Analytics.Api/
RUN dotnet restore src/Analytics.Api/Analytics.Api.csproj
COPY src/ src/
RUN dotnet publish src/Analytics.Api/Analytics.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Analytics.Api.dll"]
