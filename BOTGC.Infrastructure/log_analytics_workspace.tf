resource "azurerm_log_analytics_workspace" "log_analytics" {
  name                = "law-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}
