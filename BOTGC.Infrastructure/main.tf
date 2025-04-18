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

resource "azurerm_resource_group" "services_api_rg" {
  name     = "rg-${var.project_name}-${var.environment}"
  location = "West Europe"
}

data "azurerm_storage_account" "services_api_sa" {
  name                = "sabotgcmain"
  resource_group_name = "rg-botgc-shared"
}

data "azurerm_storage_container" "data" {
  name                 = "data"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
}

resource "azurerm_storage_queue" "membership_applications_queue" {
  name                 = "membershipapplications"
  storage_account_name = data.azurerm_storage_account.services_api_sa.name
}

resource "azurerm_service_plan" "services_api_asp" {
  name                = "asp-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  os_type             = "Linux"
  sku_name            = "B1"  
}

resource "azurerm_redis_cache" "redis" {
  name                = "redis-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  capacity            = 1
  family              = "C"
  sku_name            = "Basic"

  # Use the updated argument name
  non_ssl_port_enabled = false

  # Specify the minimum TLS version
  minimum_tls_version = "1.2"
}

data "azurerm_redis_cache" "redis" {
  name                = azurerm_redis_cache.redis.name
  resource_group_name = azurerm_resource_group.services_api_rg.name
}

resource "azurerm_linux_web_app" "services_api_app" {
  name                = "api-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  service_plan_id     = azurerm_service_plan.services_api_asp.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"                         = azurerm_application_insights.app_insights.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING"                  = azurerm_application_insights.app_insights.connection_string
    "SCM_DO_BUILD_DURING_DEPLOYMENT"                         = true
    "WEBSITE_RUN_FROM_PACKAGE"                               = "1"
    "MEMBER_ID"                                              = var.member_id
    "MEMBER_PIN"                                             = var.member_pin
    "ADMIN_PASSWORD"                                         = var.admin_password
    "DATA_CONTAINER_CONNECTION_STRING"                       = data.azurerm_storage_account.services_api_sa.primary_connection_string

    "AppSettings__TrophyFilePath"                            = "/data/trophies"

    "AppSettings__Auth__XApiKey"                             = var.x_api_key

    "AppSettings__AzureFaceApi__EndPoint"                    = "https://face-botgc-shared.cognitiveservices.azure.com/"
    "AppSettings__AzureFaceApi__SubscriptionKey"             = var.azure_face_api_key

    "AppSettings__Cache__Type"                               = "Redis"
    "AppSettings__Cache__ShortTerm_TTL_mins"                 = "10080"
    "AppSettings__Cache__LongTerm_TTL_mins"                  = "10080"
    "AppSettings__Cache__RedisCache__ConnectionString"       = data.azurerm_redis_cache.redis.primary_connection_string

    "AppSettings__GitHub__Token"                             = var.github_token
    "AppSettings__GitHub__RepoUrl"                           = "https://github.com/botgc-comms/data"
    "AppSettings__GitHub__ApiUrl"                            = "https://api.github.com/repos/botgc-comms/data"
    "AppSettings__GitHub__RawUrl"                            = "https://raw.githubusercontent.com/botgc-comms/data/master"

    "AppSettings__IG__LoginEveryNMinutes"                    = "30"
    "AppSettings__IG__BaseUrl"                               = "https://www.botgc.co.uk"
    "AppSettings__IG__MemberId"                              = var.member_id
    "AppSettings__IG__MemberPassword"                        = var.member_id
    "AppSettings__IG__AdminPassword"                         = var.admin_password

    "AppSettings__IG__Urls__JuniorMembershipReportUrl"       = "/membership_reports.php?tab=report&section=viewreport&md=b52f6bd4cf74cc5dbfd84dec616ceb42"
    "AppSettings__IG__Urls__AllCurrentMembersReportUrl"      = "/membership_reports.php?tab=report&section=viewreport&md=5d71e7119d780dba4850506f622c1cfb"
    "AppSettings__IG__Urls__MemberRoundsReportUrl"           = "/roundmgmt.php?playerid={playerId}"
    "AppSettings__IG__Urls__PlayerIdLookupReportUrl"         = "/membership_reports.php?tab=status"
    "AppSettings__IG__Urls__RoundReportUrl"                  = "/viewround.php?roundid={roundId}"
    "AppSettings__IG__Urls__MembershipReportingUrl"          = "/membership_reports.php?tab=report&section=viewreport&md=9be9f71c8988351887840f3826a552da"
    "AppSettings__IG__Urls__NewMembershipApplicationUrl"     = "/membership_addmember.php?&requestType=ajax&ajaxaction=confirmadd"
    "AppSettings__IG__Urls__TeeBookingsUrl"                  = "/teetimes.php?date={date}"
    "AppSettings__IG__Urls__UpcomingCompetitionsUrl"         = "/compdash.php?tab=competitions&requestType=ajax&ajaxaction=morecomps&status=upcoming&entrants=all&kind=all&teamsolo=all&year=all&offset=0&limit=20"
    "AppSettings__IG__Urls__ActiveCompetitionsUrl"           = "/compdash.php?tab=competitions&requestType=ajax&ajaxaction=morecomps&status=active&entrants=all&kind=all&teamsolo=all&year=all&offset=0&limit=20"
    "AppSettings__IG__Urls__CompetitionSettingsUrl"          = "/compadmin3.php?compid={compid}&tab=settings"
    "AppSettings__IG__Urls__LeaderBoardUrl"                  = "/competition.php?compid={compid}&preview=1"
    "AppSettings__IG__URLS__SecurityLogMobileOrders"         = "/log.php?search=Mobile+order&person=&start={today}&starttime=&end={today}&endtime="

    "AppSettings__Queue__ConnectionString" = data.azurerm_storage_account.services_api_sa.primary_connection_string
    "AppSettings__Queue__Name"             = azurerm_storage_queue.membership_applications_queue.name

  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_linux_web_app" "services_leaderboards_app" {
  name                = "leaderboards-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  service_plan_id     = azurerm_service_plan.services_api_asp.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"                         = azurerm_application_insights.app_insights.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING"                  = azurerm_application_insights.app_insights.connection_string
    "SCM_DO_BUILD_DURING_DEPLOYMENT"                         = true
    "WEBSITE_RUN_FROM_PACKAGE"                               = "1"
    "DATA_CONTAINER_CONNECTION_STRING"                       = data.azurerm_storage_account.services_api_sa.primary_connection_string
    "ASPNETCORE_ENVIRONMENT"                                 = "Production"

    "AppSettings__API__XApiKey"                              = var.x_api_key
    "AppSettings__API__Url"                                  = "https://${azurerm_linux_web_app.services_api_app.default_hostname}"
  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_key_vault" "services_api_kv" {
  name                = "kv-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  tenant_id           = data.azurerm_client_config.example.tenant_id
  sku_name            = "standard"
}

