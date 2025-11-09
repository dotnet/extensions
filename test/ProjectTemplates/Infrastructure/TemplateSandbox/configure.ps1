<#
.SYNOPSIS
    Interactive configuration script for .NET AI chat web project templates.

.DESCRIPTION
    This script scans all generated projects in the output folder, extracts
    dotnet user-secrets commands from README files, and interactively prompts
    the user to configure secrets for each unique configuration key.

.PARAMETER SkipConfigured
    Skip secrets that are already configured in all affected projects.

.PARAMETER Force
    Force re-configuration of secrets even if they already exist.

.PARAMETER Clear
    Clear all user secrets from all projects and exit.

.EXAMPLE
    .\configure.ps1
    Run interactively, prompting for all secrets including those already configured.

.EXAMPLE
    .\configure.ps1 -SkipConfigured
    Skip secrets that are already fully configured across all affected projects.

.EXAMPLE
    .\configure.ps1 -Force
    Force re-configuration of all secrets, ignoring existing values.

.EXAMPLE
    .\configure.ps1 -Clear
    Clear all user secrets from all projects.
#>

#Requires -Version 5.1

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$SkipConfigured,
    
    [Parameter()]
    [switch]$Force,
    
    [Parameter()]
    [switch]$Clear
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Script configuration
$OutputFolder = $PSScriptRoot

# Global cache for user secrets to avoid repeated dotnet CLI calls
$script:UserSecretsCache = @{}

#region Helper Functions

