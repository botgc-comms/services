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
