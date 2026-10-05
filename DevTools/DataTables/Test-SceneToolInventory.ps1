[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not ('SceneToolInventory' -as [type])) {
    Add-Type -Path (Join-Path $repositoryRoot 'Assets/Scripts/DataBase/CookingData/SceneToolInventory.cs')
}

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}

function New-Tool([string]$key, [string]$owner, [string]$hierarchy,
    [string]$area, [string]$hierarchyArea, [string]$id, [bool]$active = $true,
    [bool]$instance = $true, [string]$kind = 'carrier') {
    $tool = [SceneToolBinding]::new()
    $tool.objectKey = $key
    $tool.ownerObjectKey = $owner
    $tool.hierarchyOwnerObjectKey = $hierarchy
    $tool.areaId = $area
    $tool.hierarchyAreaId = $hierarchyArea
    $tool.toolId = $id
    $tool.toolKind = $kind
    $tool.active = $active
    $tool.sceneInstance = $instance
    $tool.mappingSource = [SceneToolInventory]::ExplicitMapping
    return $tool
}

$bindings = [System.Collections.Generic.List[SceneToolBinding]]::new()
[void]$bindings.Add((New-Tool 'scene/1' 'room/1' 'room/1' 'prep' 'prep' 'tool_board'))
[void]$bindings.Add((New-Tool 'scene/2' 'room/1' 'room/1' 'prep' 'prep' 'tool_board'))
[void]$bindings.Add((New-Tool 'scene/3' 'room/2' 'room/2' 'stove' 'stove' 'tool_pot'))
[void]$bindings.Add((New-Tool 'scene/4' 'room/1' 'room/1' 'prep' 'prep' 'tool_knife' $false))
[void]$bindings.Add((New-Tool 'prefab/1' 'room/1' 'room/1' 'prep' 'prep' 'tool_pan' $true $false))
[void]$bindings.Add((New-Tool 'scene/5' 'room/1' 'room/2' 'prep' 'stove' 'tool_scissors'))
[void]$bindings.Add((New-Tool 'scene/6' 'room/1' 'room/1' 'prep' 'prep' ''))
[void]$bindings.Add((New-Tool 'scene/1' 'room/1' 'room/1' 'prep' 'prep' 'tool_pan'))
$result = [SceneToolInventory]::Resolve($bindings)
Assert-True ($result.toolsByArea['prep'].Count -eq 1 -and $result.HasTool('prep','tool_board')) 'Same-ID instances must deduplicate.'
Assert-True ($result.HasTool('stove','tool_pot') -and -not $result.HasTool('prep','tool_pot')) 'Neighbor room must not leak.'
Assert-True (-not $result.HasTool('prep','tool_knife') -and -not $result.HasTool('prep','tool_pan')) 'Inactive and prefab assets must not grant availability.'
Assert-True ($result.issues.Count -eq 3) 'Mismatched owner, unmapped ID and duplicate object must be diagnosed.'
$bindings[2].areaId = 'prep'
$bindings[2].ownerObjectKey = 'room/1'
$bindings[2].hierarchyAreaId = 'prep'
$bindings[2].hierarchyOwnerObjectKey = 'room/1'
$moved = [SceneToolInventory]::Resolve($bindings)
Assert-True ($moved.HasTool('prep','tool_pot') -and -not $moved.HasTool('stove','tool_pot')) 'Movement must refresh ownership on re-query.'

