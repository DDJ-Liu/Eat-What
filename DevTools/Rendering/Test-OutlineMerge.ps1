[CmdletBinding()]
param([string]$UnityEditorPath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$memberPath = Join-Path $repoRoot 'Assets\Scripts\Tools\Rendering\SpriteOutlineMergeMember2D.cs'
$rendererPath = Join-Path $repoRoot 'Assets\Scripts\Tools\Rendering\SpriteOutlineMergeRenderer2D.cs'
$driverPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Debug\OutlineShadowLabDriver.cs'
$originalOutlinePath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutline2D.cs'
$originalOutlineGroupPath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutlineGroup2D.cs'
$outlineDefaultsPath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutlineDefaults.cs'
$shaderPath = Join-Path $repoRoot 'Assets\Shaders\2D\SpriteOutlineMerge2D.shader'
$editorPaths = @(
    (Join-Path $repoRoot 'Assets\Editor\Rendering\OutlineShadowLabDriverEditor.cs'),
    (Join-Path $repoRoot 'Assets\Editor\Rendering\OutlineShadowLabBuilder.cs'),
    (Join-Path $repoRoot 'Assets\Editor\Rendering\OutlineShadowLabAudit.cs')
)
$stubPath = Join-Path $PSScriptRoot 'OutlineMergeUnityStubs.cs'
$testPath = Join-Path $PSScriptRoot 'OutlineMergeTests.cs'
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

$member = Get-Content -Raw -LiteralPath $memberPath
$renderer = Get-Content -Raw -LiteralPath $rendererPath
$driver = Get-Content -Raw -LiteralPath $driverPath
$shader = Get-Content -Raw -LiteralPath $shaderPath
$editor = ($editorPaths | ForEach-Object { Get-Content -Raw -LiteralPath $_ }) -join "`n"

Assert-Contains $renderer 'MaskPassIndex\s*=\s*0' 'Mask pass index contract is missing.'
Assert-Contains $renderer 'CandidatePassIndex\s*=\s*1' 'Candidate pass index contract is missing.'
Assert-Contains $renderer 'command\.DrawMesh.*MaskPassIndex' 'Mask pass is not explicitly scheduled.'
Assert-Contains $renderer 'command\.DrawMesh.*CandidatePassIndex' 'Candidate pass is not explicitly scheduled.'
Assert-Contains $renderer 'RenderPipelineManager\.beginCameraRendering' 'Selected camera SRP scheduling is missing.'
Assert-Contains $renderer 'RoundUpToEight' '8-pixel budget rounding is missing.'
Assert-Contains $renderer 'SystemInfo\.maxTextureSize' 'Device texture limit diagnostic is missing.'
Assert-Contains $renderer 'CaptureHostState' 'Host state snapshot is missing.'
Assert-Contains $renderer 'RestoreHostState' 'Host state restore is missing.'
Assert-Contains $renderer 'CachedUnchanged' 'Unchanged-frame cache evidence is missing.'
Assert-Contains $renderer 'sortingOrder\s*-\s*2' 'Shadow host order contract is missing.'
Assert-Contains $renderer 'sortingOrder\s*-\s*1' 'Outline host order contract is missing.'
Assert-Contains $renderer 'SortingDomainId' 'SortingGroup isolation-domain validation is missing.'
Assert-Contains $renderer 'SetShaderPassEnabled\(OffscreenLightMode, false\)' 'Host materials must disable the custom offscreen LightMode.'
Assert-Contains $renderer 'SetShaderPassEnabled\(ShadowDisplayLightMode, shadowRole\)' 'Shadow role must be selected by LightMode.'
Assert-Contains $renderer 'SetShaderPassEnabled\(OutlineDisplayLightMode, !shadowRole\)' 'Outline role must be selected by LightMode.'
Assert-NotContains $renderer 'SetShaderPassEnabled\("(?:MaskUnion|OutlineCandidate|ShadowDisplay|OutlineDisplay)"' 'Pass Name must not be used as a SetShaderPassEnabled key.'
Assert-Contains $renderer 'UpdateDisplayQuad\(displayQuad, renderer\.transform\.worldToLocalMatrix, rectangle, worldZ\)' 'Display hosts must map the desired world rectangle into their own local mesh space.'
Assert-Contains $renderer 'worldToLocal\.MultiplyPoint3x4' 'World-to-local display vertex mapping is missing.'
Assert-Contains $renderer 'mesh\.RecalculateBounds\(\)' 'Runtime display mesh bounds must be refreshed for culling.'
Assert-Contains $renderer 'shadowHost\.transform\.localToWorldMatrix\.GetHashCode\(\)' 'Shadow host transform must invalidate cached display geometry.'
Assert-Contains $renderer 'outlineHost\.transform\.localToWorldMatrix\.GetHashCode\(\)' 'Outline host transform must invalidate cached display geometry.'
Assert-Contains $renderer 'sprite\.textureRectOffset\s*-\s*sprite\.pivot' 'Tight sprite local sampling must preserve the cropped texture offset from the source pivot.'
Assert-Contains $renderer 'sprite\.textureRect\.size\s*/\s*pixelsPerUnit' 'Tight sprite local sampling size must preserve one texture texel per source pixel.'
Assert-Contains $renderer 'if\s*\(flipX\)\s*minimum\.x\s*=\s*-minimum\.x\s*-\s*size\.x' 'FlipX must relocate an asymmetric cropped support rect before mirroring UVs.'
Assert-Contains $renderer 'if\s*\(flipY\)\s*minimum\.y\s*=\s*-minimum\.y\s*-\s*size\.y' 'FlipY must relocate an asymmetric cropped support rect before mirroring UVs.'
Assert-Contains $renderer 'Renderer\.flipX\.GetHashCode\(\)' 'FlipX changes must invalidate the cached member mapping.'
Assert-Contains $renderer 'Renderer\.flipY\.GetHashCode\(\)' 'FlipY changes must invalidate the cached member mapping.'
Assert-NotContains $renderer 'SetVector\("_SpriteLocalRect",\s*new Vector4\(sprite\.bounds' 'Full sprite.bounds must not stretch a Tight textureRect across the source canvas.'
Assert-NotContains $renderer 'renderer\.transform\.SetPositionAndRotation\(center|renderer\.transform\.localScale\s*=\s*scale' 'World rectangle dimensions must not be assigned as child-host localScale.'
if (([regex]::Matches($renderer, 'new\s+RenderTexture\s*\(')).Count -ne 1) { throw 'Expected one shared allocation site invoked for exactly two persistent fields.' }
$staticAssertions++
Assert-Contains $renderer 'EnsureTexture\(ref maskTexture' 'Mask RT ownership is missing.'
Assert-Contains $renderer 'EnsureTexture\(ref candidateTexture' 'Candidate RT ownership is missing.'
Assert-NotContains ($renderer + $shader) 'GetTemporary|ReleaseTemporary|Camera\.Render|Shader\.SetGlobal|CommandBuffer\.SetGlobal|RendererFeature' 'Forbidden hidden RT/camera/global/feature path found.'
Assert-NotContains $renderer '\.material\b' 'Renderer.material must not allocate hidden instances.'

Assert-Contains $member 'NearestOwner' 'Nearest-group identity contract is missing.'
Assert-Contains $member 'ForceOn' 'ForceOn state is missing.'
Assert-Contains $member 'ForceOff' 'ForceOff state is missing.'
Assert-Contains $member 'useGroupOutlineParameters' 'Formal outline parameter-source state is missing.'
Assert-Contains $member 'ConfigureFormal' 'Formal member adapter entry is missing.'
Assert-Contains $renderer 'Member\.ResolveOutlineColor\(groupColor\)' 'Renderer must resolve outline parameters independently from merge mode.'
Assert-Contains $renderer 'groupShadowEffectEnabled\s*&&\s*HasMergedShadowContributor' 'Merged shadow must keep effect enable separate from merge participation.'
Assert-Contains $renderer 'IsReadyFor' 'Formal ownership readiness contract is missing.'
Assert-Contains $shader 'BlendOp\s+Max' 'Max-alpha mask union is missing.'
Assert-Contains $shader 'ColorMask\s+RG' 'Entity/shadow channel reuse is missing.'
Assert-Contains $shader 'entity\s*=.*_EntityMaskTex' 'Group entity exclusion is missing.'
Assert-Contains $shader 'Blend\s+SrcAlpha\s+OneMinusSrcAlpha' 'Stable source-over candidate/display blending is missing.'
Assert-Contains $shader 'saturate\(neighbor\s*-\s*center\)\s*\*\s*\(1\.0\s*-\s*entity\)\s*\*\s*_OutlineColor\.a' 'Merged candidate coverage must consume outline Alpha without changing the entity mask.'
Assert-Contains $member 'return\s+useGroupOutlineParameters\s*\?\s*groupColor\s*:\s*outlineColor' 'Merged parameter resolution must preserve the complete local/group Color value, including Alpha.'
Assert-Contains $renderer 'material\.SetColor\("_OutlineColor",\s*runtime\.ResolveOutlineColor\(groupOutlineColor\)\)' 'Merged renderer must pass the resolved outline Color, including Alpha, to the material.'
$offscreenLightModeCount = ([regex]::Matches($shader, '"LightMode"\s*=\s*"SpriteOutlineMergeOffscreen"')).Count
if ($offscreenLightModeCount -ne 2) { throw "Both explicit offscreen passes require the custom LightMode; found $offscreenLightModeCount." }
$staticAssertions++
Assert-Contains $shader '(?s)Name\s+"ShadowDisplay".*?"LightMode"\s*=\s*"Universal2D"' 'Shadow host Universal2D role is missing.'
Assert-Contains $shader '(?s)Name\s+"OutlineDisplay".*?"LightMode"\s*=\s*"SRPDefaultUnlit"' 'Outline host SRPDefaultUnlit role is missing.'
$passCount = ([regex]::Matches($shader, '(?m)^\s*Pass\s*$')).Count
if ($passCount -ne 4) { throw "Expected four explicitly indexed shader passes, found $passCount." }
$staticAssertions++

Assert-Contains $driver 'VERIFY-TEMP' 'Protected Lab driver marker is missing.'
Assert-Contains $driver 'ExportBudgetState' 'Budget export is missing.'
Assert-Contains $editor 'member\.MergeShadow\s*!=\s*OutlineMergeOverride\.ForceOff' 'Merged-shadow Inspector branch is missing.'
Assert-Contains $editor 'existing sample opened without modification' 'Idempotent Builder protection is missing.'
Assert-Contains $editor 'AddComponent<SpriteOutline2D>' 'ForceOff/original independent shadow comparison is missing.'
Assert-Contains $editor 'Audit Outline Shadow Merge \(Read Only\)' 'Read-only Audit entry is missing.'
Assert-Contains $editor 'FormalComponentMatrix_Group' 'Formal-component Lab matrix is missing.'
Assert-Contains $editor 'ParametersLocal_FollowGroup' 'Local-parameter + FollowGroup cross sample is missing.'
Assert-Contains $editor 'ParametersGroup_ForceOn_Nested' 'Group-parameter + ForceOn cross sample is missing.'
Assert-Contains $editor 'DisableUnrelatedBehaviours' 'Real Cat fixtures must isolate unrelated runtime drivers.'
Assert-Contains $editor 'Create Sprite Outline Defaults \(Once\)' 'Explicit one-time Defaults creation entry is missing.'
Assert-Contains $editor 'PrefabUtility\.GetCorrespondingObjectFromSource' 'Audit must prove real source-prefab fixtures.'
Assert-Contains $editor 'Apply Forward_2 Independent Outline Fix' 'Explicit Forward_2 repair entry is missing.'
Assert-Contains $editor 'ValidateForward2Baseline' 'Forward_2 repair must reject unknown scene pre-state.'
Assert-Contains $editor 'VfxLabOutlinePadded\.png' 'Dedicated padded outline fixture path is missing.'
Assert-Contains $editor 'PaddedFixtureSize\s*=\s*128' 'Padded fixture canvas must be 128 pixels.'
Assert-Contains $editor 'PaddedFixtureOpaqueSize\s*=\s*64' 'Padded fixture opaque center must be 64 pixels.'
Assert-Contains $editor 'PaddedFixtureBorder\s*=\s*32' 'Padded fixture border must be 32 pixels.'
Assert-Contains $editor 'TextureImporterSettings' 'Unity 2022.3 importer settings path is missing.'
Assert-Contains $editor 'updatedSettings\.spriteMeshType\s*=\s*SpriteMeshType\.FullRect' 'Padded fixture must preserve its transparent FullRect mesh.'
Assert-NotContains $editor 'SetTextureSettings\(currentSettings\)' 'The pre-change TextureImporterSettings snapshot must never be applied.'
Assert-Contains $editor '(?s)importer\.textureType\s*=\s*TextureImporterType\.Sprite;.*?importer\.spriteImportMode\s*=\s*SpriteImportMode\.Single;.*?importer\.spritePixelsPerUnit\s*=\s*PaddedFixturePixelsPerUnit;.*?importer\.alphaSource\s*=\s*TextureImporterAlphaSource\.FromInput;.*?var updatedSettings\s*=\s*new TextureImporterSettings\(\);.*?importer\.ReadTextureSettings\(updatedSettings\);.*?updatedSettings\.spriteAlignment\s*=\s*\(int\)SpriteAlignment\.Center;.*?updatedSettings\.spritePivot\s*=\s*new Vector2\(0\.5f,\s*0\.5f\);.*?updatedSettings\.spriteMeshType\s*=\s*SpriteMeshType\.FullRect;.*?importer\.SetTextureSettings\(updatedSettings\);.*?importer\.SaveAndReimport\(\);' 'Importer must re-read settings after direct assignments and apply only the updated center/FullRect snapshot.'
Assert-Contains $editor '(?s)Sprite paddedFixture\s*=\s*EnsurePaddedFixtureAsset\(\);.*?TryValidatePaddedFixture\(out fixtureError\).*?bool changed\s*=\s*EnsureForward2FormalConfiguration' 'Complete padded fixture validation must run before any Forward_2 scene mutation.'
Assert-Contains $editor 'settings\.spriteAlignment\s*!=\s*\(int\)SpriteAlignment\.Center' 'Importer validation must prove center sprite alignment.'
Assert-Contains $editor 'importer\.alphaSource\s*!=\s*TextureImporterAlphaSource\.FromInput' 'Importer validation must prove input alpha ownership.'
Assert-Contains $editor 'sprite\.pixelsPerUnit,\s*PaddedFixturePixelsPerUnit' 'Imported Sprite validation must prove the effective PPU.'
Assert-Contains $editor 'File\.WriteAllBytes\(PaddedFixturePath' 'Explicit fixture generation write is missing.'
Assert-Contains $editor 'GroupMatchesBackend' 'Audit must prove the formal group remains the backend parameter authority.'
Assert-Contains $editor 'forwardGeometryPreserved' 'Audit must protect the original Forward sample geometry and sorting.'
Assert-Contains $driver 'FocusForward2ReviewCamera' 'Forward_2 Play-only fixed camera entry is missing.'
Assert-Contains $driver 'RestoreForward2ReviewCamera' 'Forward_2 camera restore entry is missing.'
Assert-Contains $driver 'if\s*\(!Application\.isPlaying\)' 'Forward_2 focus must not mutate the saved Edit Mode camera.'
Assert-Contains $driver '(?s)private void OnDisable\(\).*?RestoreForward2ReviewCamera' 'Driver disable must restore the temporary camera state.'
Assert-NotContains $editor 'InitializeOnLoad|AssetPostprocessor|DidReloadScripts|InitializeOnEnterPlayMode' 'Builder/Audit must not auto-run.'

Add-Type -Path @($stubPath, $testPath)
$runtimeResult = [OutlineMergeTests]::Run()

$projectVersion = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt')
$versionMatch = [regex]::Match($projectVersion, 'm_EditorVersion:\s*([^\r\n]+)')
if (-not $versionMatch.Success) { throw 'Could not resolve Unity editor version.' }
$unityVersion = $versionMatch.Groups[1].Value.Trim()
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = @("C:/Program Files/Unity/Hub/Editor/$unityVersion/Editor/Unity.exe", "C:/Program Files/Unity $unityVersion/Editor/Unity.exe") | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $UnityEditorPath -or -not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw 'Pass -UnityEditorPath for the project Editor.' }
if (-not (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion.StartsWith($unityVersion, [StringComparison]::Ordinal)) { throw 'Editor version does not match ProjectVersion.txt.' }
$unityData = Join-Path (Split-Path -Parent $UnityEditorPath) 'Data'
$dotnet = Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $unityData 'DotNetSdkRoslyn\csc.dll'
$framework = Join-Path $unityData 'MonoBleedingEdge\lib\mono\4.7.1-api'
$managed = Join-Path $unityData 'Managed'
foreach ($required in @($dotnet, $csc, $framework, $managed)) {
    if (-not (Test-Path -LiteralPath $required)) { throw "Required Unity compiler input is missing: $required" }
}

$buildDirectory = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '.outline-merge-validation'))
$renderingRoot = [System.IO.Path]::GetFullPath($PSScriptRoot) + [System.IO.Path]::DirectorySeparatorChar
if (-not $buildDirectory.StartsWith($renderingRoot, [System.StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe validation directory: $buildDirectory" }
if (Test-Path -LiteralPath $buildDirectory) { Remove-Item -LiteralPath $buildDirectory -Recurse -Force }
New-Item -ItemType Directory -Path $buildDirectory | Out-Null

$runtimeDll = Join-Path $buildDirectory 'OutlineMerge.Runtime.dll'
$editorDll = Join-Path $buildDirectory 'OutlineMerge.Editor.dll'
$frameworkReferences = @(
    (Join-Path $framework 'mscorlib.dll'),
    (Join-Path $framework 'System.dll'),
    (Join-Path $framework 'System.Core.dll'),
    (Join-Path $framework 'Facades\netstandard.dll'),
    (Join-Path $framework 'Facades\System.Runtime.dll')
)
$unityReferences = Get-ChildItem -LiteralPath $managed -Recurse -Filter '*.dll' | Select-Object -ExpandProperty FullName
$projectReferences = @(
    (Join-Path $repoRoot 'Library\ScriptAssemblies\UnityEngine.UI.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.TextMeshPro.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.RenderPipelines.Core.Runtime.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.RenderPipelines.Universal.Runtime.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.RenderPipelines.Universal.2D.Runtime.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.RenderPipelines.Universal.2D.Internal.dll')
) | Where-Object { Test-Path -LiteralPath $_ }

try {
    $runtimeArguments = @($csc, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', ('/out:' + $runtimeDll))
    $runtimeArguments += ($frameworkReferences + $unityReferences + $projectReferences | Select-Object -Unique | ForEach-Object { '/reference:' + $_ })
    $runtimeArguments += @($memberPath, $rendererPath, $originalOutlinePath,
        $originalOutlineGroupPath, $outlineDefaultsPath, $driverPath)
    & $dotnet @runtimeArguments
    if ($LASTEXITCODE -ne 0) { throw "True runtime compilation failed with exit code $LASTEXITCODE." }

    $editorArguments = @($csc, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', ('/out:' + $editorDll))
    $editorArguments += ($frameworkReferences + $unityReferences + $projectReferences + @($runtimeDll) | Select-Object -Unique | ForEach-Object { '/reference:' + $_ })
    $editorArguments += $editorPaths
    & $dotnet @editorArguments
    if ($LASTEXITCODE -ne 0) { throw "True Editor compilation failed with exit code $LASTEXITCODE." }
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
    note = 'Source/stub/true assembly compilation does not replace the recovered AI-000103 real Tight Sprite normal-frame Play/GPU, cleanup, or deferred human review.'
} | ConvertTo-Json -Depth 3
