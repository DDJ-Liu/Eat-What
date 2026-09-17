[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$runtimePaths = @(
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Rendering\SceneColorCapture2D.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Rendering\EtherBubbleDistortion2D.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Rendering\EtherBubbleEmitter2D.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Tools\Debug\VisualEffectsLabDriver.cs')
)
$editorPaths = @(
    (Join-Path $repoRoot 'Assets\Editor\VisualEffects\VisualEffectsLabBuilder.cs'),
    (Join-Path $repoRoot 'Assets\Editor\VisualEffects\VisualEffectsLabAudit.cs')
)
$shaderPath = Join-Path $repoRoot 'Assets\Shaders\2D\EtherBubbleDistortion2D.shader'
$stubPath = Join-Path $PSScriptRoot 'EtherBubbleUnityStubs.cs'
$testPath = Join-Path $PSScriptRoot 'EtherBubbleDistortionTests.cs'
$staticAssertions = 0

function Assert-Contains {
    param([string]$Source, [string]$Pattern, [string]$Message)
    if ($Source -notmatch $Pattern) { throw $Message }
    $script:staticAssertions++
}

function Assert-NotContains {
    param([string]$Source, [string]$Pattern, [string]$Message)
    if ($Source -match $Pattern) { throw $Message }
    $script:staticAssertions++
}

$captureSource = Get-Content -Raw -LiteralPath $runtimePaths[0]
$bubbleSource = Get-Content -Raw -LiteralPath $runtimePaths[1]
$emitterSource = Get-Content -Raw -LiteralPath $runtimePaths[2]
$driverSource = Get-Content -Raw -LiteralPath $runtimePaths[3]
$builderSource = Get-Content -Raw -LiteralPath $editorPaths[0]
$auditSource = Get-Content -Raw -LiteralPath $editorPaths[1]
$shader = Get-Content -Raw -LiteralPath $shaderPath

Assert-Contains $shader 'Shader\s+"EatWhat/2D/Ether Bubble Distortion"' 'Shader name changed.'
Assert-Contains $shader '_SceneColorTexture\s+\("Captured Scene Color"' 'Scene color property is missing.'
Assert-Contains $shader 'if\s*\(_CaptureValid\s*<\s*0\.5' 'Invalid capture early-out is missing.'
Assert-Contains $shader 'ComputeScreenPos' 'Screen-space sampling contract is missing.'
Assert-Contains $shader 'float2\s+sampleUv\s*=\s*clamp' 'Screen-edge sample clamp is missing.'
Assert-Contains $shader 'circleMask' 'Local circular mask is missing.'
Assert-Contains $shader 'wave\s*=\s*sin' 'Radial wave is missing.'
Assert-Contains $shader 'float2\s+flow\s*=' 'Low-frequency flow is missing.'
Assert-Contains $shader '_BubbleAspect' 'Instance aspect correction is missing.'
Assert-Contains $shader '"LightMode"\s*=\s*"Universal2D"' 'Universal2D pass is missing.'
Assert-Contains $shader '"LightMode"\s*=\s*"UniversalForward"' 'UniversalForward fallback is missing.'
Assert-NotContains $shader 'GrabPass|_CameraOpaqueTexture|_CameraSortingLayerTexture' 'Shader must use only the explicit capture texture.'

$passCount = ([regex]::Matches($shader, '(?m)^\s*Pass\s*$')).Count
if ($passCount -ne 2) { throw "Expected two shader passes, found $passCount." }
$staticAssertions++

Assert-Contains $captureSource 'RenderPipelineManager\.beginCameraRendering\s*\+=' 'SRP begin callback is missing.'
Assert-Contains $captureSource 'RenderPipelineManager\.endCameraRendering\s*\+=' 'SRP completion callback is missing.'
Assert-Contains $captureSource 'originalCameraState\.Restore' 'Camera state restoration is missing.'
Assert-Contains $captureSource 'ReleaseOwnedTexture' 'Owned RT release path is missing.'
Assert-Contains $captureSource 'public\s+int\s+OwnedRenderTextureCount' 'RT ownership observation is missing.'
if (([regex]::Matches($captureSource, 'new\s+RenderTexture\s*\(')).Count -ne 1) {
    throw 'Capture component must have exactly one RenderTexture allocation site.'
}
$staticAssertions++

$runtimeCombined = $captureSource + "`n" + $bubbleSource + "`n" + $emitterSource + "`n" + $driverSource
Assert-NotContains $runtimeCombined 'Shader\.SetGlobal|\.material\b|new\s+Material\s*\(' 'Runtime must not allocate materials or set global shader state.'
Assert-NotContains $runtimeCombined 'GameObject\.Find|FindObjectOfType|static\s+.*\s+Instance' 'Runtime must use explicit references, not scene search or new singletons.'
Assert-NotContains $captureSource '\.Render\s*\(' 'Capture must not manually call Camera.Render.'
Assert-Contains $bubbleSource 'spriteRenderer\.GetPropertyBlock\(propertyBlock\)' 'Bubble must merge existing PropertyBlock values.'
Assert-Contains $bubbleSource '(?s)if\s*\(!propertiesDirty.*?Mathf\.Approximately' 'Bubble dirty-write guard is missing.'
Assert-Contains $emitterSource 'public\s+bool\s+Emit\s*\(' 'Emit API is missing.'
Assert-Contains $emitterSource 'public\s+int\s+Burst\s*\(' 'Burst API is missing.'
Assert-Contains $emitterSource 'public\s+void\s+Clear\s*\(' 'Clear API is missing.'
Assert-Contains $emitterSource 'MinimumBubbleCount\s*=\s*1' 'Minimum cap contract is missing.'
Assert-Contains $emitterSource 'MaximumBubbleCount\s*=\s*64' 'Finite maximum cap contract is missing.'

Assert-Contains $driverSource 'VERIFY-TEMP' 'Protected verification marker is missing.'
Assert-Contains $driverSource 'InputManager\.OnKeyPressed\s*\+=' 'Driver must consume InputManager.'
Assert-NotContains $driverSource 'Keyboard\.current|Input\.GetKey|InputAction\s*\.' 'Driver must not poll a parallel input path.'
foreach ($method in @('EmitOne','BurstEight','BurstSixteen','ToggleContinuousEmission','TogglePause','ToggleDistortion','ApplySoftPreset','ApplyObviousPreset','ClearAll')) {
    Assert-Contains $driverSource ('public\s+void\s+' + $method + '\s*\(') "Driver method $method is missing."
}

Assert-Contains $builderSource '\[MenuItem\("Tools/Visual Effects Lab/Build or Open Lab"\)\]' 'Explicit Builder menu is missing.'
Assert-NotContains $builderSource '\[\s*InitializeOnLoad|:\s*AssetPostprocessor\b|\bDidReloadScripts\s*\(|\[\s*InitializeOnEnterPlayMode' 'Builder must not run automatically.'
Assert-Contains $builderSource 'existing scene opened without modification' 'Idempotent existing-scene protection is missing.'
Assert-Contains $builderSource 'debugAndReferences\.SetActive\(false\)' 'DebugAndReferences must default inactive.'
Assert-Contains $builderSource 'checker\.AddComponent<GridLayout>' 'Builder must consume TK-LAY-01 GridLayout.'
Assert-Contains $builderSource 'text\.font\s*=\s*LoadRequired<TMP_FontAsset>\(ChineseFontPath\)' 'World labels must use the existing Chinese TMP font.'
Assert-Contains $auditSource 'Audit Active Lab \(Read Only\)' 'Read-only Audit menu is missing.'
Assert-NotContains $auditSource 'SaveScene|SaveAssets|CreateAsset|WriteAll|SetActive\s*\(' 'Audit must remain read-only.'
Assert-Contains $auditSource 'backgroundLayerViolations' 'Independent layer measurement is missing.'
Assert-Contains $auditSource 'missingScripts' 'Missing Script measurement is missing.'
Assert-Contains $auditSource 'canvasCount' 'Canvas measurement is missing.'

$sourcePaths = @($stubPath) + $runtimePaths + @($testPath)
Add-Type -Path $sourcePaths
$runtimeResult = [EtherBubbleDistortionTests]::Run()

$projectVersion = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt')
$versionMatch = [regex]::Match($projectVersion, 'm_EditorVersion:\s*([^\r\n]+)')
if (-not $versionMatch.Success) { throw 'Could not resolve Unity editor version.' }
$unityVersion = $versionMatch.Groups[1].Value.Trim()
$unityData = "C:\Program Files\Unity $unityVersion\Editor\Data"
$dotnet = Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $unityData 'DotNetSdkRoslyn\csc.dll'
$unityManaged = Join-Path $unityData 'Managed\UnityEngine'
$framework = Join-Path $unityData 'MonoBleedingEdge\lib\mono\4.7.1-api'
foreach ($required in @($dotnet, $csc, $unityManaged, $framework)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Required Unity compiler input is missing: $required" }
}

$buildDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '.etherbubble-validation'))
$renderingRoot = [System.IO.Path]::GetFullPath($PSScriptRoot) + [System.IO.Path]::DirectorySeparatorChar
if (-not $buildDirectory.StartsWith($renderingRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe validation directory: $buildDirectory"
}
if (Test-Path -LiteralPath $buildDirectory) { Remove-Item -LiteralPath $buildDirectory -Recurse -Force }
New-Item -ItemType Directory -Path $buildDirectory | Out-Null

$runtimeDll = Join-Path $buildDirectory 'EtherBubble.Runtime.dll'
$editorDll = Join-Path $buildDirectory 'EtherBubble.Editor.dll'
$inputManagerContract = Join-Path $buildDirectory 'InputManagerContract.cs'
[System.IO.File]::WriteAllText(
    $inputManagerContract,
    @'
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputManager : MonoBehaviour
{
    public static event Func<Key, bool> OnKeyPressed
    {
        add { }
        remove { }
    }
}

public sealed class SpriteOutline2D : MonoBehaviour
{
    public void Configure(Material material, Color color, float widthInSourcePixels) { }
    public void ConfigureShadow(bool enabled, Color color, Vector2 offsetInSourcePixels, float opacity, float softnessInSourcePixels) { }
}

public sealed class GridLayout : MonoBehaviour
{
    public void SetCellSize(Vector2 value) { }
    public void ArrangeChildren() { }
}
'@)
$frameworkReferences = @(
    (Join-Path $framework 'mscorlib.dll'),
    (Join-Path $framework 'System.dll'),
    (Join-Path $framework 'System.Core.dll'),
    (Join-Path $framework 'Facades\netstandard.dll'),
    (Join-Path $framework 'Facades\System.Runtime.dll')
)
$runtimeReferences = $frameworkReferences + @(
    (Join-Path $unityManaged 'UnityEngine.dll'),
    (Join-Path $unityManaged 'UnityEngine.CoreModule.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.InputSystem.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.TextMeshPro.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\UnityEngine.UI.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.RenderPipelines.Core.Runtime.dll')
)
$runtimeArguments = @($csc, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', ('/out:' + $runtimeDll))
$runtimeArguments += $runtimeReferences | ForEach-Object { '/reference:' + $_ }
$runtimeArguments += $runtimePaths + @($inputManagerContract)
try {
    & $dotnet @runtimeArguments
    if ($LASTEXITCODE -ne 0) { throw "True runtime DLL compilation failed with exit code $LASTEXITCODE." }

    $editorReferences = $runtimeReferences + @(
        (Join-Path $unityManaged 'UnityEngine.AudioModule.dll'),
        (Join-Path $unityManaged 'UnityEngine.ImageConversionModule.dll'),
        (Join-Path $unityManaged 'UnityEngine.TextRenderingModule.dll'),
        (Join-Path $unityManaged 'UnityEngine.UIModule.dll'),
        (Join-Path $unityManaged 'UnityEditor.dll'),
        (Join-Path $unityManaged 'UnityEditor.CoreModule.dll'),
        (Join-Path $unityManaged 'UnityEditor.SceneViewModule.dll'),
        (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.RenderPipelines.Universal.Runtime.dll'),
        (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.RenderPipelines.Universal.2D.Internal.dll'),
        $runtimeDll
    )
    $editorArguments = @($csc, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', ('/out:' + $editorDll))
    $editorArguments += $editorReferences | Select-Object -Unique | ForEach-Object { '/reference:' + $_ }
    $editorArguments += $editorPaths
    & $dotnet @editorArguments
    if ($LASTEXITCODE -ne 0) { throw "True Editor DLL compilation failed with exit code $LASTEXITCODE." }

    $runtimeDllBytes = (Get-Item -LiteralPath $runtimeDll).Length
    $editorDllBytes = (Get-Item -LiteralPath $editorDll).Length
}
finally {
    if (Test-Path -LiteralPath $buildDirectory) { Remove-Item -LiteralPath $buildDirectory -Recurse -Force }
}

[pscustomobject]@{
    success = $true
    runtimeResult = $runtimeResult
    staticAssertions = $staticAssertions
    shaderPasses = $passCount
    trueRuntimeDllBytes = $runtimeDllBytes
    trueEditorDllBytes = $editorDllBytes
    unityVersion = $unityVersion
    note = 'Stub behavior and source checks do not replace Unity Shader/GPU compilation, Lab assembly import, Play Mode, or human visual approval.'
} | ConvertTo-Json -Depth 3
