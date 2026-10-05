[CmdletBinding()]
param([string]$UnityEditorPath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$projectVersion = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt')
$versionMatch = [regex]::Match($projectVersion, 'm_EditorVersion:\s*([^\r\n]+)')
if (-not $versionMatch.Success) { throw 'Could not resolve Unity editor version.' }
$unityVersion = $versionMatch.Groups[1].Value.Trim()
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = @(
        "C:/Program Files/Unity/Hub/Editor/$unityVersion/Editor/Unity.exe",
        "C:/Program Files/Unity $unityVersion/Editor/Unity.exe"
    ) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $UnityEditorPath -or -not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw "Project Editor $unityVersion is not installed in either supported location."
}
if (-not (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion.StartsWith($unityVersion, [StringComparison]::Ordinal)) {
    throw 'Editor version does not match ProjectVersion.txt.'
}

$unityData = Join-Path (Split-Path -Parent $UnityEditorPath) 'Data'
$dotnet = Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $unityData 'DotNetSdkRoslyn\csc.dll'
$mono = Join-Path $unityData 'MonoBleedingEdge\bin\mono.exe'
$framework = Join-Path $unityData 'MonoBleedingEdge\lib\mono\4.7.1-api'
$unityManaged = Join-Path $unityData 'Managed\UnityEngine'
$compileRoot = Join-Path ([IO.Path]::GetTempPath()) ('EatWhat-MouseFallback-' + [Guid]::NewGuid().ToString('N'))
$assemblyPath = Join-Path $compileRoot 'MousePositionFallbackTests.exe'
$realUnityAssemblyPath = Join-Path $compileRoot 'PointerInput.RealUnityApi.dll'
$sources = @(
    (Join-Path $PSScriptRoot 'UnityStubs.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Tools.cs'),
    (Join-Path $PSScriptRoot 'MousePositionFallbackTests.cs')
)
$references = @(
    (Join-Path $framework 'mscorlib.dll'),
    (Join-Path $framework 'System.dll'),
    (Join-Path $framework 'System.Core.dll'),
    (Join-Path $framework 'Facades\netstandard.dll'),
    (Join-Path $framework 'Facades\System.Runtime.dll')
)
$unityEngineReferences = @(Get-ChildItem -LiteralPath $unityManaged -Filter 'UnityEngine*.dll' -File | Select-Object -ExpandProperty FullName)
$realUnityReferences = @($references) + $unityEngineReferences + @(
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.InputSystem.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\UnityEngine.UI.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Assembly-CSharp.dll')
)
$realUnitySources = @(
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Tools.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\MouseInteractive\MouseManager.cs')
)

try {
    foreach ($required in @($dotnet, $csc, $mono, $framework, $unityManaged) + $sources + $realUnitySources + $realUnityReferences) {
        if (-not (Test-Path -LiteralPath $required)) { throw "Missing compiler input: $required" }
    }
    New-Item -ItemType Directory -Path $compileRoot | Out-Null
    $arguments = @($csc, '/nologo', '/target:exe', '/langversion:latest', '/define:EATWHAT_COMPILE_REAL_TOOLS', '/nostdlib+', ('/out:' + $assemblyPath))
    $arguments += $references | ForEach-Object { '/reference:' + $_ }
    $arguments += $sources
    $compilerOutput = & $dotnet @arguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Roslyn compilation failed.`n$($compilerOutput | Out-String)" }

    $realArguments = @($csc, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', ('/out:' + $realUnityAssemblyPath))
    $realArguments += $realUnityReferences | ForEach-Object { '/reference:' + $_ }
    $realArguments += $realUnitySources
    $realCompilerOutput = & $dotnet @realArguments 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Real Unity API compilation failed.`n$($realCompilerOutput | Out-String)" }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    $runtimeOutput = & $mono $assemblyPath 2>&1
    $runtimeExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($runtimeExitCode -ne 0) { throw "Mouse fallback tests failed.`n$($runtimeOutput | Out-String)" }
    [pscustomobject]@{
        success = $true
        result = ($runtimeOutput | Out-String).Trim()
        compiledSourceCount = $sources.Count
        assertions = 13
        realUnityApiCompile = $true
        realUnityApiCompiledSources = $realUnitySources.Count
    } | ConvertTo-Json -Depth 3
}
finally {
    if (Test-Path -LiteralPath $compileRoot) {
        Remove-Item -LiteralPath $compileRoot -Recurse -Force
    }
}
