# Database Migration Instructions

Since the environment restricts running `dotnet ef` directly, please run the following commands in your local PowerShell to create and apply the database migrations.

### Prerequisites
Ensure you have the `dotnet-ef` tool installed globally:
```powershell
dotnet tool install --global dotnet-ef
```

### 1. Create the Initial Migration
Run this command from the solution root to generate the migration files:
```powershell
dotnet ef migrations add InitialCreate --project src/OrderingSystem.Infrastructure --startup-project src/OrderingSystem.Api --output-dir Persistence/Migrations
```

### 2. Apply Migration to Database
Run this command to create the database and the `Orders` table:
```powershell
dotnet ef database update --project src/OrderingSystem.Infrastructure --startup-project src/OrderingSystem.Api
```

### Notes
- Make sure your PostgreSQL server is running.
- The connection string is currently configured in `src/OrderingSystem.Api/Program.cs` as:
  `Host=localhost;Database=OrderingDb;Username=postgres;Password=password`
- You can override this by adding a `DefaultConnection` to `appsettings.json` in the API project.
