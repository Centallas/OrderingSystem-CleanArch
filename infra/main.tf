# 1. Define the Terraform provider requirements
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

# 2. Configure the Azure Provider
provider "azurerm" {
  features {}
}

# 3. Create the Resource Group
resource "azurerm_resource_group" "rg" {
  name     = "rg-orderingsystem-dev"
  location = "West US 3" # Switched to West US 3
}
# 4. Create the PostgreSQL Flexible Server
resource "azurerm_postgresql_flexible_server" "db_server" {
  name                   = "psql-orderingsystem-edward-dev" # Added a unique identifier
  resource_group_name    = azurerm_resource_group.rg.name
  location               = azurerm_resource_group.rg.location
  version                = "13" # Matching your typical local Docker setup
  
  # match Azure's current state
  zone = "1"
  
  administrator_login    = "psqladmin"
  administrator_password = "Password1234!" # In a real project, we'd use variables/Vault

  storage_mb = 32768
  sku_name   = "B_Standard_B1ms" # Burstable tier (cheapest for dev/labs)
}

# 5. Allow Azure services to access the database (Firewall Rule)
resource "azurerm_postgresql_flexible_server_firewall_rule" "fw" {
  name             = "allow-azure-services"
  server_id        = azurerm_postgresql_flexible_server.db_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
# 6. Create the App Service Plan (Linux)
resource "azurerm_service_plan" "app_plan" {
  name                = "plan-orderingsystem-dev"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  os_type             = "Linux"
  sku_name            = "B1" # Burstable tier (cost-effective for labs)
}

# 7. Create the Web App for the .NET 8 API
resource "azurerm_linux_web_app" "app_service" {
  name                = "app-orderingsystem-edward-dev" # Must be globally unique
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  service_plan_id     = azurerm_service_plan.app_plan.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
    always_on = false # Set to false for F1/B1 tiers to save costs
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = "Development"
    # Constructing the Postgres connection string for .NET
    "ConnectionStrings__DefaultConnection" = "Host=${azurerm_postgresql_flexible_server.db_server.fqdn};Database=postgres;Username=${azurerm_postgresql_flexible_server.db_server.administrator_login};Password=${azurerm_postgresql_flexible_server.db_server.administrator_password};SSL Mode=Require;"
  }
}
output "webapp_url" {
  value = "https://${azurerm_linux_web_app.app_service.default_hostname}"
}

output "database_hostname" {
  value = azurerm_postgresql_flexible_server.db_server.fqdn
}
# 8. Create the Azure Cache for Redis
resource "azurerm_redis_cache" "redis" {
  name                = "redis-orderingsystem-edward-dev"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  capacity            = 0
  family              = "C"
  sku_name            = "Basic"
  non_ssl_port_enabled = false  # Replaces enable_non_ssl_port
  minimum_tls_version = "1.2"
}
# 9 Create the Service Bus Namespace
resource "azurerm_servicebus_namespace" "sb" {
  name                = "sb-orderingsystem-edward-dev"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Basic" # Basic is fine for learning and cheapest
}

# 10 Create a Queue for Order Processing
resource "azurerm_servicebus_queue" "order_queue" {
  name         = "order-processing-queue"
  namespace_id = azurerm_servicebus_namespace.sb.id
}
# 11. Updated Web App Settings (Adding Redis Connection)
# Note: Usually, we update the existing resource, but I'm highlighting the new settings here:
# "Redis__ConnectionString" = "${azurerm_redis_cache.redis.hostname}:6380,password=${azurerm_redis_cache.redis.primary_access_key},ssl=True,abortConnect=False"

output "redis_hostname" {
  value = azurerm_redis_cache.redis.hostname
}
