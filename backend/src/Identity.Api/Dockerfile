# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Contracts/Contracts.csproj src/Contracts/
COPY src/BuildingBlocks/BuildingBlocks.csproj src/BuildingBlocks/
COPY src/Identity.Api/Identity.Api.csproj src/Identity.Api/
RUN dotnet restore src/Identity.Api/Identity.Api.csproj
COPY src/ src/
RUN dotnet publish src/Identity.Api/Identity.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Identity.Api.dll"]
