# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# MATCH THE CASING: OrderingSystem.Api (lowercase 'i')
COPY src/OrderingSystem.Api/OrderingSystem.Api.csproj src/OrderingSystem.Api/
COPY src/OrderingSystem.Domain/OrderingSystem.Domain.csproj src/OrderingSystem.Domain/
COPY src/OrderingSystem.Infrastructure/OrderingSystem.Infrastructure.csproj src/OrderingSystem.Infrastructure/

# Restore using the exact case
RUN dotnet restore "src/OrderingSystem.Api/OrderingSystem.Api.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/src/OrderingSystem.Api"
RUN dotnet build "OrderingSystem.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "OrderingSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderingSystem.Api.dll"]