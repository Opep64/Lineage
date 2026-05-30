param(
    [string]$GodotExe = "",
    [switch]$NoBuild,
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
}

$arguments = @("--release", "--path", $projectDir)
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
