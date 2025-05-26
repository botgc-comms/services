resource "azurerm_redis_cache" "redis" {
  name                = "redis-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  capacity            = 1
  family              = "C"
  sku_name            = "Basic"

  # Use the updated argument name
  non_ssl_port_enabled = false

  # Specify the minimum TLS version
  minimum_tls_version = "1.2"
}

data "azurerm_redis_cache" "redis" {
  name                = azurerm_redis_cache.redis.name
  resource_group_name = azurerm_resource_group.services_api_rg.name
}