function Write-Header {
    param([string]$Message)
    Write-Host "`n$('=' * 80)" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "$('=' * 80)" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Write-Warning2 {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error2 {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-SecretPrompt {
    param([string]$Message)
    Write-Host "`n🔑 $Message" -ForegroundColor Magenta
}

#endregion

#region Main Functions

function Get-ProjectInfo {
    <#
    .SYNOPSIS
        Scans output folder for all projects with README.md files.
    #>
    [CmdletBinding()]
    param()

    Write-Info "Scanning for projects in: $OutputFolder"
    
    if (-not (Test-Path $OutputFolder)) {
        Write-Error2 "Output folder not found: $OutputFolder"
        return @()
    }

    $projects = @()
    
    # Find all README.md files
    $readmeFiles = Get-ChildItem -Path $OutputFolder -Filter 'README.md' -Recurse -File
    
    foreach ($readme in $readmeFiles) {
        $projectDir = $readme.Directory.FullName
        $argsFile = Join-Path $projectDir 'ARGS.txt'
        
        # Skip if no ARGS.txt file (not a generated project)
        if (-not (Test-Path $argsFile)) {
            continue
        }

        $projectName = $readme.Directory.Name
        $args = (Get-Content $argsFile -Raw).Trim()
        
        $projects += [PSCustomObject]@{
            ProjectName = $projectName
            ProjectPath = $projectDir
            ReadmePath  = $readme.FullName
            Args        = $args
        }
    }
    
    Write-Info "Found $($projects.Count) projects"
    return $projects
}

function Get-UserSecretsCommands {
    <#
    .SYNOPSIS
        Extracts all dotnet user-secrets set commands from README files.
    .OUTPUTS
        Returns a hashtable with two keys:
        - Secrets: Array of grouped secret configurations
        - ProjectSecrets: Hashtable mapping project paths to their required secret keys
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject[]]$Projects
    )

    $secretsData = @{}
    $projectSecrets = @{}
    
    foreach ($project in $Projects) {
        $content = Get-Content $project.ReadmePath -Raw
        
        # Track all secret keys required by this project
        $projectKey = $project.ProjectPath
        $projectSecrets[$projectKey] = @()
        
        # Find all dotnet user-secrets set commands
        # Pattern matches: dotnet user-secrets set KEY "VALUE" or KEY VALUE
        $pattern = 'dotnet\s+user-secrets\s+set\s+([^\s"]+)\s+(.+?)(?=\r?\n|$)'
        $regexMatches = [regex]::Matches($content, $pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
        
        foreach ($match in $regexMatches) {
            $key = $match.Groups[1].Value.Trim()
            $valuePattern = $match.Groups[2].Value.Trim()
            
            # Track this secret key for the project
            if ($projectSecrets[$projectKey] -notcontains $key) {
                $projectSecrets[$projectKey] += $key
            }
            
            # Clean up the value pattern (remove quotes if they're part of the pattern)
            $valuePattern = $valuePattern -replace '^"(.+)"$', '$1'
            
            # Determine the working directory for the secret command
            # For Aspire projects, check if the command specifies a subdirectory
            $workingDir = $null
            if ($content -match "cd\s+([^\r\n]+)\s+dotnet\s+user-secrets\s+set\s+$([regex]::Escape($key))") {
                $cdPath = $Matches[1].Trim()
                # Handle relative paths like "<<your-project-directory>>" or actual folder names
                if ($cdPath -notmatch '<<|>>') {
                    $workingDir = $cdPath
                }
            }
            
            # Create a unique key for this secret configuration
            $secretKey = "$key|$valuePattern"
            
            if (-not $secretsData.ContainsKey($secretKey)) {
                $secretsData[$secretKey] = [PSCustomObject]@{
                    Key          = $key
                    ValuePattern = $valuePattern
                    WorkingDir   = $workingDir
                    Projects     = @()
                }
            }
            
            $secretsData[$secretKey].Projects += [PSCustomObject]@{
                ProjectName = $project.ProjectName
                ProjectPath = $project.ProjectPath
                Args        = $project.Args
                WorkingDir  = $workingDir
            }
        }
    }
    
    return @{
        Secrets        = ($secretsData.Values | Sort-Object -Property Key)
        ProjectSecrets = $projectSecrets
    }
}

function Set-UserSecret {
    <#
    .SYNOPSIS
        Sets a user secret for a specific project.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath,
        
        [Parameter(Mandatory)]
        [string]$Key,
        
        [Parameter(Mandatory)]
        [string]$Value,
        
        [Parameter()]
        [string]$WorkingDir
    )

    try {
        # Determine the actual working directory
        $actualWorkingDir = $ProjectPath
        if ($WorkingDir) {
            $actualWorkingDir = Join-Path $ProjectPath $WorkingDir
        }
        
        if (-not (Test-Path $actualWorkingDir)) {
            Write-Error2 "Working directory not found: $actualWorkingDir"
            return $false
        }

        # Find the project file (.csproj or .fsproj)
        $projectFiles = @(Get-ChildItem -Path $actualWorkingDir -Filter '*.csproj' -File -ErrorAction SilentlyContinue)
        if ($projectFiles.Count -eq 0) {
            $projectFiles = @(Get-ChildItem -Path $actualWorkingDir -Filter '*.fsproj' -File -ErrorAction SilentlyContinue)
        }
        
        if ($projectFiles.Count -eq 0) {
            Write-Error2 "No project file found in: $actualWorkingDir"
            return $false
        }

        # Run dotnet user-secrets set
        $result = & dotnet user-secrets set $Key $Value --project $projectFiles[0].FullName 2>&1
        
        if ($LASTEXITCODE -ne 0) {
            Write-Error2 "Failed to set secret: $result"
            return $false
        }
        
        return $true
    }
    catch {
        Write-Error2 "Error setting secret: $_"
        return $false
    }
}

function Get-UserSecret {
    <#
    .SYNOPSIS
        Gets a user secret value for a specific project using lazy-loaded cached data.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath,
        
        [Parameter(Mandatory)]
        [string]$Key,
        
        [Parameter()]
        [string]$WorkingDir
    )

    # Build cache key (actual working directory)
    $actualWorkingDir = $ProjectPath
    if ($WorkingDir) {
        $actualWorkingDir = Join-Path $ProjectPath $WorkingDir
    }
    
    # Lazy load: If not cached yet, load it now
    if (-not $script:UserSecretsCache.ContainsKey($actualWorkingDir)) {
        # Show progress message
        $displayPath = if ($WorkingDir) {
            # For Aspire projects, show "ParentFolder/SubFolder"
            $parentName = Split-Path $ProjectPath -Leaf
            "$parentName/$WorkingDir"
        } else {
            # For basic projects, just show the project name
            Split-Path $actualWorkingDir -Leaf
        }
        Write-Host "  ⏳ Loading secrets from $displayPath..." -ForegroundColor DarkGray
        
        # Load secrets for this specific project
        try {
            if (-not (Test-Path $actualWorkingDir)) {
                $script:UserSecretsCache[$actualWorkingDir] = @{}
                return $null
            }

            # Find the project file
            $projectFiles = @(Get-ChildItem -Path $actualWorkingDir -Filter '*.csproj' -File -ErrorAction SilentlyContinue)
            if ($projectFiles.Count -eq 0) {
                $projectFiles = @(Get-ChildItem -Path $actualWorkingDir -Filter '*.fsproj' -File -ErrorAction SilentlyContinue)
            }
            
            if ($projectFiles.Count -eq 0) {
                $script:UserSecretsCache[$actualWorkingDir] = @{}
                return $null
            }

            # Run dotnet user-secrets list
            $result = & dotnet user-secrets list --project $projectFiles[0].FullName 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                # Parse the output into a hashtable
                $secrets = @{}
                foreach ($line in $result) {
                    # Format is: "Key = Value" (equals sign is the delimiter, not colon)
                    # Keys can contain colons (e.g., "AzureAISearch:Endpoint")
                    if ($line -match '^(.+?)\s*=\s*(.+)$') {
                        $secretKey = $Matches[1].Trim()
                        $value = $Matches[2].Trim()
                        $secrets[$secretKey] = $value
                    }
                }
                
                # Cache the secrets
                $script:UserSecretsCache[$actualWorkingDir] = $secrets
                
                # Debug output
                if ($secrets.Count -gt 0) {
                    Write-Verbose "Loaded $($secrets.Count) secret(s) from $actualWorkingDir"
                }
            }
            else {
                # No secrets or error - cache empty hashtable
                $script:UserSecretsCache[$actualWorkingDir] = @{}
            }
        }
        catch {
            # Cache empty hashtable on error
            $script:UserSecretsCache[$actualWorkingDir] = @{}
        }
    }
    
    # Return the value from cache
    $cachedSecrets = $script:UserSecretsCache[$actualWorkingDir]
    if ($cachedSecrets.ContainsKey($Key)) {
        return $cachedSecrets[$Key]
    }
    return $null
}

