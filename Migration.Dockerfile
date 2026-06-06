# Stage 1: Build the EF Core Migration Bundle
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS migration-build
WORKDIR /src

# 1. Install the dotnet-ef tool locally in the container
RUN dotnet tool install --global dotnet-ef --version 9.0.*
ENV PATH="$PATH:/root/.dotnet/tools"

# 2. Copy all files to let NuGet inspect the entire dependency tree
COPY . .

# Add the design package matching your core project version safely
RUN dotnet add src/OrderingSystem.Infrastructure/OrderingSystem.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 9.0.0

# Restore explicitly for the Infrastructure project with full source context
RUN dotnet restore src/OrderingSystem.Infrastructure/OrderingSystem.Infrastructure.csproj

# 3. Generate the self-contained executable bundle for Linux
# We change the working directory to the root to resolve cross-project paths cleanly
WORKDIR /src
RUN dotnet ef migrations bundle \
    --self-contained \
    -r linux-x64 \
    -o /app/efbundle \
    --project src/OrderingSystem.Infrastructure/OrderingSystem.Infrastructure.csproj \
    --startup-project src/OrderingSystem.Api/OrderingSystem.Api.csproj \
    --context ApplicationDbContext
    
# Stage 2: Create the minimal runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-noble AS final
WORKDIR /app

# Copy the self-contained binary from the build stage
COPY --from=migration-build /app/efbundle .

# Ensure the binary has execution permissions
RUN chmod +x ./efbundle

# Set the entrypoint to execute the bundle
ENTRYPOINT ["./efbundle"]    