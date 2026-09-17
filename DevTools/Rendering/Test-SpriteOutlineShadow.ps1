[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$shaderPath = Join-Path $repoRoot 'Assets\Shaders\2D\SpriteOutline2D.shader'
$componentPath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutline2D.cs'
$groupPath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutlineGroup2D.cs'
$defaultsPath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutlineDefaults.cs'
$shader = Get-Content -Raw -LiteralPath $shaderPath
$component = Get-Content -Raw -LiteralPath $componentPath
$group = Get-Content -Raw -LiteralPath $groupPath
$defaults = Get-Content -Raw -LiteralPath $defaultsPath
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

Assert-Contains $shader 'Shader\s+"EatWhat/2D/Sprite Outline"' 'Legacy shader name changed.'
Assert-Contains $shader '_OutlineColor\s+\("Outline Color"' 'Legacy outline color property changed.'
Assert-Contains $shader '_OutlineThickness\s+\("Outline Thickness \(Source Pixels\)"' 'Legacy outline thickness property changed.'
Assert-Contains $shader '_ShadowEnabled\s+\("Shadow Enabled",\s*Float\)\s*=\s*0' 'Shadow enable must default to zero.'
Assert-Contains $shader '_ShadowOpacity\s+\("Shadow Opacity",\s*Range\(0,\s*1\)\)\s*=\s*0' 'Shadow opacity must default to zero.'
Assert-Contains $shader '_ShadowOffset\s+\("Shadow Offset \(Source Pixels\)".*=\s*\(0,\s*0,\s*0,\s*0\)' 'Shadow offset must default to zero.'
Assert-Contains $shader '_ShadowBlur\s+\("Shadow Blur \(Source Pixels\)".*=\s*0' 'Shadow blur must default to zero.'
Assert-Contains $shader 'if\s*\(_ShadowEnabled\s*<\s*0\.5\s*\|\|\s*_ShadowOpacity\s*<=\s*0\.0\)\s*\r?\n\s*return\s+foreground' 'Disabled shadow path must return the legacy result before shadow sampling.'
Assert-Contains $shader 'weightedAlpha\s*\*\s*\(1\.0h\s*/\s*16\.0h\)' 'The 3x3 Gaussian kernel must be normalized by 16.'
Assert-Contains $shader 'SampleAlpha\(uv\)\s*\*\s*4\.0h' 'The Gaussian center weight must be four.'
Assert-Contains $shader 'float2\s+shadowUv\s*=\s*input\.uv\s*-\s*\(_MainTex_TexelSize\.xy\s*\*\s*safeOffset\)' 'Shadow offset must use source-pixel texel size and inverse sample displacement.'
Assert-Contains $shader 'foreground\.a\s*\+\s*shadowAlpha\s*\*\s*\(1\.0h\s*-\s*foreground\.a\)' 'Shadow must be source-over composited behind the legacy foreground.'
Assert-Contains $shader 'shadow \(bottom\), legacy outline \(middle\), original sprite \(top\)' 'Layer order evidence is missing.'
Assert-Contains $shader 'const\s+float\s+sin30\s*=\s*0\.5' 'Legacy 30-degree outline sampler changed.'
Assert-Contains $shader 'const\s+float\s+cos30\s*=\s*0\.8660254' 'Legacy 30-degree outline sampler changed.'
Assert-NotContains $shader '0\.9238795' 'Incorrect 22.5-degree sentinel found.'

$shadowSampler = [regex]::Match($shader, '(?s)half\s+SampleShadowAlpha\s*\(.*?\n\s*}\s*\n\s*half4\s+ComposeLegacyOutline').Value
if ([string]::IsNullOrWhiteSpace($shadowSampler)) { throw 'Could not isolate SampleShadowAlpha.' }
$staticAssertions++
$weightedTapCount = ([regex]::Matches($shadowSampler, 'weightedAlpha\s*\+=\s*SampleAlpha')).Count
if ($weightedTapCount -ne 9) { throw "Expected nine Gaussian taps, found $weightedTapCount." }
$staticAssertions++
$edgeWeightCount = ([regex]::Matches($shadowSampler, '\*\s*2\.0h')).Count
if ($edgeWeightCount -ne 4) { throw "Expected four Gaussian edge weights of two, found $edgeWeightCount." }
$staticAssertions++
$centerWeightCount = ([regex]::Matches($shadowSampler, '\*\s*4\.0h')).Count
if ($centerWeightCount -ne 1) { throw "Expected one Gaussian center weight of four, found $centerWeightCount." }
$staticAssertions++

$passCount = ([regex]::Matches($shader, '(?m)^\s*Pass\s*$')).Count
if ($passCount -ne 2) { throw "Expected exactly two shader passes, found $passCount." }
$staticAssertions++
foreach ($lightMode in @('Universal2D', 'UniversalForward')) {
    Assert-Contains $shader ('"LightMode"\s*=\s*"' + $lightMode + '"') "Missing $lightMode pass."
}
$fragmentPragmaCount = ([regex]::Matches($shader, '#pragma\s+fragment\s+OutlineFragment')).Count
if ($fragmentPragmaCount -ne 2) { throw "Both passes must use OutlineFragment; found $fragmentPragmaCount pragmas." }
$staticAssertions++

Assert-Contains $component 'public\s+void\s+Configure\s*\(\s*Material\s+material,\s*Color\s+color,\s*float\s+widthInSourcePixels\s*\)' 'Legacy component Configure API is missing.'
Assert-Contains $group 'public\s+void\s+Configure\s*\(\s*Material\s+material,\s*Color\s+color,\s*float\s+widthInSourcePixels\s*\)' 'Legacy group Configure API is missing.'
Assert-Contains $component '(?s)\[ColorUsage\(true,\s*true\)\]\s*private\s+Color\s+outlineColor' 'Single-Sprite HDR outline color must expose Alpha.'
Assert-Contains $group '\[SerializeField,\s*ColorUsage\(true,\s*true\)\]\s*private\s+Color\s+outlineColor' 'Group HDR outline color must expose Alpha.'
Assert-Contains $defaults '\[SerializeField,\s*ColorUsage\(true,\s*true\)\]\s*private\s+Color\s+outlineColor' 'Defaults HDR outline color must expose Alpha.'
Assert-Contains $component '(?s)\[ColorUsage\(false,\s*true\)\]\s*private\s+Color\s+shadowColor' 'Shadow color Alpha must stay hidden because Shadow Opacity is the user control.'
Assert-Contains $shader 'outlineCoverage\s*=.*\*\s*_OutlineColor\.a' 'Independent shader must consume outline Alpha only in outline coverage.'
Assert-NotContains $component 'SetColor\(Shader\.PropertyToID\("_Color"\)' 'Outline component must not rewrite SpriteRenderer tint/alpha.'
Assert-Contains $component 'private\s+bool\s+shadowEnabled\s*=\s*false' 'Component shadow must default off.'
Assert-Contains $group 'private\s+bool\s+shadowEnabled\s*=\s*false' 'Group shadow must default off.'
Assert-Contains $component '(?s)spriteRenderer\.GetPropertyBlock\(propertyBlock\).*?propertyBlock\.SetColor\(OutlineColorId' 'The component must merge the existing property block before writing owned values.'
Assert-NotContains ($component + "`n" + $group) '\.material\b' 'Renderer.material access would allocate material instances.'
Assert-NotContains ($component + "`n" + $group) 'new\s+Material\s*\(' 'Runtime code must not allocate materials.'
Assert-Contains $component 'currentSprite\s*==\s*lastSynchronizedSprite' 'Sprite dirty tracking is missing.'
Assert-Contains $component 'ApplyPropertyBlock\(0f,\s*false\)' 'Disable path must zero outline and shadow flags.'
Assert-Contains $component 'ResolveActualMergeOwnership' 'Actual backend ownership query is missing.'
Assert-Contains $component 'isActiveAndEnabled\s*&&\s*!mergedOutline\s*\?\s*thickness\s*:\s*0f' 'Merged outline must suppress only the independent outline output.'
Assert-Contains $component 'shadowEnabled\s*&&\s*!mergedShadow' 'Merged shadow must suppress only the independent shadow output.'
Assert-Contains $component 'ConfigureFormal\(spriteRenderer,\s*mergeOutline,\s*mergeShadow,\s*overrideGroup' 'Parameter ownership must be passed independently from merge modes.'
Assert-Contains $group 'CreateMissingMembers\s*\(' 'Group creation must be explicit.'
Assert-Contains $group 'CreateOrSynchronizeMergeAdapters\s*\(' 'Explicit formal backend wiring entry is missing.'
Assert-Contains $group 'ConfigureFormalGroup\(mergeOutline,\s*outlineColor,\s*thickness,\s*mergeShadow' 'Group merge and visual parameters must remain independent.'
Assert-Contains $group 'SynchronizeFollowingMembers\s*\(' 'Group synchronization must be explicit.'
Assert-NotContains $group 'OnTransformChildrenChanged\s*\(' 'Hierarchy changes must not create or rewrite members.'
Assert-Contains $component 'overrideGroup\s*=\s*false' 'Member parameters must default to local values.'
Assert-Contains $defaults 'Resources\.Load<SpriteOutlineDefaults>\(ResourcePath\)' 'One-shot defaults resource contract is missing.'

$sourcePaths = @(
    (Join-Path $PSScriptRoot 'UnityStubs.cs'),
    $componentPath,
    $groupPath,
    $defaultsPath,
    (Join-Path $PSScriptRoot 'SpriteOutlineShadowTests.cs')
)
Add-Type -Path $sourcePaths
$runtimeResult = [SpriteOutlineShadowTests]::Run()

[pscustomobject]@{
    success = $true
    runtimeResult = $runtimeResult
    compiledSourceCount = $sourcePaths.Count
    shaderPasses = $passCount
    staticAssertions = $staticAssertions
    note = 'Shader source checks are structural and do not replace Unity GPU compilation or visual validation.'
} | ConvertTo-Json -Depth 3
