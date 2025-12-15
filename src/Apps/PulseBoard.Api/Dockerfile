FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM base AS base-debug
RUN apt-get update && apt-get install -y curl unzip procps \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY .editorconfig .

COPY src/Apps/PulseBoard.Api/PulseBoard.Api.csproj src/Apps/PulseBoard.Api/
COPY src/BuildingBlocks/PulseBoard.Configuration/PulseBoard.Configuration.csproj src/BuildingBlocks/PulseBoard.Configuration/
COPY src/BuildingBlocks/PulseBoard.Contracts/PulseBoard.Contracts.csproj src/BuildingBlocks/PulseBoard.Contracts/
COPY src/BuildingBlocks/PulseBoard.Domain/PulseBoard.Domain.csproj src/BuildingBlocks/PulseBoard.Domain/
COPY src/BuildingBlocks/PulseBoard.Infrastructure/PulseBoard.Infrastructure.csproj src/BuildingBlocks/PulseBoard.Infrastructure/

RUN dotnet restore src/Apps/PulseBoard.Api/PulseBoard.Api.csproj

COPY src/ src/
WORKDIR /src/src/Apps/PulseBoard.Api
RUN dotnet build PulseBoard.Api.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish PulseBoard.Api.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PulseBoard.Api.dll"]
