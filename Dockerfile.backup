# Stage 1: Build & Restore
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution file and project files
COPY ["OrderingSystem.sln", "./"]
COPY ["src/", "src/"]

# Restore dependencies for the whole solution
RUN dotnet restore "OrderingSystem.sln"

# Build the API project
WORKDIR "/src/src/OrderingSystem.Api"
RUN dotnet build "OrderingSystem.Api.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "OrderingSystem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Build final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderingSystem.Api.dll"]