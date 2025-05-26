terraform {
  required_version = ">= 1.5.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = ">= 3.30.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "rg-botgc-shared"
    storage_account_name = "sabotgcmain"
    container_name       = "tfstate"
    key                  = "terraform.tfstat.svc"
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "example" {}

data "azurerm_storage_account" "services_api_sa" {
  name                = "sabotgcmain"
  resource_group_name = "rg-botgc-shared"
}

data "azurerm_storage_container" "data" {
  name                 = "data"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
}

resource "azurerm_resource_group" "services_api_rg" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = "West Europe"
}
