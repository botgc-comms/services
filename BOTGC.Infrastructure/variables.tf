variable "project_name" {
  type        = string
  description = "The name of the project"
}

variable "environment" {
  type        = string
  description = "The environment (e.g., dev, prd)"
  default     = "prd"
}

variable "member_id" {
  type    = string
  default = ""
}

variable "member_pin" {
  type    = string
  default = ""
}

variable "admin_password" {
  type    = string
  default = ""
}

variable "azure_face_api_key" {
  type    = string
  default = ""
}

variable "x_api_key" {
  type    = string
  default = ""
}

variable "github_token" {
  type    = string
  default = ""
}

variable "get_address_io_apikey" {
  type    = string
  default = ""
}

variable "monday_com_apikey" {
  type    = string
  default = ""
}

variable "recent_applicants_shared_secret" {
  type      = string
  sensitive = true
}

variable "recent_applicants_allowed_referrer_host" {
  type    = string
  default = "www.botgc.co.uk"
}

variable "recent_applicants_token_ttl_minutes" {
  type    = number
  default = 129600
}

variable "recent_applicants_cookie_name" {
  type    = string
  default = "ra_tok"
}

variable "ngrok_enable" {
  type    = bool
  default = false
}

variable "ngrok_port" {
  type    = number
  default = 0
}

variable "ngrok_region" {
  type    = string
  default = ""
}

variable "ngrok_api_token" {
  type      = string
  sensitive = true
  default   = ""
}

variable "wastage_secret" {
  type      = string
  sensitive = true
  default   = ""
}
