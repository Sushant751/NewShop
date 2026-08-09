# ── Build Stage ──
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy solution and project files for layer caching
COPY BillingSystem.sln ./
COPY Directory.Build.props ./
COPY src/Billing.API/Billing.API.csproj src/Billing.API/
COPY src/Billing.Application/Billing.Application.csproj src/Billing.Application/
COPY src/Billing.Contracts/Billing.Contracts.csproj src/Billing.Contracts/
COPY src/Billing.Domain/Billing.Domain.csproj src/Billing.Domain/
COPY src/Billing.Identity/Billing.Identity.csproj src/Billing.Identity/
COPY src/Billing.Infrastructure/Billing.Infrastructure.csproj src/Billing.Infrastructure/
COPY src/Billing.Persistence/Billing.Persistence.csproj src/Billing.Persistence/
COPY src/Billing.Shared/Billing.Shared.csproj src/Billing.Shared/

# Restore dependencies
RUN dotnet restore src/Billing.API/Billing.API.csproj

# Copy full source code
COPY src/ src/

# Publish Release build
RUN dotnet publish src/Billing.API/Billing.API.csproj -c Release -o /out /p:UseAppHost=false

# ── Runtime Stage ──
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Configure ASP.NET Core URL and environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /out .

ENTRYPOINT ["dotnet", "Billing.API.dll"]
