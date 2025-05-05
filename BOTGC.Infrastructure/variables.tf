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

