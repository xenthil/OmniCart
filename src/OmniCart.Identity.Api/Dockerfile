# syntax=docker/dockerfile:1

# --- Base runtime image (what actually ships) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
# Run as the built-in non-root user for defense in depth
USER app

# --- Build stage (SDK image, has compilers/tooling) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the .csproj files first so `dotnet restore` is cached
# separately from source changes (huge build-time win on rebuilds)
COPY ["src/OmniCart.Identity.Api/OmniCart.Identity.Api.csproj", "src/OmniCart.Identity.Api/"]
RUN dotnet restore "src/OmniCart.Identity.Api/OmniCart.Identity.Api.csproj"

# Now copy the rest of the source and build
COPY ["src/OmniCart.Identity.Api/", "src/OmniCart.Identity.Api/"]
WORKDIR "/src/src/OmniCart.Identity.Api"
RUN dotnet build "OmniCart.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# --- Publish stage ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OmniCart.Identity.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# --- Final image ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OmniCart.Identity.Api.dll"]