function Get-ExistingSecretConfiguration {
    <#
    .SYNOPSIS
        Checks existing secret configuration across all affected projects.
    .OUTPUTS
        Returns a hashtable with configuration status and existing values.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject]$Secret,
        
        [Parameter()]
        [switch]$Force
    )

    # If Force mode, skip checking existing configuration
    if ($Force) {
        return @{
            ConfiguredCount = 0
            TotalProjects   = $Secret.Projects.Count
            IsFullyConfigured = $false
            IsPartiallyConfigured = $false
            IsNotConfigured = $true
            ExistingValues = @{}
            UniqueValues = @()
        }
    }

    $existingValues = @{}
    $configuredCount = 0
    
    foreach ($project in $Secret.Projects) {
        $existingValue = Get-UserSecret -ProjectPath $project.ProjectPath `
                                        -Key $Secret.Key `
                                        -WorkingDir $project.WorkingDir
        
        if ($null -ne $existingValue) {
            $configuredCount++
            $existingValues[$project.ProjectName] = $existingValue
            Write-Verbose "Project $($project.ProjectName): $($Secret.Key) = $existingValue"
        }
        else {
            $existingValues[$project.ProjectName] = $null
            Write-Verbose "Project $($project.ProjectName): $($Secret.Key) = (not set)"
        }
    }
    
    $totalProjects = $Secret.Projects.Count
    $uniqueValues = @($existingValues.Values | Where-Object { $null -ne $_ } | Select-Object -Unique)
    
    # Fully configured means: all projects have a value AND they all have the SAME value
    $isFullyConfigured = ($configuredCount -eq $totalProjects) -and ($uniqueValues.Count -eq 1)
    
    # Partially configured means: some projects have values, OR all have values but they're different
    $isPartiallyConfigured = (($configuredCount -gt 0 -and $configuredCount -lt $totalProjects) -or 
                              ($configuredCount -eq $totalProjects -and $uniqueValues.Count -gt 1))
    
    Write-Verbose "Secret '$($Secret.Key)': $configuredCount/$totalProjects configured, $($uniqueValues.Count) unique value(s), IsFullyConfigured=$isFullyConfigured, IsPartiallyConfigured=$isPartiallyConfigured"
    
    return @{
        ConfiguredCount = $configuredCount
        TotalProjects   = $totalProjects
        IsFullyConfigured = $isFullyConfigured
        IsPartiallyConfigured = $isPartiallyConfigured
        IsNotConfigured = ($configuredCount -eq 0)
        ExistingValues = $existingValues
        UniqueValues = $uniqueValues
    }
}

