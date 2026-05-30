param(
    [string]$GodotExe = "",
    [switch]$NoBuild,
    [switch]$BuildOnly,
    [switch]$Console,
    [switch]$Wait,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$GodotArgs
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$projectDir = Join-Path $repoRoot "src\Lineage.Godot"
$projectFile = Join-Path $projectDir "Lineage.Godot.csproj"

if ([string]::IsNullOrWhiteSpace($GodotExe)) {
    if (-not [string]::IsNullOrWhiteSpace($env:GODOT4)) {
        $GodotExe = $env:GODOT4
    } else {
        $GodotExe = "D:\Godot\Godot_v4.6.2-stable_mono_win64\Godot_v4.6.2-stable_mono_win64.exe"
    }
}

if ($Console -and $GodotExe.EndsWith(".exe", [System.StringComparison]::OrdinalIgnoreCase) -and
    -not $GodotExe.EndsWith("_console.exe", [System.StringComparison]::OrdinalIgnoreCase)) {
    $consoleExe = $GodotExe.Substring(0, $GodotExe.Length - 4) + "_console.exe"
    if (Test-Path -LiteralPath $consoleExe) {
        $GodotExe = $consoleExe
    }
}

if (-not (Test-Path -LiteralPath $GodotExe)) {
    throw "Godot executable was not found: $GodotExe. Set GODOT4 or pass -GodotExe."
}

if (-not $NoBuild) {
    dotnet build $projectFile -c Release -v:minimal
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    # Godot editor runs load the temp/bin/Debug managed assembly even when the
    # C# project has been built in Release. Mirror the Release output there so
    # the double-click launcher uses the current optimized code path.
    $releaseBin = Join-Path $projectDir ".godot\mono\temp\bin\Release"
    $editorLoadBin = Join-Path $projectDir ".godot\mono\temp\bin\Debug"
    if (Test-Path -LiteralPath $releaseBin) {
        New-Item -ItemType Directory -Force -Path $editorLoadBin | Out-Null
        Copy-Item -Path (Join-Path $releaseBin "*") -Destination $editorLoadBin -Recurse -Force
    }
}

if ($BuildOnly) {
    return
}

$arguments = @("--path", $projectDir)
if ($GodotArgs) {
    $arguments += $GodotArgs
}

if ($Wait) {
    Push-Location $projectDir
    try {
        & $GodotExe @arguments
        exit $LASTEXITCODE
    } finally {
        Pop-Location
    }
}

Start-Process -FilePath $GodotExe -ArgumentList $arguments -WorkingDirectory $projectDir
