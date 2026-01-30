resource "azurerm_storage_queue" "membership_applications_queue" {
  name                 = "membershipapplications"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
}

resource "azurerm_storage_table" "lookup_table" {
  name                 = "lookup"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
}

resource "azurerm_management_lock" "services_api_sa_cannot_delete" {
  name       = "lock-sabotgcmain-cannot-delete"
  scope      = data.azurerm_storage_account.services_api_sa.id
  lock_level = "CanNotDelete"
  notes      = "Protects sabotgcmain and all contained data (tables/queues/blobs) from accidental deletion."
}