$validator = Join-Path $PSScriptRoot 'Validate-DataTables.ps1'
$withoutManifest = & $validator -NoExit -Quiet
Assert-True ($withoutManifest.sceneInventory.status -eq 'UNVERIFIED' -and -not $withoutManifest.isFullyVerified) 'Missing manifest must not claim scene verification.'
$requiredManifest = & $validator -RequireSceneInventory -NoExit -Quiet
Assert-True (-not $requiredManifest.isValid -and -not $requiredManifest.isFullyVerified) 'Formal scene gate must reject missing manifest.'
$temporary = Join-Path ([System.IO.Path]::GetTempPath()) ('eatwhat-scene-tools-' + [guid]::NewGuid().ToString('N') + '.json')
$stagedRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('eatwhat-scene-tools-data-' + [guid]::NewGuid().ToString('N'))
try {
    [System.IO.File]::WriteAllText($temporary, '{"formatVersion":1,"projectGuid":"wrong"}')
    $stale = & $validator -SceneInventoryPath $temporary -NoExit -Quiet
    Assert-True (-not $stale.isValid -and $stale.sceneInventory.status -eq 'STALE_OR_INVALID') 'Stale/wrong-project manifest must be rejected.'

    # Synthetic fixture only: this is not an actual Editor export or a production scene attestation.
    $scenePath = 'Assets/Scenes/Cooking/ShortCycle_P0P1.unity'
    $sceneFile = Join-Path $repositoryRoot $scenePath
    $projectSettings = Get-Content -Raw -LiteralPath (Join-Path $repositoryRoot 'ProjectSettings/ProjectSettings.asset')
    $projectGuid = [regex]::Match($projectSettings, '(?m)^\s*productGUID:\s*([0-9a-fA-F]{32})\s*$').Groups[1].Value
    $sceneGuid = [regex]::Match((Get-Content -Raw -LiteralPath ($sceneFile + '.meta')), '(?m)^guid:\s*([0-9a-fA-F]{32})\s*$').Groups[1].Value
    $areaRows = @(Import-Csv -LiteralPath (Join-Path $repositoryRoot 'DataTables/Cooking/KitchenAreas.csv'))
    $toolRows = @(Import-Csv -LiteralPath (Join-Path $repositoryRoot 'DataTables/Cooking/Tools.csv'))
    $manifest = [ordered]@{
        formatVersion = 1
        projectGuid = $projectGuid
        sceneGuid = $sceneGuid
        scenePath = $scenePath
        sceneSha256 = (Get-FileHash -LiteralPath $sceneFile -Algorithm SHA256).Hash.ToLowerInvariant()
        generatedUtc = 'fixture-only'
        areas = @()
        tools = @()
        issues = @()
    }
    foreach ($area in $areaRows) {
        $objectKey = 'fixture-area-' + $area.id
        $manifest.areas += @{ areaId = $area.id; objectKey = $objectKey; hierarchyPath = 'fixture'; active = $true }
        $toolRow = @($toolRows | Where-Object tool_id -eq $area.core_facility)[0]
        $manifest.tools += @{
            objectKey = 'fixture-tool-' + $area.id
            ownerObjectKey = $objectKey
            hierarchyOwnerObjectKey = $objectKey
            areaId = $area.id
            hierarchyAreaId = $area.id
            toolId = $area.core_facility
            toolKind = $toolRow.tool_kind
            active = $true
            sceneInstance = $true
            mappingSource = [SceneToolInventory]::ExplicitMapping
        }
    }
    [System.IO.File]::WriteAllText($temporary, ($manifest | ConvertTo-Json -Depth 8))
    $validFixture = & $validator -SceneInventoryPath $temporary -NoExit -Quiet
    Assert-True ($validFixture.sceneInventory.status -eq 'VERIFIED' -and $validFixture.isFullyVerified) 'Complete synthetic manifest must exercise verified path.'
    New-Item -ItemType Directory -Path $stagedRoot | Out-Null
    Copy-Item -Path (Join-Path $repositoryRoot 'DataTables/*') -Destination $stagedRoot -Recurse -Force
    $stagedAreas = Join-Path $stagedRoot 'Cooking/KitchenAreas.csv'
    $beforePreset = [System.IO.File]::ReadAllText($stagedAreas)
    $afterPreset = $beforePreset.Replace(
        'tool_board|tool_knife|tool_scissors|tool_bowl',
        'tool_board|tool_pan|tool_pot')
    Assert-True ($afterPreset -cne $beforePreset) 'Preset fixture did not change.'
    [System.IO.File]::WriteAllText($stagedAreas, $afterPreset)
    $changedPreset = & $validator -DataTablesRoot $stagedRoot -SceneInventoryPath $temporary -NoExit -Quiet
    Assert-True ($changedPreset.sceneInventory.status -eq 'VERIFIED') 'Changing only legacy preset values must not invalidate scene mapping.'
    $beforeCandidates = $validFixture.audits.multiCandidateCarriers | ConvertTo-Json -Depth 6 -Compress
    $afterCandidates = $changedPreset.audits.multiCandidateCarriers | ConvertTo-Json -Depth 6 -Compress
    Assert-True ($beforeCandidates -ceq $afterCandidates) 'Legacy preset values must not alter V-04 actual carrier candidates.'
    $partial = [ordered]@{}
    foreach ($key in $manifest.Keys) { $partial[$key] = $manifest[$key] }
    $partial.areas = @($manifest.areas | Select-Object -SkipLast 1)
    $partial.tools = @($manifest.tools | Select-Object -SkipLast 1)
    [System.IO.File]::WriteAllText($temporary, ($partial | ConvertTo-Json -Depth 8))
    $partialReport = & $validator -SceneInventoryPath $temporary -RequireSceneInventory -NoExit -Quiet
    Assert-True ($partialReport.sceneInventory.status -eq 'PARTIAL' -and
        -not $partialReport.isFullyVerified -and @($partialReport.sceneInventory.missingAreaIds).Count -eq 1) 'A scene lacking a locked/future area must be partial, never fully verified.'
    $manifest.sceneSha256 = 'stale'
    [System.IO.File]::WriteAllText($temporary, ($manifest | ConvertTo-Json -Depth 8))
    $staleHash = & $validator -SceneInventoryPath $temporary -NoExit -Quiet
    Assert-True (-not $staleHash.isValid -and $staleHash.sceneInventory.status -eq 'STALE_OR_INVALID') 'Scene source hash drift must reject inventory.'
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    $tempPrefix = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ((Test-Path -LiteralPath $stagedRoot) -and
        [System.IO.Path]::GetFullPath($stagedRoot).StartsWith($tempPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $stagedRoot -Recurse -Force
    }
}

[pscustomobject]@{ success = $true; scenarios = 14; sceneStatus = $withoutManifest.sceneInventory.status } | ConvertTo-Json -Compress
