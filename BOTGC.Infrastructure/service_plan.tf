resource "azurerm_service_plan" "services_api_asp" {
  name                = "asp-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  os_type             = "Linux"
  sku_name            = "S1"
}
