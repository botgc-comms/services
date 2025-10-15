resource "azurerm_storage_queue" "membership_applications_queue" {
  name                 = "membershipapplications"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
}

resource "azurerm_storage_table" "lookup_table" {
  name                 = "lookup"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
  resource_group_name  = data.azurerm_resource_group.services_api_rg.name
}
