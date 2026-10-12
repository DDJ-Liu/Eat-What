[CmdletBinding()]
param([switch]$CompileOnly)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$source = Join-Path $repoRoot 'Assets/Editor/Fonts/FontSwapOnce.cs'
$mapping = Join-Path $repoRoot '.ai-workspace/outputs/control/U1-PRE-20260923/PRE1_字体材质迁移逐项草案.csv'
if ((Get-FileHash -LiteralPath $mapping).Hash -ne 'EB0CED01C1BBB0050DCF30BAA3D5354E9F2C7326C3DF97375DE87467D8FE347B') { throw 'Approved mapping changed.' }
$lineCount = [IO.File]::ReadAllLines($source).Length
# AI216 adds one cached hash read and its explanation to the AI213 protection.
# 264 lines remains a documented exception; the original 150-line target is unmet.
if ($lineCount -gt 264) { throw "FontSwapOnce exceeds the documented AI216 264-line exception: $lineCount" }
$materialGuids = @{
    'SourceHan' = '8e525bf3cdd33804189d28645ca0f556'
    'LegacyShortCycle' = '87a7857d7b1355641aee5b8ac50c5c1b'
    'Heavy' = '389683e661c8d24468fea402edba0841'
}
$materialEvidence = @()
foreach ($name in $materialGuids.Keys) {
    $path = Join-Path $repoRoot "Assets/Font/Heavy/Materials/ShortCycle_Static_From$name.mat"
    if (-not (Test-Path -LiteralPath $path)) { throw "Existing material missing: $path" }
    if ([IO.File]::ReadAllText("$path.meta") -notmatch "(?m)^guid: $($materialGuids[$name])\s*$") { throw "Existing GUID changed: $path" }
    $materialEvidence += [pscustomobject]@{ name=$name; guid=$materialGuids[$name]; sha256=(Get-FileHash $path).Hash; metaSha256=(Get-FileHash "$path.meta").Hash }
}
# Mutation control differs only in class name and the two binding setters.
$fixtureDirectory = Join-Path $repoRoot ('.ai-workspace/outputs/fontswap-once-fixtures/' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($fixtureDirectory)
$production = [IO.File]::ReadAllText($source).Replace("`r`n", "`n")
$newOrder = "text.fontSharedMaterial = material;`n            text.font = font;"
$oldOrder = "text.font = font;`n            text.fontSharedMaterial = material;"
if (-not $production.Contains($newOrder)) { throw 'Exact production binding-order mutant anchor missing.' }
$mutantPath = Join-Path $fixtureDirectory 'FontSwapOnceOldOrder.cs'
$mutant = $production.Replace('public static class FontSwapOnce', 'public static class FontSwapOnceOldOrder').Replace($newOrder, $oldOrder)
[IO.File]::WriteAllText($mutantPath, $mutant, [Text.UTF8Encoding]::new($false))
# Second control restores only the old Memory sampling order, leaving all other guards intact.
$hashRead = "                // The getter can populate the serialized texture hash; sample it once before JSON.`n                var imageHash = textures[0].imageContentsHash;`n"
$hashTail = 'EditorJsonUtility.ToJson(textures[0]) + "\n" + imageHash;'
if (-not $production.Contains($hashRead) -or -not $production.Contains($hashTail)) { throw 'Exact production snapshot mutant anchors missing.' }
$oldSnapshotPath = Join-Path $fixtureDirectory 'FontSwapOnceOldSnapshot.cs'
$oldSnapshot = $production.Replace('public static class FontSwapOnce', 'public static class FontSwapOnceOldSnapshot').Replace($hashRead, '').Replace($hashTail, 'EditorJsonUtility.ToJson(textures[0]) + "\n" + textures[0].imageContentsHash;')
[IO.File]::WriteAllText($oldSnapshotPath, $oldSnapshot, [Text.UTF8Encoding]::new($false))
Add-Type -Path @($source, $mutantPath, $oldSnapshotPath, (Join-Path $repoRoot 'Assets/Editor/DataTables/DataTableValidationCore.cs'), (Join-Path $PSScriptRoot 'FontSwapOnceHarness.cs'))
if ($CompileOnly) { [pscustomobject]@{compiled=$true; sourceLines=$lineCount; productionSha256=(Get-FileHash -LiteralPath $source).Hash; mutantSource=$mutantPath} | ConvertTo-Json; return }
Push-Location $repoRoot
try { $result = [FontSwapOnceHarness]::Run([IO.File]::ReadAllText($mapping)) | ConvertFrom-Json }
finally { Pop-Location }
$result | Add-Member sourceLines $lineCount
$result | Add-Member original150LineLimitMet ($lineCount -le 150)
$result | Add-Member lineLimitException 'AI216: 264 lines (+2 for single hash read before JSON and explanation); all AI210/AI213 guards retained. Original 150 target remains unmet.'
$result | Add-Member mutantSource $mutantPath
$result | Add-Member oldSnapshotSource $oldSnapshotPath
$result | Add-Member oldSnapshotSha256 (Get-FileHash -LiteralPath $oldSnapshotPath).Hash
$result | Add-Member mutantSha256 (Get-FileHash -LiteralPath $mutantPath).Hash
$result | Add-Member productionSha256 (Get-FileHash -LiteralPath $source).Hash
$result | Add-Member existingMaterials $materialEvidence
$result | Add-Member scope 'Unmodified production C#, a two-setter old-binding mutant and an old-sampling mutant; minimal AI215 cache/hash boundary model and TMP atlas/padding doubles, not full Unity semantics. Actual Editor/native ratio/serialization/render proof belongs to the new ENGINE validation before original AI207 resume.'
$result | ConvertTo-Json -Depth 6