resource "azurerm_key_vault_access_policy" "services_api_kv_policy" {
  key_vault_id = azurerm_key_vault.services_api_kv.id
  tenant_id    = data.azurerm_client_config.example.tenant_id
  object_id    = azurerm_linux_web_app.services_api_app.identity[0].principal_id

  secret_permissions = [
    "Get",
    "List",
    "Set"
  ]
}

resource "azurerm_key_vault_access_policy" "terraform_sp_kv_policy" {
  key_vault_id = azurerm_key_vault.services_api_kv.id
  tenant_id    = data.azurerm_client_config.example.tenant_id
  object_id    = data.azurerm_client_config.example.object_id

  secret_permissions = [
    "Get",
    "List",
    "Set"
  ]
}

resource "azurerm_key_vault_secret" "member_id" {
  name         = "member-id"
  value        = var.member_id
  key_vault_id = azurerm_key_vault.services_api_kv.id
}

resource "azurerm_key_vault_secret" "member_pin" {
  name         = "member-pin"
  value        = var.member_pin
  key_vault_id = azurerm_key_vault.services_api_kv.id
}

resource "azurerm_key_vault_secret" "admin_password" {
  name         = "admin-password"
  value        = var.admin_password
  key_vault_id = azurerm_key_vault.services_api_kv.id
}

resource "azurerm_application_insights" "app_insights" {
  name                = "app-insights-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  application_type    = "web"
}

output "api_app_name" {
  value = azurerm_linux_web_app.services_api_app.name
}

output "leaderboards_app_name" {
  value = azurerm_linux_web_app.services_leaderboards_app.name
}
