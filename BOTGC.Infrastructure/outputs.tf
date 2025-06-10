output "api_app_name" {
  value = azurerm_linux_web_app.services_api_app.name
}

output "leaderboards_app_name" {
  value = azurerm_linux_web_app.services_leaderboards_app.name
}

output "applicationform_app_name" {
  value = azurerm_linux_web_app.services_application_form.name
}

output "managementreports_app_name" {
  value = azurerm_linux_web_app.services_mgntreports_form.name
}

output "cdn_base_url" {
  value = "https://${azurerm_cdn_endpoint.js_delivery.fqdn}"
}

output "membership_embed_js_url" {
  value = "https://${azurerm_cdn_endpoint.js_delivery.fqdn}/js/membership-form-embed.js"
}