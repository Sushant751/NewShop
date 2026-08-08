# ==============================================================================
# Dockerfile for Billing.API (ASP.NET Core 9)
# Multi-stage build: restore → build → publish → runtime
# ==============================================================================

# ---------- Stage 1: Base image with SDK for building ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only project files first for better layer caching
COPY ["BillingSystem.sln", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/Billing.Shared/Billing.Shared.csproj", "src/Billing.Shared/"]
COPY ["src/Billing.Domain/Billing.Domain.csproj", "src/Billing.Domain/"]
COPY ["src/Billing.Contracts/Billing.Contracts.csproj", "src/Billing.Contracts/"]
COPY ["src/Billing.Application/Billing.Application.csproj", "src/Billing.Application/"]
COPY ["src/Billing.Persistence/Billing.Persistence.csproj", "src/Billing.Persistence/"]
COPY ["src/Billing.Infrastructure/Billing.Infrastructure.csproj", "src/Billing.Infrastructure/"]
COPY ["src/Billing.Identity/Billing.Identity.csproj", "src/Billing.Identity/"]
COPY ["src/Billing.API/Billing.API.csproj", "src/Billing.API/"]

# Restore dependencies
RUN dotnet restore "BillingSystem.sln"

# Copy the rest of the source code
COPY ["src/", "src/"]
COPY ["tests/", "tests/"]

# Build and publish the API project
RUN dotnet publish "src/Billing.API/Billing.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- Stage 2: Runtime image (smaller, no SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

# Set environment variables for container
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish .

# Create a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
RUN chown -R appuser:appgroup /app
USER appuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Billing.API.dll"]
