resource "azurerm_cdn_profile" "main" {
  name                = "cdn-${var.project_name}-${var.environment}"
  resource_group_name = azurerm_resource_group.services_api_rg.name
  location            = azurerm_resource_group.services_api_rg.location
  sku                 = "Standard_Microsoft"
}

resource "azurerm_cdn_endpoint" "js_delivery" {
  name                = "cdnjs-${var.project_name}-${var.environment}"
  profile_name        = azurerm_cdn_profile.main.name
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  origin_host_header  = data.azurerm_storage_account.services_api_sa.primary_web_host
  is_http_allowed     = false
  is_https_allowed    = true

  origin {
    name      = "blobstorageorigin"
    host_name = data.azurerm_storage_account.services_api_sa.primary_web_host
  }
}

resource "azurerm_storage_container" "web" {
  name                  = "$web"
  storage_account_name  = data.azurerm_storage_account.services_api_sa.name
  container_access_type = "blob"
}