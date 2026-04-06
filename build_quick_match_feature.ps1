param(
    [string]$UnityPath = "",
    [switch]$NoQuit,
    [switch]$TempOnly
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "Adaptabrawl\Adaptabrawl"
$tempProjectPath = Join-Path $PSScriptRoot "Adaptabrawl\Adaptabrawl_QuickMatchBuildTemp"
$rawLogPath = Join-Path $PSScriptRoot "quickmatch_unity_temp.log"

if (-not (Test-Path $projectPath)) {
    throw "Unity project not found at $projectPath"
}

function Test-IsChildPath {
    param(
        [string]$ParentPath,
        [string]$ChildPath
    )

    $resolvedParent = [System.IO.Path]::GetFullPath($ParentPath).TrimEnd('\')
    $resolvedChild = [System.IO.Path]::GetFullPath($ChildPath)
    return $resolvedChild.StartsWith("$resolvedParent\", [System.StringComparison]::OrdinalIgnoreCase) -or
        $resolvedChild.Equals($resolvedParent, [System.StringComparison]::OrdinalIgnoreCase)
}

function Invoke-RobocopyStrict {
    param(
        [string]$Source,
        [string]$Destination,
        [string[]]$Arguments
    )

    & robocopy $Source $Destination @Arguments | Out-Host
    if ($LASTEXITCODE -gt 7) {
        throw "Robocopy failed with exit code $LASTEXITCODE while syncing '$Source' -> '$Destination'."
    }
}

function Get-RawLogText {
    if (-not (Test-Path $rawLogPath)) {
        return ""
    }

    return Get-Content -LiteralPath $rawLogPath -Raw
}

function Stop-QuickMatchTempProcesses {
    try {
        $tempProcesses = Get-CimInstance Win32_Process | Where-Object {
            ($_.Name -eq "Unity.exe" -or $_.Name -eq "UnityPackageManager.exe" -or $_.Name -eq "UnityAutoQuitter.exe") -and
            $_.CommandLine -like "*Adaptabrawl_QuickMatchBuildTemp*"
        }

        foreach ($process in $tempProcesses) {
            Stop-Process -Id $process.ProcessId -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
        Write-Warning "Unable to inspect or stop stale temp-project Unity processes. Continuing anyway."
    }
}

function Sync-QuickMatchTempProject {
    New-Item -ItemType Directory -Path $tempProjectPath -Force | Out-Null
    Invoke-RobocopyStrict -Source $projectPath -Destination $tempProjectPath -Arguments @(
        "/MIR",
        "/XD", "Library", "Temp", "Logs", "UserSettings", "obj", "Build", "Builds"
    )

    $sourcePackageCache = Join-Path $projectPath "Library\PackageCache"
    $tempPackageCache = Join-Path $tempProjectPath "Library\PackageCache"
    if ((Test-Path $sourcePackageCache) -and (-not (Test-Path $tempPackageCache))) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $tempPackageCache) -Force | Out-Null
        Invoke-RobocopyStrict -Source $sourcePackageCache -Destination $tempPackageCache -Arguments @("/MIR")
    }
}

function Copy-GeneratedArtifact {
    param(
        [string]$RelativePath
    )

    $sourcePath = Join-Path $tempProjectPath $RelativePath
    if (-not (Test-Path $sourcePath)) {
        return
    }

    $destinationPath = Join-Path $projectPath $RelativePath
    $destinationParent = Split-Path -Parent $destinationPath
    if (-not [string]::IsNullOrWhiteSpace($destinationParent)) {
        New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
    }

    if (Test-Path $sourcePath -PathType Container) {
        if (Test-Path $destinationPath) {
            Remove-Item -LiteralPath $destinationPath -Recurse -Force
        }

        Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Recurse -Force
        return
    }

    Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Force
}

function Sync-GeneratedArtifacts {
    $artifacts = @(
        "Assets\Scenes\QuickMatchScene.unity",
        "Assets\Scenes\QuickMatchScene.unity.meta",
        "Assets\Scenes\StartScene.unity",
        "Assets\Scenes\StartScene.unity.meta",
        "Assets\Resources\QuickMatch",
        "Assets\Resources\QuickMatch.meta",
        "Assets\AI trainings\models",
        "Assets\AI trainings\models.meta",
        "Assets\AI trainings\logs",
        "Assets\AI trainings\logs.meta",
        "ProjectSettings\EditorBuildSettings.asset"
    )

    foreach ($artifact in $artifacts) {
        Copy-GeneratedArtifact -RelativePath $artifact
    }
}

function Test-TransientTempBuildFailure {
    $rawLog = Get-RawLogText
    return $rawLog -match "unable to open database file" -or
        $rawLog -match "Could not establish a connection with the Unity Package Manager local server process"
}

function Test-QuickMatchAutomationCompleted {
    param(
        [string]$TargetProjectPath
    )

    $rawLog = Get-RawLogText
    $quickMatchScenePath = Join-Path $TargetProjectPath "Assets\Scenes\QuickMatchScene.unity"
    $completionLogged = $rawLog -match "\[QuickMatchFeatureAutomation\] Seed="
    return $completionLogged -and (Test-Path $quickMatchScenePath)
}

function Wait-ForQuickMatchCompletionEvidence {
    param(
        [string]$TargetProjectPath,
        [int]$TimeoutSeconds = 10
    )

    for ($elapsed = 0; $elapsed -lt $TimeoutSeconds; $elapsed++) {
        if (Test-QuickMatchAutomationCompleted -TargetProjectPath $TargetProjectPath) {
            return $true
        }

        Start-Sleep -Seconds 1
    }

    return Test-QuickMatchAutomationCompleted -TargetProjectPath $TargetProjectPath
}

function Invoke-UnityQuickMatchAutomation {
    param(
        [string]$UnityExecutablePath,
        [string]$TargetProjectPath,
        [string]$TargetLabel
    )

    $maxAttempts = if ($TargetProjectPath -eq $tempProjectPath) { 2 } else { 1 }
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        if (Test-Path $rawLogPath) {
            Remove-Item -LiteralPath $rawLogPath -Force
        }

        $unityArguments = @(
            "-batchmode",
            "-projectPath", $TargetProjectPath,
            "-executeMethod", "Adaptabrawl.Editor.QuickMatchFeatureAutomation.BuildSceneTrainModelsAndExit",
            "-logFile", $rawLogPath
        )

        if (-not $NoQuit) {
            $unityArguments += "-quit"
        }

        Write-Host "Running Quick Match build + training automation on the $TargetLabel project (attempt $attempt/$maxAttempts)..."
        & $UnityExecutablePath @unityArguments
        $unityExitCode = $LASTEXITCODE
        if ($unityExitCode -eq 0) {
            return
        }

        if (Wait-ForQuickMatchCompletionEvidence -TargetProjectPath $TargetProjectPath) {
            Write-Warning "Unity returned exit code $unityExitCode for the $TargetLabel project, but the Quick Match automation completed and generated the expected artifacts. Treating the run as successful."
            return
        }

        $rawLog = Get-RawLogText

        $databaseLockDetected = $rawLog -match "unable to open database file"
        if (-not $databaseLockDetected -or $attempt -eq $maxAttempts) {
            throw "Unity automation failed with exit code $unityExitCode for the $TargetLabel project."
        }

        Write-Warning "Unity reported a temp-project database lock. Waiting briefly and retrying once."
        Stop-QuickMatchTempProcesses
        Start-Sleep -Seconds 5
    }
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $preferred = @(
        "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe"
    )

    foreach ($candidate in $preferred) {
        if (Test-Path $candidate) {
            $UnityPath = $candidate
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($UnityPath) -or -not (Test-Path $UnityPath)) {
    throw "Unity executable was not found. Pass -UnityPath <full path to Unity.exe>."
}

if (-not (Test-IsChildPath -ParentPath (Join-Path $PSScriptRoot "Adaptabrawl") -ChildPath $tempProjectPath)) {
    throw "Refusing to use temp project outside the workspace: $tempProjectPath"
}

Stop-QuickMatchTempProcesses
Sync-QuickMatchTempProject

Write-Host "Using Unity: $UnityPath"
Write-Host "Source project: $projectPath"
Write-Host "Temp project: $tempProjectPath"
Write-Host "Raw Unity log: $rawLogPath"

$usedMainProjectFallback = $false

try {
    Invoke-UnityQuickMatchAutomation -UnityExecutablePath $UnityPath -TargetProjectPath $tempProjectPath -TargetLabel "temp"

    if (-not (Test-Path (Join-Path $tempProjectPath "Assets\Scenes\QuickMatchScene.unity"))) {
        throw "Quick Match automation did not generate Assets\Scenes\QuickMatchScene.unity in the temp project."
    }

    Sync-GeneratedArtifacts
}
catch {
    if ($TempOnly -or -not (Test-TransientTempBuildFailure)) {
        throw
    }

    $tempFailureMessage = $_.Exception.Message
    Write-Warning "Temp-project automation failed with a transient Unity issue: $tempFailureMessage"
    Write-Warning "Falling back to running the automation directly on the real project."

    Invoke-UnityQuickMatchAutomation -UnityExecutablePath $UnityPath -TargetProjectPath $projectPath -TargetLabel "real"
    $usedMainProjectFallback = $true
}

if ($usedMainProjectFallback) {
    Write-Host "Quick Match scene, champion models, training log, and build settings were updated directly in the real project."
}
else {
    Write-Host "Quick Match scene, champion models, training log, and build settings were synced back into the real project."
}
