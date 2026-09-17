[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$testRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $testRoot))
$sourcePath = Join-Path $repoRoot 'Assets\Scripts\MouseInteractive\Drag\DragContainer.cs'
$childRunner = Join-Path $testRoot 'Invoke-CoordinateCompilation.ps1'
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('AI-000086-' + [Guid]::NewGuid().ToString('N'))
$legacyPath = Join-Path $tempRoot 'DragContainer.Legacy.cs'

$currentBlock = @'
                Vector3 targetWorld = InternalToWorld(targetPos);
                Vector3 geoOffsetWorld = GetGeometricCenterOffsetWorld();
                var result = dragLimit.ClampPosition(targetWorld + geoOffsetWorld, GetWorldHalfSize());
                return (WorldToInternal(result.clampedPos - geoOffsetWorld), result.hitBoundary);
'@
$legacyBlock = @'
                Vector3 geoOffsetWorld = GetGeometricCenterOffsetWorld();
                Vector3 geoOffsetInternal = useLocalSpace && transform.parent != null
                    ? transform.parent.InverseTransformVector(geoOffsetWorld)
                    : geoOffsetWorld;
                var result = dragLimit.ClampPosition(targetPos + geoOffsetInternal, GetHalfSize());
                return (result.clampedPos - geoOffsetInternal, result.hitBoundary);
'@

try {
    $source = Get-Content -Raw -Encoding UTF8 -LiteralPath $sourcePath
    if (-not $source.Contains($currentBlock)) { throw 'Current production coordinate-boundary block was not found.' }
    if ([regex]::Matches($source, 'dragLimit\.ClampPosition\s*\(').Count -ne 2) {
        throw 'DragContainer must retain exactly one bounds clamp and one movement clamp.'
    }
    $limitedBody = [regex]::Match($source, '(?s)private\s+\(Vector3 clampedPos, bool hitBoundary\)\s+CalculateLimitedPosition.*?public\s+void\s+onDragStart').Value
    foreach ($required in @('InternalToWorld(targetPos)', 'WorldToInternal(result.clampedPos - geoOffsetWorld)', 'GetWorldHalfSize()')) {
        if (-not $limitedBody.Contains($required)) { throw "Missing production-path contract: $required" }
    }
    if ($limitedBody -match '\b(?:Instantiate|new\s+(?:GameObject|Material|List|Dictionary))\b') {
        throw 'Coordinate limiting introduced a managed/object allocation in the movement path.'
    }

    New-Item -ItemType Directory -Path $tempRoot | Out-Null
    [IO.File]::WriteAllText($legacyPath, $source.Replace($currentBlock, $legacyBlock), [Text.UTF8Encoding]::new($false))

    $legacyBuild = Join-Path $tempRoot 'legacy-build'
    $legacyOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File $childRunner -DragContainerPath $legacyPath -Label LEGACY -BuildDirectory $legacyBuild 2>&1
    $legacyExit = $LASTEXITCODE
    if ($legacyExit -eq 0 -or (($legacyOutput | Out-String) -notmatch 'REGRESSION_SYNC_WORLD_TARGET')) {
        throw "Legacy source did not fail the intended world-target regression.`n$($legacyOutput | Out-String)"
    }
    Write-Output 'LEGACY_EXPECTED_FAIL marker=REGRESSION_SYNC_WORLD_TARGET'

    $currentBuild = Join-Path $tempRoot 'current-build'
    $currentOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File $childRunner -DragContainerPath $sourcePath -Label CURRENT -BuildDirectory $currentBuild 2>&1
    $currentExit = $LASTEXITCODE
    $currentText = $currentOutput | Out-String
    if ($currentExit -ne 0 -or $currentText -notmatch 'COORDINATE_SPACE_REGRESSION_PASS') {
        throw "Current production source failed coordinate regression.`n$currentText"
    }
    $currentOutput
    Write-Output 'SOURCE_CONTRACT_PASS clamps=2 allocationSentinel=pass'
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        $resolvedTemp = [IO.Path]::GetFullPath($tempRoot)
        $resolvedSystemTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if (-not $resolvedTemp.StartsWith($resolvedSystemTemp, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove non-temp path: $resolvedTemp"
        }
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
