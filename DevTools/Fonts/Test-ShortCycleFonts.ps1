[CmdletBinding()]
param([string]$UnityEditorPath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$builderPath = Join-Path $repoRoot 'Assets\Editor\Fonts\ShortCycleFontAtlasBuilder.cs'
$swapPath = Join-Path $repoRoot 'Assets\Editor\Fonts\ShortCycleFontSwapTool.cs'
$layoutAuditPath = Join-Path $repoRoot 'Assets\Editor\ShortCycle\ShortCycleLayoutAudit.cs'
$parserPath = Join-Path $repoRoot 'Assets\Editor\DataTables\DataTableValidationCore.cs'
$legacyPath = Join-Path $repoRoot 'Assets\Font\Heavy\ShortCycle_Heavy_WithFallback.asset'
$localizationPath = Join-Path $repoRoot 'DataTables\Localization\Localization.csv'
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

$builder = Get-Content -Raw -LiteralPath $builderPath
$swap = Get-Content -Raw -LiteralPath $swapPath
$layoutAudit = Get-Content -Raw -LiteralPath $layoutAuditPath

Assert-Contains $builder 'DataTableCsvParser\.Parse\s*\(' 'Builder must use the project CSV parser.'
Assert-Contains $builder 'LegacyFontAssetPath' 'Builder must source the reviewed legacy character table.'
Assert-Contains $builder 'LocalizationCsvPath' 'Builder must include localization CSV text.'
Assert-Contains $builder 'requiredCharacters\.UnionWith\(localizationCharacters\)' 'Builder must union both character sources.'
Assert-Contains $builder 'missing\.Length\s*>\s*0' 'Builder must gate atlas writes on missing characters.'
Assert-Contains $builder 'ClearFontAssetData\(false\)' 'Dynamic fallback needs an explicit clear-for-commit path.'
Assert-Contains $builder 'SetFallbackChain\(asset,\s*sourceHan\)' 'Dynamic fallback must end at SourceHan.'
Assert-Contains $builder 'SetFallbackChain\(asset,\s*dynamicFallback\)' 'Static asset must fall back to the dynamic asset.'
Assert-Contains $builder 'approvalStatus\s*=\s*"PENDING_REVIEW"' 'Generated manifest must require explicit review.'
Assert-NotContains $builder 'TMP_Settings\s*\.' 'Builder must not mutate global TMP settings.'
Assert-NotContains $builder '[\u4e00-\u9fff]' 'Builder source must not hardcode a Chinese character range.'

Assert-Contains $swap 'ValidateAllowlist\s*\(' 'Swap tool must require an explicit allowlist.'
Assert-Contains $swap 'safeToSwitchOnlyFontAsset' 'Swap preview must report per-entry safety.'
Assert-Contains $swap 'sourceFontCompatible' 'Swap preview must report source-font compatibility.'
Assert-Contains $swap 'materialCompatible' 'Swap preview must report material compatibility.'
Assert-Contains $swap 'MATERIAL_ATLAS_INCOMPATIBLE' 'Swap preview must explain incompatible material atlases.'
Assert-Contains $swap 'FindProperty\("m_fontAsset"\)' 'Execute must address only the TMP font field.'
Assert-Contains $swap 'FindProperty\("m_sharedMaterial"\)' 'Approved execute must address the paired TMP material field.'
Assert-Contains $swap 'PreviewApprovedMigration' 'Approved migration must expose a read-only Editor preflight API.'
Assert-Contains $swap 'ExecuteApprovedMigration' 'Approved migration must expose a transactional execute API.'
Assert-Contains $swap 'RollbackApprovedMigration' 'Approved migration must expose an idempotent rollback API.'
Assert-Contains $swap 'ApprovedMappingSha256' 'Approved migration must bind the reviewed CSV hash.'
Assert-Contains $swap 'CopyAtlasProperties' 'Approved material clones must have an explicit atlas replacement boundary.'
foreach ($atlasProperty in @('_MainTex','_TextureWidth','_TextureHeight','_GradientScale')) {
    Assert-Contains $swap ([regex]::Escape($atlasProperty)) "Approved migration lost atlas property $atlasProperty."
}
Assert-NotContains $swap 'm_text|m_Text|sortingOrder\s*=' 'Execute must not rewrite text or sorting.'
Assert-Contains $swap 'Complete a read-only preflight over the whole allowlist' 'Execute must preflight before writes.'

Assert-Contains $layoutAudit 'ShortCycleStaticFontPath' 'Layout audit must identify the future static font.'
Assert-Contains $layoutAudit 'spacesStaticFontValid' 'Layout audit must report the Spaces static-font assertion.'
Assert-Contains $layoutAudit 'spacesStaticFontMismatchPaths' 'Layout audit must list every mismatch.'
foreach ($preserved in @('fixT5FourMarkers','fixT5ClueAnchors','fixT5ClueHitArea','fixT5LegacyOpenButtonRetired')) {
    Assert-Contains $layoutAudit ([regex]::Escape($preserved)) "Layout audit lost preserved assertion $preserved."
}

# Build the actual deterministic character union without Unity or a CSV approximation.
if (-not ('EatWhat.DataTables.DataTableCsvParser' -as [type])) {
    Add-Type -Path $parserPath
}
$parsed = [EatWhat.DataTables.DataTableCsvParser]::Parse(
    [System.IO.File]::ReadAllText($localizationPath, [System.Text.UTF8Encoding]::new($false, $true)))
if (-not $parsed.IsValid) { throw "Localization parser errors: $($parsed.Errors -join ' | ')" }
$zhIndex = $parsed.Table.GetHeaderIndex('zh_cn')
if ($zhIndex -lt 0) { throw 'Localization.csv has no zh_cn column.' }
$legacyCharacters = [System.Collections.Generic.HashSet[uint]]::new()
foreach ($match in [regex]::Matches([System.IO.File]::ReadAllText($legacyPath), '(?m)^\s*m_Unicode:\s*(\d+)\s*$')) {
    [void]$legacyCharacters.Add([uint]::Parse($match.Groups[1].Value))
}
if ($legacyCharacters.Count -ne 216) {
    throw "Reviewed legacy atlas character count changed: $($legacyCharacters.Count), expected 216."
}
$localizationCharacters = [System.Collections.Generic.HashSet[uint]]::new()
foreach ($row in $parsed.Table.Rows) {
    if ($row.Values.Count -le $zhIndex) { continue }
    $value = $row.Values[$zhIndex]
    for ($index = 0; $index -lt $value.Length; $index++) {
        $character = $value[$index]
        if ([char]::IsHighSurrogate($character) -and $index + 1 -lt $value.Length -and
            [char]::IsLowSurrogate($value[$index + 1])) {
            [void]$localizationCharacters.Add([uint][char]::ConvertToUtf32($character, $value[++$index]))
        }
        elseif (-not [char]::IsSurrogate($character)) {
            [void]$localizationCharacters.Add([uint]$character)
        }
    }
}
$requiredCharacters = [System.Collections.Generic.HashSet[uint]]::new($legacyCharacters)
$requiredCharacters.UnionWith($localizationCharacters)
if (-not $requiredCharacters.IsSupersetOf($legacyCharacters) -or
    -not $requiredCharacters.IsSupersetOf($localizationCharacters)) {
    throw 'Deterministic character union is incomplete.'
}

# Compile the new Editor sources against the installed project's real Unity 6 / TMP DLLs.
$projectVersion = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ProjectSettings\ProjectVersion.txt')
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
    throw 'Pass -UnityEditorPath for the project Editor.'
}
if (-not (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion.StartsWith(
        $unityVersion, [StringComparison]::Ordinal)) {
    throw 'Editor version does not match ProjectVersion.txt.'
}
$unityData = Join-Path (Split-Path -Parent $UnityEditorPath) 'Data'
$dotnet = Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$csc = Join-Path $unityData 'DotNetSdkRoslyn\csc.dll'
$unityManaged = Join-Path $unityData 'Managed\UnityEngine'
$framework = Join-Path $unityData 'MonoBleedingEdge\lib\mono\4.7.1-api'
$references = @(
    (Join-Path $framework 'mscorlib.dll'),
    (Join-Path $framework 'System.dll'),
    (Join-Path $framework 'System.Core.dll'),
    (Join-Path $framework 'Facades\netstandard.dll'),
    (Join-Path $framework 'Facades\System.Runtime.dll'),
    (Join-Path $unityManaged 'UnityEngine.dll'),
    (Join-Path $unityManaged 'UnityEngine.CoreModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.JSONSerializeModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.Physics2DModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.TextCoreFontEngineModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.TextCoreTextEngineModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $unityManaged 'UnityEngine.UIModule.dll'),
    (Join-Path $unityManaged 'UnityEditor.dll'),
    (Join-Path $unityManaged 'UnityEditor.CoreModule.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Unity.TextMeshPro.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\UnityEngine.UI.dll'),
    (Join-Path $repoRoot 'Library\ScriptAssemblies\Assembly-CSharp.dll')
)
foreach ($required in @($dotnet, $csc) + $references) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required Unity compile input is missing: $required"
    }
}
$buildDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhat-ShortCycleFonts-' + [guid]::NewGuid().ToString('N'))
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$resolvedBuildDirectory = [System.IO.Path]::GetFullPath($buildDirectory)
if (-not $resolvedBuildDirectory.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe font validation directory: $resolvedBuildDirectory"
}
[void][System.IO.Directory]::CreateDirectory($resolvedBuildDirectory)
try {
    $outputDll = Join-Path $resolvedBuildDirectory 'ShortCycleFonts.Editor.dll'
    $arguments = @($csc, '/nologo', '/target:library', '/langversion:latest', '/nostdlib+', ('/out:' + $outputDll))
    $arguments += $references | Select-Object -Unique | ForEach-Object { '/reference:' + $_ }
    $arguments += @($parserPath, $builderPath, $swapPath, $layoutAuditPath)
    $compilerOutput = @(& $dotnet @arguments 2>&1)
    if ($compilerOutput.Count -gt 0) { $compilerOutput | Write-Output }
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $outputDll -PathType Leaf)) {
        throw "ShortCycle font Editor sources failed real Unity 6 DLL compilation (exit $LASTEXITCODE)."
    }
    $compiledBytes = (Get-Item -LiteralPath $outputDll).Length
}
finally {
    if (Test-Path -LiteralPath $resolvedBuildDirectory) {
        Remove-Item -LiteralPath $resolvedBuildDirectory -Recurse -Force
    }
}

$python = Get-Command python -ErrorAction Stop
$guardOutput = @(& $python.Source (Join-Path $repoRoot 'DevTools\GitHooks\test-font-guard.py') 2>&1)
if ($guardOutput.Count -gt 0) { $guardOutput | Write-Output }
if ($LASTEXITCODE -ne 0) { throw "Font guard fake-repository tests failed with exit $LASTEXITCODE." }
$guardResult = ($guardOutput -join [Environment]::NewLine) | ConvertFrom-Json

[pscustomobject]@{
    success = $true
    unityVersion = $unityVersion
    staticAssertions = $staticAssertions
    legacyCharacterCount = $legacyCharacters.Count
    localizationCharacterCount = $localizationCharacters.Count
    requiredCharacterCount = $requiredCharacters.Count
    compiledEditorDllBytes = $compiledBytes
    guardCases = $guardResult.count
} | ConvertTo-Json -Depth 4
