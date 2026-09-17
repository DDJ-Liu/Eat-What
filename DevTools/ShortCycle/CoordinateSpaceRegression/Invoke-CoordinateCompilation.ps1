[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$DragContainerPath,
    [Parameter(Mandatory = $true)][string]$Label,
    [Parameter(Mandatory = $true)][string]$BuildDirectory
)

$ErrorActionPreference = 'Stop'
$testRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $testRoot))
$projectVersion = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt')
$versionMatch = [regex]::Match($projectVersion, 'm_EditorVersion:\s*([^\r\n]+)')
if (-not $versionMatch.Success) { throw 'Could not resolve Unity editor version.' }
$unityData = "C:\Program Files\Unity $($versionMatch.Groups[1].Value.Trim())\Editor\Data"
$dotnet = Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $unityData 'DotNetSdkRoslyn\csc.dll'
$framework = Join-Path $unityData 'MonoBleedingEdge\lib\mono\4.7.1-api'
$compileRoot = [IO.Path]::GetFullPath($BuildDirectory)
$resolvedSystemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
if (-not $compileRoot.StartsWith($resolvedSystemTemp, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe compile directory: $compileRoot"
}
$assemblyPath = Join-Path $compileRoot "$Label.CoordinateSpace.dll"
$paths = @(
    (Join-Path $testRoot 'UnityCoordinateStubs.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\MouseInteractive\Drag\DragLimit.cs'),
    $DragContainerPath,
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Transition\TransitionBehaviour.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Transition\TransitionBehaviour_Position.cs'),
    (Join-Path $testRoot 'CoordinateSpaceRegressionTests.cs')
)
$references = @(
    (Join-Path $framework 'mscorlib.dll'),
    (Join-Path $framework 'System.dll'),
    (Join-Path $framework 'System.Core.dll'),
    (Join-Path $framework 'Facades\netstandard.dll'),
    (Join-Path $framework 'Facades\System.Runtime.dll')
)

try {
    foreach ($required in @($dotnet, $csc, $framework) + $paths + $references) {
        if (-not (Test-Path -LiteralPath $required)) { throw "Missing compiler input: $required" }
    }
    New-Item -ItemType Directory -Path $compileRoot | Out-Null
    $arguments = @($csc, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', ('/out:' + $assemblyPath))
    $arguments += $references | ForEach-Object { '/reference:' + $_ }
    $arguments += $paths
    $compilerOutput = & $dotnet @arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Roslyn compilation failed.`n$($compilerOutput | Out-String)" }
    Add-Type -Path $assemblyPath -ErrorAction Stop
    [CoordinateSpaceRegressionTests]::Run()
    Write-Output "$Label`_PASS"
    exit 0
}
catch {
    Write-Output "$Label`_FAIL $($_.Exception.Message)"
    exit 1
}
