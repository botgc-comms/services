resource "azurerm_application_insights" "app_insights" {
  name                = "app-insights-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.log_analytics.id
}

resource "azurerm_monitor_diagnostic_setting" "app_insights_diag" {
  name                       = "appinsights-diagnostics"
  target_resource_id         = azurerm_application_insights.app_insights.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.log_analytics.id

  log {
    category = "Request"
    enabled  = true
  }

  log {
    category = "Trace"
    enabled  = true
  }

  log {
    category = "Exception"
    enabled  = true
  }

  log {
    category = "Dependency"
    enabled  = true
  }

  log {
    category = "Availability"
    enabled  = true
  }

  log {
    category = "PerformanceCounters"
    enabled  = true
  }

  metric {
    category = "AllMetrics"
    enabled  = true
  }
}