function Invoke-InteractiveConfiguration {
    <#
    .SYNOPSIS
        Prompts user for each unique secret and configures all affected projects.
    .OUTPUTS
        Returns a hashtable tracking which projects were successfully configured for each secret.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [PSCustomObject[]]$Secrets,
        
        [Parameter()]
        [switch]$SkipConfigured,
        
        [Parameter()]
        [switch]$Force
    )

    $totalSecrets = $Secrets.Count
    $currentSecret = 0
    $skippedConfigured = 0
    
    # Track configuration results per project
    $projectConfigResults = @{}
    
    Write-Header "Interactive Secret Configuration"
    Write-Info "Found $totalSecrets unique secret(s) to configure"
    
    if ($SkipConfigured) {
        Write-Info "Mode: Skip fully-configured secrets"
    }
    elseif ($Force) {
        Write-Info "Mode: Force re-configuration of all secrets (skipping existing value checks)"
    }
    
    foreach ($secret in $Secrets) {
        $currentSecret++
        
        # Check existing configuration (will skip in Force mode)
        $existingConfig = Get-ExistingSecretConfiguration -Secret $secret -Force:$Force
        
        # Handle fully configured secrets
        if ($existingConfig.IsFullyConfigured -and -not $Force) {
            if ($SkipConfigured) {
                $skippedConfigured++
                Write-Info "[$currentSecret/$totalSecrets] Skipping $($secret.Key) (already configured in all $($existingConfig.TotalProjects) project(s))"
                
                # Mark as already configured (success)
                foreach ($project in $secret.Projects) {
                    $projectKey = $project.ProjectPath
                    if (-not $projectConfigResults.ContainsKey($projectKey)) {
                        $projectConfigResults[$projectKey] = @{
                            Project = $project
                            Skipped = @()
                            Failed  = @()
                            Success = @()
                        }
                    }
                    $projectConfigResults[$projectKey].Success += $secret.Key
                }
                continue
            }
            else {
                # Show existing configuration and ask if user wants to update
                Write-SecretPrompt "[$currentSecret/$totalSecrets] dotnet user-secrets set $($secret.Key) `"$($secret.ValuePattern)`""
                Write-Host "`n✓ Already configured in all $($existingConfig.TotalProjects) project(s)" -ForegroundColor Green
                
                $uniqueValues = @($existingConfig.UniqueValues)
                if ($uniqueValues.Count -eq 1) {
                    Write-Host "  Current value: $($uniqueValues[0])" -ForegroundColor DarkGray
                }
                
                Write-Host "`n➡️  Update configuration? (y/N): " -ForegroundColor Cyan -NoNewline
                $response = Read-Host
                
                if ($response -ne 'y' -and $response -ne 'Y') {
                    Write-Info "Keeping existing configuration"
                    # Mark as already configured (success)
                    foreach ($project in $secret.Projects) {
                        $projectKey = $project.ProjectPath
                        if (-not $projectConfigResults.ContainsKey($projectKey)) {
                            $projectConfigResults[$projectKey] = @{
                                Project = $project
                                Skipped = @()
                                Failed  = @()
                                Success = @()
                            }
                        }
                        $projectConfigResults[$projectKey].Success += $secret.Key
                    }
                    continue
                }
            }
        }
        
        # Handle partial configuration
        if ($existingConfig.IsPartiallyConfigured -and -not $Force) {
            Write-SecretPrompt "[$currentSecret/$totalSecrets] dotnet user-secrets set $($secret.Key) `"$($secret.ValuePattern)`""
            Write-Host "`n⚠️  Partial configuration detected:" -ForegroundColor Yellow
            Write-Host "  Configured: $($existingConfig.ConfiguredCount)/$($existingConfig.TotalProjects) projects" -ForegroundColor Yellow
            
            # Show projects and their current values
            Write-Host "`nCurrent values:" -ForegroundColor Yellow
            $valueIndex = 1
            $valueToProjects = @{}
            
            foreach ($kvp in $existingConfig.ExistingValues.GetEnumerator()) {
                $projectName = $kvp.Key
                $value = $kvp.Value
                
                if ($null -eq $value) {
                    Write-Host "  - $projectName : (not set)" -ForegroundColor DarkGray
                }
                else {
                    Write-Host "  - $projectName : $value" -ForegroundColor Gray
                    if (-not $valueToProjects.ContainsKey($value)) {
                        $valueToProjects[$value] = @{
                            Index = $valueIndex++
                            Projects = @()
                        }
                    }
                    $valueToProjects[$value].Projects += $projectName
                }
            }
            
            # Present options
            Write-Host "`nOptions:" -ForegroundColor Cyan
            foreach ($kvp in $valueToProjects.GetEnumerator()) {
                $value = $kvp.Key
                $info = $kvp.Value
                Write-Host "  [$($info.Index)] Use '$value' for all projects ($($info.Projects.Count) currently using this)" -ForegroundColor Cyan
            }
            Write-Host "  [N] Enter a new value for all projects" -ForegroundColor Cyan
            Write-Host "  [S] Skip / leave as-is" -ForegroundColor Cyan
            
            Write-Host "`n➡️  Select option: " -ForegroundColor Cyan -NoNewline
            $choice = Read-Host
            
            $valueToUse = $null
            
            # Parse the choice
            if ($choice -eq 'S' -or $choice -eq 's' -or [string]::IsNullOrWhiteSpace($choice)) {
                Write-Warning2 "Skipped - keeping existing values"
                foreach ($project in $secret.Projects) {
                    $projectKey = $project.ProjectPath
                    if (-not $projectConfigResults.ContainsKey($projectKey)) {
                        $projectConfigResults[$projectKey] = @{
                            Project = $project
                            Skipped = @()
                            Failed  = @()
                            Success = @()
                        }
                    }
                    # If the project already has this secret, count it as success; otherwise skipped
                    if ($null -ne $existingConfig.ExistingValues[$project.ProjectName]) {
                        $projectConfigResults[$projectKey].Success += $secret.Key
                    }
                    else {
                        $projectConfigResults[$projectKey].Skipped += $secret.Key
                    }
                }
                continue
            }
            elseif ($choice -eq 'N' -or $choice -eq 'n') {
                Write-Host "➡️  Enter new value: " -ForegroundColor Cyan -NoNewline
                $valueToUse = Read-Host
                
                if ([string]::IsNullOrWhiteSpace($valueToUse)) {
                    Write-Warning2 "Skipped - no value entered"
                    foreach ($project in $secret.Projects) {
                        $projectKey = $project.ProjectPath
                        if (-not $projectConfigResults.ContainsKey($projectKey)) {
                            $projectConfigResults[$projectKey] = @{
                                Project = $project
                                Skipped = @()
                                Failed  = @()
                                Success = @()
                            }
                        }
                        $projectConfigResults[$projectKey].Skipped += $secret.Key
                    }
                    continue
                }
            }
            else {
                # Try to parse as a number
                $selectedIndex = 0
                if ([int]::TryParse($choice, [ref]$selectedIndex)) {
                    $selectedValue = $valueToProjects.GetEnumerator() | Where-Object { $_.Value.Index -eq $selectedIndex } | Select-Object -First 1
                    if ($selectedValue) {
                        $valueToUse = $selectedValue.Key
                    }
                    else {
                        Write-Warning2 "Invalid option - skipping"
                        foreach ($project in $secret.Projects) {
                            $projectKey = $project.ProjectPath
                            if (-not $projectConfigResults.ContainsKey($projectKey)) {
                                $projectConfigResults[$projectKey] = @{
                                    Project = $project
                                    Skipped = @()
                                    Failed  = @()
                                    Success = @()
                                }
                            }
                            $projectConfigResults[$projectKey].Skipped += $secret.Key
                        }
                        continue
                    }
                }
                else {
                    Write-Warning2 "Invalid option - skipping"
                    foreach ($project in $secret.Projects) {
                        $projectKey = $project.ProjectPath
                        if (-not $projectConfigResults.ContainsKey($projectKey)) {
                            $projectConfigResults[$projectKey] = @{
                                Project = $project
                                Skipped = @()
                                Failed  = @()
                                Success = @()
                            }
                        }
                        $projectConfigResults[$projectKey].Skipped += $secret.Key
                    }
                    continue
                }
            }
            
            # Configure with selected value
            Write-Host "⌛ Configuring all projects with selected value..." -ForegroundColor Yellow
            
            $successCount = 0
            $failCount = 0
            
            foreach ($project in $secret.Projects) {
                $projectKey = $project.ProjectPath
                if (-not $projectConfigResults.ContainsKey($projectKey)) {
                    $projectConfigResults[$projectKey] = @{
                        Project = $project
                        Skipped = @()
                        Failed  = @()
                        Success = @()
                    }
                }
                
                $success = Set-UserSecret -ProjectPath $project.ProjectPath `
                                         -Key $secret.Key `
                                         -Value $valueToUse `
                                         -WorkingDir $project.WorkingDir
                
                if ($success) {
                    $successCount++
                    $projectConfigResults[$projectKey].Success += $secret.Key
                }
                else {
                    $failCount++
                    $projectConfigResults[$projectKey].Failed += $secret.Key
                }
            }
            
            if ($failCount -eq 0) {
                Write-Success "Successfully configured $successCount project(s)"
            }
            else {
                Write-Warning2 "Configured $successCount project(s), $failCount failed"
            }
            
            continue
        }
        
        # Handle not configured or force mode
        Write-SecretPrompt "[$currentSecret/$totalSecrets] dotnet user-secrets set $($secret.Key) `"$($secret.ValuePattern)`""
        
        Write-Host "`nAffected projects:" -ForegroundColor Yellow
        foreach ($project in $secret.Projects) {
            $workingDirInfo = if ($project.WorkingDir) { " (in $($project.WorkingDir))" } else { "" }
            Write-Host "  - $($project.ProjectName)$workingDirInfo" -ForegroundColor Gray
            Write-Host "    Args: $($project.Args)" -ForegroundColor DarkGray
        }
        
        # Prompt for value
        Write-Host "`n➡️  Enter value (or press Enter to skip): " -ForegroundColor Cyan -NoNewline
        $value = Read-Host
        
        if ([string]::IsNullOrWhiteSpace($value)) {
            Write-Warning2 "Skipped"
            # Mark this secret as skipped for all affected projects
            foreach ($project in $secret.Projects) {
                $projectKey = $project.ProjectPath
                if (-not $projectConfigResults.ContainsKey($projectKey)) {
                    $projectConfigResults[$projectKey] = @{
                        Project = $project
                        Skipped = @()
                        Failed  = @()
                        Success = @()
                    }
                }
                $projectConfigResults[$projectKey].Skipped += $secret.Key
            }
            continue
        }
        
        # Configure all affected projects
        Write-Host "⌛ Configuring..." -ForegroundColor Yellow
        
        $successCount = 0
        $failCount = 0
        
        foreach ($project in $secret.Projects) {
            $projectKey = $project.ProjectPath
            if (-not $projectConfigResults.ContainsKey($projectKey)) {
                $projectConfigResults[$projectKey] = @{
                    Project = $project
                    Skipped = @()
                    Failed  = @()
                    Success = @()
                }
            }
            
            $success = Set-UserSecret -ProjectPath $project.ProjectPath `
                                     -Key $secret.Key `
                                     -Value $value `
                                     -WorkingDir $project.WorkingDir
            
            if ($success) {
                $successCount++
                $projectConfigResults[$projectKey].Success += $secret.Key
            }
            else {
                $failCount++
                $projectConfigResults[$projectKey].Failed += $secret.Key
            }
        }
        
        if ($failCount -eq 0) {
            Write-Success "Successfully configured $successCount project(s)"
        }
        else {
            Write-Warning2 "Configured $successCount project(s), $failCount failed"
        }
    }
    
    if ($skippedConfigured -gt 0) {
        Write-Info "`nSkipped $skippedConfigured fully-configured secret(s)"
    }
    
    return $projectConfigResults
}

function Clear-AllUserSecrets {
    <#
    .SYNOPSIS
        Clears all user secrets from all projects.
    #>
    [CmdletBinding()]
    param()

    Write-Header "Clear All User Secrets"
    
    # Find all .csproj and .fsproj files recursively
    Write-Info "Scanning for project files..."
    $projectFiles = @()
    $projectFiles += Get-ChildItem -Path $OutputFolder -Filter '*.csproj' -Recurse -File -ErrorAction SilentlyContinue
    $projectFiles += Get-ChildItem -Path $OutputFolder -Filter '*.fsproj' -Recurse -File -ErrorAction SilentlyContinue
    
    if ($projectFiles.Count -eq 0) {
        Write-Warning2 "No project files found in output folder"
        return
    }
    
    Write-Warning2 "This will clear ALL user secrets from ALL $($projectFiles.Count) project file(s)!"
    Write-Host "➡️  Are you sure? Type 'yes' to confirm: " -ForegroundColor Yellow -NoNewline
    $confirmation = Read-Host
    
    if ($confirmation -ne 'yes') {
        Write-Info "Operation cancelled"
        return
    }
    
    Write-Host "`n⌛ Clearing secrets..." -ForegroundColor Yellow
    
    $successCount = 0
    $failCount = 0
    $skippedCount = 0
    
    foreach ($projectFile in $projectFiles) {
        try {
            # Get relative path for display
            $relativePath = $projectFile.FullName.Replace("$OutputFolder\", "")
            Write-Host "  Clearing $relativePath..." -ForegroundColor Gray
            
            # Run dotnet user-secrets clear
            $result = & dotnet user-secrets clear --project $projectFile.FullName 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                $successCount++
            }
            else {
                # Check if error is because no UserSecretsId exists
                if ($result -match "UserSecretsId") {
                    $skippedCount++
                }
                else {
                    Write-Error2 "  Failed: $result"
                    $failCount++
                }
            }
        }
        catch {
            Write-Error2 "  Error: $_"
            $failCount++
        }
    }
    
    Write-Host ""
    if ($failCount -eq 0) {
        Write-Success "Successfully cleared secrets from $successCount project(s)"
        if ($skippedCount -gt 0) {
            Write-Info "Skipped $skippedCount project(s) (no UserSecretsId configured)"
        }
    }
    else {
        Write-Warning2 "Cleared $successCount project(s), $failCount failed"
        if ($skippedCount -gt 0) {
            Write-Info "Skipped $skippedCount project(s) (no UserSecretsId configured)"
        }
    }
}

function New-AITodoFile {
    <#
    .SYNOPSIS
        Generates an AI_TODO.txt file with all fully-configured projects.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$ConfigResults,
        
        [Parameter(Mandatory)]
        [PSCustomObject[]]$AllProjects,
        
        [Parameter(Mandatory)]
        [hashtable]$ProjectSecrets
    )

    Write-Header "Generating AI Test Configuration"
    
    $fullyConfiguredProjects = @()
    $partiallyConfiguredProjects = @()
    
    foreach ($project in $AllProjects) {
        $projectKey = $project.ProjectPath
        $requiredSecrets = $ProjectSecrets[$projectKey]
        
        if ($requiredSecrets.Count -eq 0) {
            # Project requires no secrets, consider it fully configured
            $fullyConfiguredProjects += $project
            continue
        }
        
        if ($ConfigResults.ContainsKey($projectKey)) {
            $result = $ConfigResults[$projectKey]
            $hasSkipped = $result.Skipped.Count -gt 0
            $hasFailed = $result.Failed.Count -gt 0
            
            if (-not $hasSkipped -and -not $hasFailed) {
                # All secrets were successfully configured
                $fullyConfiguredProjects += $project
            }
            else {
                $partiallyConfiguredProjects += $project
            }
        }
        else {
            # Project was never touched (all its secrets were skipped)
            $partiallyConfiguredProjects += $project
        }
    }
    
    # Generate the XML content
    $xmlContent = @"
<!--
    This file contains information about the projects created during template execution tests.
    Copilot and other AI assistants can augment this file with additional test results.
-->
"@
    
    foreach ($project in $fullyConfiguredProjects) {
        $xmlContent += @"

<test ai_status="pending">
    <args>$($project.Args)</args>
    <path>$($project.ProjectPath)</path>
    <ai_remarks>
    </ai_remarks>
<test/>
"@
    }
    
    # Write the file
    $outputFile = Join-Path $OutputFolder 'AI_TODO.txt'
    Set-Content -Path $outputFile -Value $xmlContent -Encoding UTF8
    
    Write-Success "Generated AI_TODO.txt with $($fullyConfiguredProjects.Count) fully-configured project(s)"
    
    if ($partiallyConfiguredProjects.Count -gt 0) {
        Write-Warning2 "Excluded $($partiallyConfiguredProjects.Count) project(s) with incomplete configuration"
        Write-Info "Run the script again to configure the remaining secrets"
    }
    
    Write-Info "File location: $outputFile"
}

#endregion

#region Main Script

function Main {
    Write-Header ".NET AI Template Configuration Script"
    
    # Check if dotnet is available
    try {
        $dotnetVersion = & dotnet --version 2>&1
        Write-Info "Using .NET SDK version: $dotnetVersion"
    }
    catch {
        Write-Error2 ".NET SDK not found. Please install .NET SDK first."
        exit 1
    }
    
    # Handle Clear mode early (no need to scan projects)
    if ($Clear) {
        Clear-AllUserSecrets
        exit 0
    }
    
    # Step 1: Find all projects
    $projects = Get-ProjectInfo
    
    if ($projects.Count -eq 0) {
        Write-Warning2 "No projects found in the output folder."
        Write-Info "Make sure you have generated projects in: $OutputFolder"
        exit 0
    }
    
    # Step 2: Extract user secrets commands
    Write-Info "Extracting user-secrets commands from README files..."
    $secretsData = Get-UserSecretsCommands -Projects $projects
    $secrets = $secretsData.Secrets
    $projectSecrets = $secretsData.ProjectSecrets
    
    if ($secrets.Count -eq 0) {
        Write-Warning2 "No user-secrets commands found in any README files."
        exit 0
    }
    
    # Step 3: Interactive configuration (secrets loaded lazily as needed)
    $configResults = Invoke-InteractiveConfiguration -Secrets $secrets -SkipConfigured:$SkipConfigured -Force:$Force
    
    # Step 4: Generate AI_TODO.txt file
    New-AITodoFile -ConfigResults $configResults -AllProjects $projects -ProjectSecrets $projectSecrets
    
    Write-Header "Configuration Complete"
    Write-Success "All secrets have been processed."
    Write-Info "You can now run your projects!"
}

# Run main script
Main

#endregion
