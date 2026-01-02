# ----------------------------
# Build stage
# ----------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DshEtlSearch.slnx ./
COPY DshEtlSearch.Api/DshEtlSearch.Api.csproj DshEtlSearch.Api/
COPY DshEtlSearch.Core/DshEtlSearch.Core.csproj DshEtlSearch.Core/
COPY DshEtlSearch.Infrastructure/DshEtlSearch.Infrastructure.csproj DshEtlSearch.Infrastructure/
COPY DshEtlSearch.Tests/DshEtlSearch.Tests.csproj DshEtlSearch.Tests/

# Restore (API project only — safest with slnx)
RUN dotnet restore DshEtlSearch.Api/DshEtlSearch.Api.csproj

# Copy remaining source
COPY . .

# Publish (DO NOT disable restore)
WORKDIR /src/DshEtlSearch.Api
RUN dotnet publish -c Release -o /app/publish

# ----------------------------
# Runtime stage
# ----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80801

ENTRYPOINT ["dotnet", "DshEtlSearch.Api.dll"]
