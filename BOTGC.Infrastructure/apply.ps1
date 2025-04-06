
[CmdletBinding()]
param
(
    [Parameter(Mandatory = $true,
               ValueFromPipeline = $true,
               HelpMessage = 'Valid environments are test, integration, preprod and production')]
    [ValidateSet('test', 'int', 'qa', 'production', 'sandbox', 'preprod')]
    [string]$env,

    [Parameter(Mandatory = $false,
               ValueFromPipeline = $true)]
    [Alias("auto-approve")]
    [string]$autoApprove,

    [Parameter(Mandatory = $false,
               ValueFromPipeline = $true,
               HelpMessage = 'Prevents the APPLY step from running')]
    [switch]$dryRun
)

function Write-ColorOutput($ForegroundColor)
{
    # save the current color
    $fc = $host.UI.RawUI.ForegroundColor

    # set the new color
    $host.UI.RawUI.ForegroundColor = $ForegroundColor

    # output
    if ($args) {
        Write-Output $args
    }
    else {
        $input | Write-Output
    }

    # restore the original color
    $host.UI.RawUI.ForegroundColor = $fc
}

$aa = ""
if ($autoApprove.ToLower() = "true") {
    $aa = "-auto-approve"
}

Write-Output "`n"

Write-ColorOutput darkgreen ("Initialising Terraform")
Write-ColorOutput darkgreen ("-------------------------------------------------------")

terraform init -backend-config="./.terraform_workspaces/env.$env/backend.tfvars" -no-color -reconfigure


# Write-ColorOutput darkgreen ("Create Workspace $env")
# Write-ColorOutput darkgreen ("-------------------------------------------------------")

# terraform workspace new $env

Write-ColorOutput darkgreen ("Switching to Environment $env")
Write-ColorOutput darkgreen ("-------------------------------------------------------")

terraform workspace select $env -no-color

Write-ColorOutput darkgreen ("Determine Plan")
Write-ColorOutput darkgreen ("-------------------------------------------------------")

terraform plan -no-color -var-file="./.terraform_workspaces/env.$env/variables.tfvars"

if (!$dryRun) {
    Write-ColorOutput darkgreen ("Applying Changes")
    Write-ColorOutput darkgreen ("-------------------------------------------------------")

    terraform apply -no-color -var-file="./.terraform_workspaces/env.$env/variables.tfvars" $aa
}
else {
    Write-ColorOutput darkgreen ("SKIPPING: Applying Changes")
    Write-ColorOutput darkgreen ("-------------------------------------------------------")
}