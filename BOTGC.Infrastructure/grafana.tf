
resource "azurerm_dashboard_grafana" "grafana" {
  name                  = "gfn-${var.project_name}-${var.environment}"
  location              = azurerm_resource_group.services_api_rg.location
  resource_group_name   = azurerm_resource_group.services_api_rg.name
  grafana_major_version = 10

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_role_assignment" "grafana_reader_rg" {
  scope                = azurerm_resource_group.services_api_rg.id
  role_definition_name = "Reader"
  principal_id         = azurerm_dashboard_grafana.grafana.identity[0].principal_id
}

resource "azurerm_role_assignment" "grafana_logs" {
  scope                = azurerm_log_analytics_workspace.log_analytics.id
  role_definition_name = "Log Analytics Reader"
  principal_id         = azurerm_dashboard_grafana.grafana.identity[0].principal_id
}

resource "azurerm_role_assignment" "grafana_monitor" {
  scope                = azurerm_resource_group.services_api_rg.id
  role_definition_name = "Monitoring Reader"
  principal_id         = azurerm_dashboard_grafana.grafana.identity[0].principal_id
}
