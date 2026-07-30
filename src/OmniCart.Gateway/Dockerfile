# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/OmniCart.Gateway/OmniCart.Gateway.csproj", "src/OmniCart.Gateway/"]
RUN dotnet restore "src/OmniCart.Gateway/OmniCart.Gateway.csproj"

COPY ["src/OmniCart.Gateway/", "src/OmniCart.Gateway/"]
WORKDIR "/src/src/OmniCart.Gateway"
RUN dotnet build "OmniCart.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OmniCart.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OmniCart.Gateway.dll"]
