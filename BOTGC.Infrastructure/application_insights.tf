resource "azurerm_application_insights" "app_insights" {
  name                = "app-insights-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  application_type    = "web"
}
