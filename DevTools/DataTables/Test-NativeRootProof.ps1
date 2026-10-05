[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
. (Join-Path $PSScriptRoot 'DataPipeline.Common.ps1')
$sources = @(
    'DevTools/DataTables/StaticCompile/UnityStubs.cs',
    'Assets/Editor/DataTables/DataTableValidationCore.cs',
    'Assets/Editor/DataTables/DataTableImportContracts.cs',
    'Assets/Scripts/DataBase/CookingData/CookingEnums.cs',
    'Assets/Scripts/DataBase/CookingData/CK01DataModels.cs',
    'Assets/Scripts/DataBase/CookingData/CK01RecipeSlotData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01RecipeStepData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01ItemData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01InitialInventoryData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01TagData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01ToolData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01CookingGameConfigData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01FridgeCapacityLevelData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01ProcessActionData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01ProcessingRecordData.cs',
    'Assets/Scripts/DataBase/CookingData/CK01RecipeVariantData.cs',
    'Assets/Scripts/DataBase/CookingData/KitchenAreaData.cs',
    'Assets/Scripts/DataBase/Localization/LocTextCatalog.cs',
    'Assets/Scripts/DataBase/Localization/LocTableAsset.cs',
    'Assets/Scripts/DataBase/CookingData/CK01GeneratedDataCatalog.cs',
    'Assets/Editor/DataTables/DataTableImportService.cs'
) | ForEach-Object { Join-Path $root $_ }
if (-not ('EatWhat.DataTables.DataTableImportService' -as [type])) {
    Add-Type -Path $sources -ErrorAction Stop
}
$service = [EatWhat.DataTables.DataTableImportService]
$build = $service.GetMethod('BuildBatchDependencyProof', [Reflection.BindingFlags]'NonPublic,Static')
if ($null -eq $build) { throw 'Production BuildBatchDependencyProof is unavailable.' }
$db = [UnityEditor.AssetDatabase]
$sharedPath = [EatWhat.DataTables.CK01ImportRegistry]::MissingSpriteTextureAssetPath
$catalogPath = [EatWhat.DataTables.CK01ImportRegistry]::DataCatalogAssetPath
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('EatWhatNativeProof-' + [guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($testRoot)
$oldDataPath = [UnityEngine.Application]::ProofDataPath
$oldSource = [Environment]::GetEnvironmentVariable('EATWHAT_DATA_SOURCE_TREE_SHA256')
$oldInput = [Environment]::GetEnvironmentVariable('EATWHAT_DATA_INPUT_SNAPSHOT_SHA256')
[Environment]::SetEnvironmentVariable('EATWHAT_DATA_SOURCE_TREE_SHA256', 'source')
[Environment]::SetEnvironmentVariable('EATWHAT_DATA_INPUT_SNAPSHOT_SHA256', 'input')
$checks = [Collections.Generic.List[string]]::new()
function Check([bool]$condition, [string]$name) {
    if (-not $condition) { throw "FAILED: $name" }
    $checks.Add($name)
}
function MustReject([scriptblock]$action, [string]$name) {
    try { & $action | Out-Null; throw "Unexpected acceptance: $name" }
    catch {
        if ($_.Exception.Message -like 'Unexpected acceptance:*') { throw }
        $checks.Add($name)
    }
}
function Add-FixtureAsset([string]$path, [UnityEngine.Object]$main, [UnityEngine.Object[]]$all, [string]$guid) {
    $project = Split-Path -Parent ([UnityEngine.Application]::ProofDataPath)
    $full = Join-Path $project $path
    [void][IO.Directory]::CreateDirectory((Split-Path -Parent $full))
    [IO.File]::WriteAllText($full, "fixture: $path", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText(($full + '.meta'), "guid: $guid`n", [Text.UTF8Encoding]::new($false))
    $db::ProofMain[$path] = $main
    $db::ProofAll[$path] = $all
    $db::ProofGuids[$path] = $guid
    for ($i = 0; $i -lt $all.Length; $i++) {
        $db::ProofObjectPaths[$all[$i]] = $path
        $db::ProofFileIds[$all[$i]] = [long](11400000 + $i)
    }
}
function New-Fixture {
    $db::ResetProofFixture()
    $project = Join-Path $testRoot ([guid]::NewGuid().ToString('N'))
    $generated = Join-Path $project 'Assets/Generated/DataTables'
    [void][IO.Directory]::CreateDirectory($generated)
    [UnityEngine.Application]::ProofDataPath = Join-Path $project 'Assets'
    $catalog = [CK01GeneratedDataCatalog]::new()
    $catalog.name = 'catalog'
    Add-FixtureAsset $catalogPath $catalog @($catalog) '1234567890abcdef1234567890abcdef'
    $texture = [UnityEngine.Texture2D]::new(1,1,[UnityEngine.TextureFormat]::RGBA32,$false)
    $texture.name = 'CK01-MissingSpriteTexture'
    $sprite = [UnityEngine.Sprite]::Create($texture,[UnityEngine.Rect]::new(0,0,1,1),[UnityEngine.Vector2]::new(0,0),100)
    $sprite.name = 'CK01-MissingSprite'
    Add-FixtureAsset $sharedPath $texture @($texture,$sprite) 'fedcba0987654321fedcba0987654321'
    $db::ProofDependencies = @($catalogPath,$sharedPath)
    return [pscustomobject]@{ project=$project; generated=$generated; catalog=$catalog; texture=$texture; sprite=$sprite }
}
function Build-Proof {
    return $build.Invoke($null, @('run','nonce'))
}
function Verify-Proof($fixture, $proof) {
    $credential = [pscustomobject]@{ dependencyProof=$proof }
    return Confirm-DataBatchDependencyProof -Credential $credential -ExpectedRunId 'run' -ExpectedNonce 'nonce' -ExpectedSourceTreeSha256 'source' -ExpectedInputSnapshotSha256 'input' -GeneratedRoot $fixture.generated -ProjectRoot $fixture.project
}
try {
    $f = New-Fixture
    $proof = Build-Proof
    $verified = Verify-Proof $f $proof
    Check ($proof.schemaVersion -eq 2 -and $verified.verified -and $verified.rootCount -eq 2) 'production core accepts catalog plus exact shared texture/Sprite'
    $shared = @($proof.roots | Where-Object path -eq $sharedPath)[0]
    Check ($shared.typeName -ceq 'UnityEngine.Texture2D' -and $shared.subAssets.Length -eq 1 -and
        $shared.subAssets[0].textureGuid -ceq $shared.guid -and
        $shared.subAssets[0].textureLocalFileId -eq $shared.localFileId) 'main/subasset and texture ownership are recorded'
    $actualSpriteId = [long]::Parse('-6724980738021171820', [Globalization.CultureInfo]::InvariantCulture)
    $db::ProofFileIds[$f.texture] = [long]2800000
    $db::ProofFileIds[$f.sprite] = $actualSpriteId
    $signedProof = Build-Proof
    $signedJson = $signedProof | ConvertTo-Json -Depth 20 -Compress
    $roundTrip = $signedJson | ConvertFrom-Json -Depth 20
    $signedShared = @($roundTrip.roots | Where-Object path -eq $sharedPath)[0]
    Check ($signedShared.localFileId -is [long] -and $signedShared.localFileId -eq 2800000 -and
        $signedShared.subAssets[0].localFileId -is [long] -and
        $signedShared.subAssets[0].localFileId -eq $actualSpriteId -and
        $signedShared.subAssets[0].textureLocalFileId -eq 2800000 -and
        $signedJson.Contains('-6724980738021171820')) 'actual negative Sprite Int64 survives production proof and JSON round trip'
    Check ((Verify-Proof $f $roundTrip).verified) 'production verifier accepts JSON-round-tripped negative Sprite identity'
    $signedShared.subAssets[0].localFileId = 0
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects zero Sprite localFileId'
    $signedShared.subAssets[0].localFileId = $actualSpriteId
    $signedShared.localFileId = 0
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects zero main localFileId'
    $signedShared.localFileId = [long]2800000
    $signedShared.subAssets[0].PSObject.Properties.Remove('localFileId')
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects missing Sprite localFileId'
    $roundTrip = $signedJson | ConvertFrom-Json -Depth 20
    $signedShared = @($roundTrip.roots | Where-Object path -eq $sharedPath)[0]
    $signedShared.subAssets[0].localFileId = '9223372036854775808'
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects overflowing Sprite localFileId'
    $signedShared.subAssets[0].localFileId = 1.5
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects fractional Sprite localFileId'
    $signedShared.subAssets[0].localFileId = [string]$actualSpriteId
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects string-typed Sprite localFileId'
    $signedShared.subAssets[0].localFileId = [long]2800000
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects duplicate main/subasset identity'
    $roundTrip = $signedJson | ConvertFrom-Json -Depth 20
    $signedShared = @($roundTrip.roots | Where-Object path -eq $sharedPath)[0]
    $signedShared.subAssets[0].textureLocalFileId = $actualSpriteId
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects Sprite texture identity mismatch'
    $signedShared.subAssets[0].textureLocalFileId = '2800000'
    MustReject { Verify-Proof $f $roundTrip } 'verifier rejects string-typed texture localFileId'
    $db::ProofFileIds[$f.texture] = [long]-2800000
    $negativeMainProof = Build-Proof
    $negativeMainRoundTrip = ($negativeMainProof | ConvertTo-Json -Depth 20 -Compress) | ConvertFrom-Json -Depth 20
    $negativeMain = @($negativeMainRoundTrip.roots | Where-Object path -eq $sharedPath)[0]
    Check ($negativeMain.localFileId -eq -2800000 -and
        $negativeMain.subAssets[0].textureLocalFileId -eq -2800000 -and
        (Verify-Proof $f $negativeMainRoundTrip).verified) 'negative main and texture owner Int64 survive production proof and JSON round trip'
    $db::ProofFileIds[$f.texture] = [long]2800000
    $db::ProofFileIds[$f.sprite] = [long]0
    MustReject { Build-Proof } 'production core rejects zero Sprite localFileId'
    $db::ProofFileIds[$f.sprite] = [long]2800000
    MustReject { Build-Proof } 'production core rejects duplicate main/subasset identity'
    $db::ProofFileIds[$f.sprite] = $actualSpriteId
    $db::ProofFileIds[$f.texture] = [long]0
    MustReject { Build-Proof } 'production core rejects zero main localFileId'
    $db::ProofFileIds[$f.texture] = [long]2800000
    $db::ProofAll[$sharedPath] = @($f.sprite,$f.texture)
    Check ((Build-Proof).roots.Length -eq 2) 'Unity subasset enumeration order does not change identity proof'
    $db::ProofAll[$sharedPath] = @($f.texture,$f.sprite)
    $shared.subAssets[0].textureGuid = '0' * 32
    MustReject { Verify-Proof $f $proof } 'verifier rejects forged Sprite texture ownership'
    $shared.subAssets[0].textureGuid = $shared.guid
    $proof.roots = @($shared)
    MustReject { Verify-Proof $f $proof } 'verifier rejects omitted catalog root'
    $proof = Build-Proof
    $proof.dependencies = @($proof.dependencies | Where-Object path -ne $sharedPath)
    MustReject { Verify-Proof $f $proof } 'verifier rejects omitted dependency'
    $proof = Build-Proof
    [IO.File]::AppendAllText((Join-Path $f.project $sharedPath), 'drift')
    MustReject { Verify-Proof $f $proof } 'candidate byte drift blocks'

    $f = New-Fixture
    $externalPath = 'Assets/Other/External.asset'
    $external = [UnityEngine.ScriptableObject]::new()
    Add-FixtureAsset $externalPath $external @($external) 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
    $db::ProofDependencies = @($catalogPath,$sharedPath,$externalPath)
    $proof = Build-Proof
    Check ($proof.dependencies.Length -eq 3 -and (Verify-Proof $f $proof).dependencyCount -eq 3) 'recursive closure retains an external dependency'
    $db::ProofDependencies = @($catalogPath,$sharedPath,'Assets/Scripts/Spine_Package/Avatars/output4/O4.asset')
    MustReject { Build-Proof } 'approved old Spine asset still forbidden in generated dependency closure'
    $f = New-Fixture
    $externalPath = 'Assets/Other/Animation.asset'
    $external = [Spine.Unity.DerivedAnimationReferenceAsset]::new()
    Add-FixtureAsset $externalPath $external @($external) 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
    $db::ProofDependencies = @($catalogPath,$sharedPath,$externalPath)
    MustReject { Build-Proof } 'derived animation dependency blocks'

    $f = New-Fixture
    $db::ProofDependencies = @($catalogPath)
    MustReject { Build-Proof } 'production core rejects closure missing shared root'
    $f = New-Fixture
    $db::ProofGuids.Remove($sharedPath) | Out-Null
    MustReject { Build-Proof } 'missing GUID blocks'
    $f = New-Fixture
    [IO.File]::Delete((Join-Path $f.project ($sharedPath + '.meta')))
    MustReject { Build-Proof } 'missing meta blocks'
    $f = New-Fixture
    $db::ProofBroken.Add($f.sprite) | Out-Null
    MustReject { Build-Proof } 'broken Sprite reference blocks'
    $f = New-Fixture
    $db::ProofAll[$sharedPath] = @($f.texture)
    MustReject { Build-Proof } 'missing Sprite blocks'
    $f = New-Fixture
    $f.sprite.texture = [UnityEngine.Texture2D]::new(1,1,[UnityEngine.TextureFormat]::RGBA32,$false)
    MustReject { Build-Proof } 'Sprite pointing to different texture blocks'
    $f = New-Fixture
    $unknown = [UnityEngine.Object]::new()
    $db::ProofAll[$sharedPath] = @($f.texture,$f.sprite,$unknown)
    MustReject { Build-Proof } 'unknown third subasset blocks'
    $f = New-Fixture
    $db::ProofAll[$sharedPath] = @($f.texture,[Spine.Unity.DerivedAnimationReferenceAsset]::new())
    MustReject { Build-Proof } 'derived animation subasset blocks'
    $f = New-Fixture
    $db::ProofTypes[$sharedPath] = [CK01GeneratedDataCatalog]
    MustReject { Build-Proof } 'wrong registered main type blocks'
    $f = New-Fixture
    $extra = 'Assets/Generated/DataTables/Other/Unknown.asset'
    $other = [UnityEngine.Texture2D]::new(1,1,[UnityEngine.TextureFormat]::RGBA32,$false)
    Add-FixtureAsset $extra $other @($other) 'abcdefabcdefabcdefabcdefabcdefab'
    $db::ProofDependencies = @($catalogPath,$sharedPath,$extra)
    MustReject { Build-Proof } 'nonregistered Texture2D root blocks'
    $f = New-Fixture
    $db::ProofFileIds.Remove($f.sprite) | Out-Null
    MustReject { Build-Proof } 'missing Sprite local file identity blocks'
    [pscustomobject]@{ result='PASS'; checkCount=$checks.Count; checks=@($checks); productionMethod='DataTableImportService.BuildBatchDependencyProof'; unityStarted=$false; productionWritten=$false } | ConvertTo-Json -Depth 5
}
finally {
    [UnityEngine.Application]::ProofDataPath = $oldDataPath
    [Environment]::SetEnvironmentVariable('EATWHAT_DATA_SOURCE_TREE_SHA256', $oldSource)
    [Environment]::SetEnvironmentVariable('EATWHAT_DATA_INPUT_SNAPSHOT_SHA256', $oldInput)
    $db::ResetProofFixture()
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $safeRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if ($resolved.StartsWith($safeRoot, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolved)) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
