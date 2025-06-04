resource "azurerm_application_insights" "app_insights" {
  name                = "app-insights-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  application_type    = "web"
  workspace_id        = azurerm_log_analytics_workspace.log_analytics.id
}

resource "azurerm_monitor_diagnostic_setting" "app_insights_diag" {
  name                       = "diag-${azurerm_application_insights.app_insights.name}"
  target_resource_id         = azurerm_application_insights.app_insights.id
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id

  enabled_log {
    category = "AppRequests"
  }

  enabled_log {
    category = "AppTraces"
  }

  enabled_log {
    category = "AppExceptions"
  }

  enabled_log {
    category = "AppMetrics"
  }

  enabled_log {
    category = "PerformanceCounters"
  }

  enabled_log {
    category = "Dependencies"
  }

  metric {
    category = "AllMetrics"
    enabled  = true
  }
}

