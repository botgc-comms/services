terraform {
  backend "azurerm" {
    resource_group_name  = "rg-botgc-shared"
    storage_account_name = "sabotgcmain"
    container_name       = "tfstate"
    key                  = "terraform.tfstat.svc"
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "example" {}

resource "azurerm_resource_group" "services_api_rg" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = "West Europe"
}

data "azurerm_storage_account" "services_api_sa" {
  name                = "sabotgcmain"
  resource_group_name = "rg-botgc-shared"
}

resource "azurerm_storage_container" "data" {
  name                  = "data"
  storage_account_name  = data.azurerm_storage_account.services_api_sa.name
  container_access_type = "private"
}

resource "azurerm_service_plan" "services_api_asp" {
  name                = "asp-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_linux_web_app" "services_api_app" {
  name                = "api-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  service_plan_id     = azurerm_service_plan.services_api_asp.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"        = azurerm_application_insights.app_insights.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.app_insights.connection_string
    "SCM_DO_BUILD_DURING_DEPLOYMENT"        = true
    "WEBSITE_RUN_FROM_PACKAGE"              = "1"
    "MEMBER_ID"                             = var.member_id
    "MEMBER_PIN"                            = var.member_pin
    "ADMIN_PASSWORD"                        = var.admin_password
    "DATA_CONTAINER_CONNECTION_STRING"      = data.azurerm_storage_account.services_api_sa.primary_connection_string
  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_key_vault" "services_api_kv" {
  name                = "kv-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  tenant_id           = data.azurerm_client_config.example.tenant_id
  sku_name            = "standard"
}

resource "azurerm_key_vault_access_policy" "services_api_kv_policy" {
  key_vault_id = azurerm_key_vault.services_api_kv.id
  tenant_id    = data.azurerm_client_config.example.tenant_id
  object_id    = azurerm_linux_web_app.services_api_app.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List",
    "Set"
  ]
}

resource "azurerm_key_vault_access_policy" "terraform_sp_kv_policy" {
  key_vault_id = azurerm_key_vault.services_api_kv.id
  tenant_id    = data.azurerm_client_config.example.tenant_id
  object_id    = data.azurerm_client_config.example.object_id

  secret_permissions = [
    "Get",
    "List",
    "Set"
  ]
}

resource "azurerm_key_vault_secret" "member_id" {
  name         = "member-id"
  value        = var.member_id
  key_vault_id = azurerm_key_vault.services_api_kv.id
}

resource "azurerm_key_vault_secret" "member_pin" {
  name         = "member-pin"
  value        = var.member_pin
  key_vault_id = azurerm_key_vault.services_api_kv.id
}

resource "azurerm_key_vault_secret" "admin_password" {
  name         = "admin-password"
  value        = var.admin_password
  key_vault_id = azurerm_key_vault.services_api_kv.id
}

resource "azurerm_application_insights" "app_insights" {
  name                = "app-insights-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  application_type    = "web"
}

output "web_app_name" {
  value = azurerm_linux_web_app.services_api_app.name
}
