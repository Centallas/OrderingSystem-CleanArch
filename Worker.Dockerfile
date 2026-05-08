# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files using your actual folder name: OrderingSystem.LLMWorker
COPY ["src/OrderingSystem.LLMWorker/OrderingSystem.LLMWorker.csproj", "src/OrderingSystem.LLMWorker/"]
COPY ["src/OrderingSystem.Domain/OrderingSystem.Domain.csproj", "src/OrderingSystem.Domain/"]
COPY ["src/OrderingSystem.Infrastructure/OrderingSystem.Infrastructure.csproj", "src/OrderingSystem.Infrastructure/"]

RUN dotnet restore "src/OrderingSystem.LLMWorker/OrderingSystem.LLMWorker.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/OrderingSystem.LLMWorker"
RUN dotnet build "OrderingSystem.LLMWorker.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "OrderingSystem.LLMWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderingSystem.LLMWorker.dll"]