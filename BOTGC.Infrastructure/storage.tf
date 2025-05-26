resource "azurerm_storage_queue" "membership_applications_queue" {
  name                 = "membershipapplications"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
}
