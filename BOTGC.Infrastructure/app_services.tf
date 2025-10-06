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

    "AppSettings__ApplicationInsights__ConnectionString"     = azurerm_application_insights.app_insights.connection_string   

    "AppSettings__Auth__XApiKey"                             = var.x_api_key

    "AppSettings__AzureFaceApi__EndPoint"                    = "https://face-botgc-shared.cognitiveservices.azure.com/"
    "AppSettings__AzureFaceApi__SubscriptionKey"             = var.azure_face_api_key

    "AppSettings__Cache__Type"                               = "Redis"
    "AppSettings__Cache__ShortTerm_TTL_mins"                 = "15"
    "AppSettings__Cache__LongTerm_TTL_mins"                  = "10080"
    "AppSettings__Cache__RedisCache__ConnectionString"       = data.azurerm_redis_cache.redis.primary_connection_string

    "AppSettings__GitHub__Token"                             = var.github_token
    "AppSettings__GitHub__RepoUrl"                           = "https://github.com/botgc-comms/data"
    "AppSettings__GitHub__ApiUrl"                            = "https://api.github.com/repos/botgc-comms/data"
    "AppSettings__GitHub__RawUrl"                            = "https://raw.githubusercontent.com/botgc-comms/data/master"

    "AppSettings__IG__LoginEveryNMinutes"                    = "30"
    "AppSettings__IG__BaseUrl"                               = "https://www.botgc.co.uk"
    "AppSettings__IG__MemberId"                              = var.member_id
    "AppSettings__IG__MemberPassword"                        = var.member_pin
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
    "AppSettings__IG__Urls__LeaderBoardUrl"                  = "/competition.php?compid={compid}&preview=1&sort={grossOrNett}"
    "AppSettings__IG__Urls__SecurityLogMobileOrders"         = "/log.php?search=Mobile+order&person=&start={today}&starttime=&end={today}&endtime="
    "AppSettings__IG__Urls__UpdateMemberPropertiesUrl"       = "/member.php?memberid={memberid}&requestType=ajax&ajaxaction=saveparamvalue"
    "AppSettings__IG__Urls__LadyMembersReportUrl"            = "/membership_reports.php?tab=report&section=viewreport&md=5d71e7119d780dba4850506f622c1cfb"
    "AppSettings__IG__Urls__AllWaitingMembersReportUrl"      = "/membership_reports.php?tab=report&section=viewreport&md=6da7bd30935f3f5f2374aa8206cd80ec"
    "AppSettings__IG__Urls__NewMemberLookupReportUrl"        = "/membership_reports.php?tab=newmembers"
    "AppSettings__IG__Urls__HandicapIndexHistoryReportUrl"   = "/roundmgmt.php?playerid={playerId}"
    "AppSettings__IG__Urls__MembershipEventHistoryReportUrl" = "/membership_reports.php?tab=categorychanges&requestType=ajax&ajaxaction=getreport"
    "AppSettings__IG__Urls__MemberCDHLookupUrl"              = "/membership_addmember.php?&requestType=ajax&ajaxaction=cdhidlookup"
    "AppSettings__IG__Urls__CompetitionSummaryUrl"           = "/compadmin3.php?compid={compid}&tab=summary"
    "AppSettings__IG__Urls__MemberDetailsUrl"                = "/member.php?memberid={memberid}"
    "AppSettings__IG__Urls__StockItemsUrl"                   = "/tillstockcontrol.php"


    "AppSettings__Queue__ConnectionString" = data.azurerm_storage_account.services_api_sa.primary_connection_string
    "AppSettings__Queue__Name"             = azurerm_storage_queue.membership_applications_queue.name

    "AppSettings__Monday__APIKey" = var.monday_com_apikey

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

    "AppSettings__ApplicationInsights__ConnectionString"     = azurerm_application_insights.app_insights.connection_string

    "AppSettings__API__XApiKey"                              = var.x_api_key
    "AppSettings__API__Url"                                  = "https://${azurerm_linux_web_app.services_api_app.default_hostname}"
  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_linux_web_app" "services_application_form" {
  name                = "applicationform-${var.project_name}-${var.environment}"
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

    "AppSettings__ApplicationInsights__ConnectionString"     = azurerm_application_insights.app_insights.connection_string

    "AppSettings__API__XApiKey"                              = var.x_api_key
    "AppSettings__API__Url"                                  = "https://${azurerm_linux_web_app.services_api_app.default_hostname}"

    "AppSettings__GetAddressIOSettings__ApiKey"              = var.get_address_io_apikey

    "AppSettings__Monday__APIKey"                            = var.monday_com_apikey
    
    "AppSettings__RecentApplicants__SharedSecret"           = var.recent_applicants_shared_secret
    "AppSettings__RecentApplicants__AllowedReferrerHost"    = var.recent_applicants_allowed_referrer_host
    "AppSettings__RecentApplicants__TokenTtlMinutes"        = var.recent_applicants_token_ttl_minutes
    "AppSettings__RecentApplicants__CookieName"             = var.recent_applicants_cookie_name
  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_linux_web_app" "services_mgntreports_form" {
  name                = "mgntreports-${var.project_name}-${var.environment}"
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
    "AppSettings__ApplicationInsights__ConnectionString"     = azurerm_application_insights.app_insights.connection_string

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

resource "azurerm_linux_web_app" "services_wastage_app" {
  name                = "wastage-${var.project_name}-${var.environment}"
  location            = azurerm_resource_group.services_api_rg.location
  resource_group_name = azurerm_resource_group.services_api_rg.name
  service_plan_id     = azurerm_service_plan.services_api_asp.id

  site_config {
    application_stack {
      dotnet_version = "8.0"
    }

    websockets_enabled = true
    http2_enabled      = true
    always_on          = true
  }

  app_settings = {
    "APPINSIGHTS_INSTRUMENTATIONKEY"                      = azurerm_application_insights.app_insights.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING"               = azurerm_application_insights.app_insights.connection_string
    "AppSettings__ApplicationInsights__ConnectionString"  = azurerm_application_insights.app_insights.connection_string

    "SCM_DO_BUILD_DURING_DEPLOYMENT"                      = true
    "WEBSITE_RUN_FROM_PACKAGE"                            = "1"
    "DATA_CONTAINER_CONNECTION_STRING"                    = data.azurerm_storage_account.services_api_sa.primary_connection_string
    "ASPNETCORE_ENVIRONMENT"                              = "Production"

    "AppSettings__Redis__ConnectionString"                = data.azurerm_redis_cache.redis.primary_connection_string

    "AppSettings__API__XApiKey"                           = var.x_api_key
    "AppSettings__API__Url"                               = "https://${azurerm_linux_web_app.services_api_app.default_hostname}/"

    "AppSettings__Access__SharedSecret"                   = var.wastage_secret
    "AppSettings__Access__CookieName"                     = "post_access"
    "AppSettings__Access__CookieTtlDays"                  = "30"

    "AppSettings__Ngrok__Enable"                          = tostring(var.ngrok_enable)
    "AppSettings__Ngrok__Port"                            = tostring(var.ngrok_port)
    "AppSettings__Ngrok__Region"                          = var.ngrok_region
    "AppSettings__Ngrok__ApiToken"                        = var.ngrok_api_token
  }

  identity {
    type = "SystemAssigned"
  }
}
