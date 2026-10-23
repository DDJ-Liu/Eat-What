$ErrorActionPreference='Stop'
$catalogRepository=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
function Get-CatalogCompileInputs {
    $dataRoot=Join-Path $catalogRepository 'Assets/Scripts/DataBase/CookingData'
    $runtime=@(Get-ChildItem $dataRoot -Filter 'CK01*.cs' -File|ForEach-Object FullName)+@('CookingEnums.cs','KitchenAreaData.cs','RecipeView.cs','Phase2DataViews.cs','ICatalogSource.cs','ScriptableObjectSource.cs','EncryptedBlobSource.cs','CatalogBlobSerializer.cs','Build/CatalogBuildFingerprint.g.cs'|ForEach-Object{Join-Path $dataRoot $_})+@('LocTableAsset.cs','LocTextCatalog.cs'|ForEach-Object{Join-Path $catalogRepository "Assets/Scripts/DataBase/Localization/$_"})
    $security=@(Get-ChildItem (Join-Path $catalogRepository 'Assets/Scripts/Tools/Security') -Filter '*.cs' -File|ForEach-Object FullName)+@(Join-Path $catalogRepository 'Assets/Scripts/Tools/Persistence/SaveEnvelope.cs')
    $jsonRoots=@(Get-ChildItem (Join-Path $catalogRepository 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.newtonsoft-json*')
    if($jsonRoots.Count -ne 1){throw 'Expected installed Newtonsoft package.'}
    @{runtime=$runtime;security=$security;json=(Join-Path $jsonRoots[0].FullName 'Runtime/Newtonsoft.Json.dll');shared=@('CatalogBuildPlan.cs','CatalogBlobStaging.cs'|ForEach-Object{Join-Path $catalogRepository "Assets/Editor/Build/$_"})}
}
function Initialize-CatalogHost {
    param([switch]$IncludeTests)
    $inputs=Get-CatalogCompileInputs
    Add-Type -Path $inputs.json
    $refs=@(Get-ChildItem (Join-Path $PSHOME 'ref') -File -Filter '*.dll'|ForEach-Object FullName)+@($inputs.json)
    $sources=@(Join-Path $PSScriptRoot 'StaticCompile/UnityStubs.cs')+$inputs.security+$inputs.runtime+$inputs.shared
    if($IncludeTests){$sources+=@(Join-Path $PSScriptRoot 'Fixtures/CatalogBlobTests.cs')}
    Add-Type -Path $sources -ReferencedAssemblies $refs -IgnoreWarnings
    return $inputs
}
function Invoke-CatalogNativeReferenceCompile {
    param([Parameter(Mandatory)][string]$Output,[switch]$OwnershipOnly)
    $inputs=Get-CatalogCompileInputs
    $version=[regex]::Match((Get-Content (Join-Path $catalogRepository 'ProjectSettings/ProjectVersion.txt') -Raw),'m_EditorVersion:\s*([^\r\n]+)').Groups[1].Value.Trim()
    $data="C:/Program Files/Unity/Hub/Editor/$version/Editor/Data"
    $mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe';$csc=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
    $baseRefs=@("$data/NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll","$data/NetStandard/ref/2.1.0/netstandard.dll",$inputs.json)
    $engine=@(Get-ChildItem "$data/Managed/UnityEngine" -Filter 'UnityEngine*.dll' -File|ForEach-Object FullName)
    $editor=@(Get-ChildItem "$data/Managed/UnityEngine" -Filter 'UnityEditor*.dll' -File|ForEach-Object FullName)+@("$data/Managed/UnityEditor.dll")
    New-Item -ItemType Directory -Path $Output -Force|Out-Null
    function Compile-Catalog([string]$Name,[string[]]$Sources,[string[]]$References,[string]$Defines,[string]$AssemblyKind="library"){
        $target=Join-Path $Output ($Name+'.dll')
        $compileArguments=@('/nologo','/noconfig','/nostdlib+',"/target:$AssemblyKind","/out:$target")+@($References|Sort-Object -Unique|ForEach-Object{"/reference:$_"})
        if($Defines){$compileArguments+=@("/define:$Defines")}
        $lines=@(& $mono $csc @compileArguments @Sources 2>&1);$exit=$LASTEXITCODE
        [IO.File]::WriteAllLines((Join-Path $Output ($Name+'.compile.log')),[string[]]$lines)
        if($exit -ne 0){throw "Catalog Unity API reference compile failed: $Name, see $Output"};return $target
    }
    $toolkit=Compile-Catalog 'Toolkit.CatalogSubset' $inputs.security $baseRefs ''
    if($OwnershipOnly){
        $authoring=Compile-Catalog 'Catalog.Editor' $inputs.runtime ($baseRefs+$engine+@($toolkit)) 'UNITY_EDITOR;CK01_ENCRYPTED_DATA'
        $build=Compile-Catalog 'Catalog.BuildEditor' ($inputs.shared+@(Join-Path $catalogRepository 'Assets/Editor/Build/CatalogBlobBuildProcessor.cs')) ($baseRefs+$engine+$editor+@($toolkit,$authoring)) 'UNITY_EDITOR'
        return @{version=$version;compiled=@($toolkit,$authoring,$build);executed=$false;scope='Affected shared guard + Editor API surface with required dependencies only; no native Unity or Catalog234/Security execution.'}
    }
    $player=Compile-Catalog 'Catalog.Release' $inputs.runtime ($baseRefs+$engine+@($toolkit)) 'CK01_ENCRYPTED_DATA'
    $dev=Compile-Catalog 'Catalog.Development' $inputs.runtime ($baseRefs+$engine+@($toolkit)) 'CK01_ENCRYPTED_DATA;DEVELOPMENT_BUILD'
    $authoring=Compile-Catalog 'Catalog.Editor' $inputs.runtime ($baseRefs+$engine+@($toolkit)) 'UNITY_EDITOR;CK01_ENCRYPTED_DATA'
    $build=Compile-Catalog 'Catalog.BuildEditor' ($inputs.shared+@(Join-Path $catalogRepository 'Assets/Editor/Build/CatalogBlobBuildProcessor.cs')) ($baseRefs+$engine+$editor+@($toolkit,$authoring)) 'UNITY_EDITOR'
    $behavior=Compile-Catalog 'Catalog.Behavior.Mono' (@(Join-Path $PSScriptRoot 'StaticCompile/UnityStubs.cs')+$inputs.security+$inputs.runtime+$inputs.shared+@(Join-Path $PSScriptRoot 'Fixtures/CatalogBlobTests.cs')) $baseRefs '' 'exe'
    @{mono=$mono;monoPath=(Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/Facades');json=$inputs.json;behavior=$behavior;version=$version;compiled=@($toolkit,$player,$dev,$authoring,$build);executed=$false;scope='Real Unity references, source-only surface compilation; no Editor/Player/native invocation.'}
}
function Read-CatalogNativeResult {
    param([AllowEmptyCollection()][object[]]$Lines)
    $prefix='CATALOG_RESULT='
    $payloads=@(foreach($entry in $Lines){
        $value=[string]$entry
        # Native stdout can carry one transport BOM. Never trim the JSON or use culture matching.
        if($value.Length -gt 0 -and $value[0] -eq [char]0xFEFF){$value=$value.Substring(1)}
        if($value.StartsWith($prefix,[StringComparison]::Ordinal)){$value.Substring($prefix.Length)}
    })
    if($payloads.Count -ne 1){throw 'Expected exactly one Catalog native result marker.'}
    if([string]::IsNullOrWhiteSpace($payloads[0])){throw 'Empty Catalog native result.'}
    $parsed=[Newtonsoft.Json.Linq.JObject]::Parse($payloads[0])
    foreach($name in @('success','hostPacketAccepted')){
        $flag=$parsed[$name]
        if($null -eq $flag -or $flag.Type -ne [Newtonsoft.Json.Linq.JTokenType]::Boolean -or $flag.Value -cne $true){throw "Catalog native result requires true boolean $name."}
    }
    $count=$parsed['assertionCount'];$fingerprint=$parsed['fingerprint']
    if($null -eq $count -or $count.Type -ne [Newtonsoft.Json.Linq.JTokenType]::Integer -or [long]$count -le 0 -or [long]$count -gt [int]::MaxValue){throw 'Invalid Catalog assertionCount.'}
    if($null -eq $fingerprint -or $fingerprint.Type -ne [Newtonsoft.Json.Linq.JTokenType]::String -or [string]$fingerprint -cnotmatch '^[0-9a-f]{64}$'){throw 'Invalid Catalog fingerprint.'}
    return ,$parsed
}
function Test-CatalogNativeResultProtocol {
    $prefix='CATALOG_RESULT='
    $valid='{"success":true,"assertionCount":234,"fingerprint":"ce27e9286b49c4cd345e01d9a5dd94ddf27869526d10a904e12b35b6b60ba265","hostPacketAccepted":true}'
    $plain=$prefix+$valid
    $bomBytes=[byte[]](@(0xEF,0xBB,0xBF)+[Text.Encoding]::UTF8.GetBytes($plain))
    $bom=[Text.Encoding]::UTF8.GetString($bomBytes)
    $names=[Collections.Generic.List[string]]::new()
    foreach($good in @(@{name='plain Ordinal marker';lines=@($plain)},@{name='native UTF8 BOM bytes';lines=@($bom)},@{name='other stdout lines';lines=@('compiler notice',$plain,'done')})){
        $parsed=Read-CatalogNativeResult -Lines $good.lines
        if([int]$parsed['assertionCount'] -ne 234){throw 'Catalog protocol positive case changed payload.'}
        $names.Add($good.name)
    }
    $bad=@(
        @{name='missing marker';lines=@('no result')},
        @{name='duplicate marker';lines=@($plain,$plain)},
        @{name='duplicate BOM/plain markers';lines=@($bom,$plain)},
        @{name='only one transport BOM';lines=@(([string][char]0xFEFF)+$bom)},
        @{name='Ordinal case required';lines=@('catalog_result='+$valid)},
        @{name='no leading whitespace trim';lines=@(' '+$plain)},
        @{name='no BOM inside payload';lines=@($prefix+([char]0xFEFF)+$valid)},
        @{name='empty JSON';lines=@($prefix)},
        @{name='malformed JSON';lines=@($prefix+'{')},
        @{name='object JSON required';lines=@($prefix+'[]')},
        @{name='false success';lines=@($prefix+$valid.Replace('"success":true','"success":false'))},
        @{name='string success';lines=@($prefix+$valid.Replace('"success":true','"success":"true"'))},
        @{name='missing success';lines=@($prefix+$valid.Replace('"success":true,',''))},
        @{name='false host acceptance';lines=@($prefix+$valid.Replace('"hostPacketAccepted":true','"hostPacketAccepted":false'))},
        @{name='missing host acceptance';lines=@($prefix+$valid.Replace(',"hostPacketAccepted":true',''))},
        @{name='zero assertions';lines=@($prefix+$valid.Replace('"assertionCount":234','"assertionCount":0'))},
        @{name='string assertions';lines=@($prefix+$valid.Replace('"assertionCount":234','"assertionCount":"234"'))},
        @{name='missing fingerprint';lines=@($prefix+'{"success":true,"hostPacketAccepted":true,"assertionCount":1}')},
        @{name='invalid fingerprint';lines=@($prefix+$valid.Replace('ce27e9286b49c4cd345e01d9a5dd94ddf27869526d10a904e12b35b6b60ba265','bad'))}
    )
    foreach($case in $bad){
        $rejected=$false
        try{[void](Read-CatalogNativeResult -Lines $case.lines)}catch{$rejected=$true}
        if(-not $rejected){throw "Catalog protocol accepted invalid case: $($case.name)"}
        $names.Add($case.name)
    }
    return @{success=$true;assertionCount=$names.Count;cases=@($names)}
}
function ConvertTo-CatalogJsonToken {
    param([Parameter(Mandatory)][object]$Value)
    # ConvertTo-Json unwraps PowerShell string adapters; depth truncation is an error.
    $json=ConvertTo-Json -InputObject $Value -Depth 12 -Compress -WarningAction Stop
    return ,([Newtonsoft.Json.Linq.JToken]::Parse($json))
}
function Write-CatalogResult {
    param([Parameter(Mandatory)][Newtonsoft.Json.Linq.JObject]$Result,
        [Parameter(Mandatory)][hashtable]$Mono,[Parameter(Mandatory)][hashtable]$Protocol,
        [Parameter(Mandatory)][hashtable]$Native,[Parameter(Mandatory)][string]$Path)
    $nativeToken=ConvertTo-CatalogJsonToken -Value $Native
    foreach($name in @('mono','monoPath','json','behavior','version','scope')){
        if($null -eq $nativeToken[$name] -or $nativeToken[$name].Type -ne [Newtonsoft.Json.Linq.JTokenType]::String -or [string]::IsNullOrWhiteSpace([string]$nativeToken[$name])){throw "Missing/invalid native metadata $name."}
    }
    if($null -eq $nativeToken['compiled'] -or $nativeToken['compiled'].Type -ne [Newtonsoft.Json.Linq.JTokenType]::Array -or $nativeToken['compiled'].Count -eq 0){throw 'Native compiled metadata must be a nonempty array.'}
    foreach($item in $nativeToken['compiled']){if($item.Type -ne [Newtonsoft.Json.Linq.JTokenType]::String -or [string]::IsNullOrWhiteSpace([string]$item)){throw 'Invalid native compiled path.'}}
    if($null -eq $nativeToken['executed'] -or $nativeToken['executed'].Type -ne [Newtonsoft.Json.Linq.JTokenType]::Boolean){throw 'Native executed metadata must be boolean.'}
    $Result['mono']=ConvertTo-CatalogJsonToken -Value $Mono
    $Result['nativeResultProtocol']=ConvertTo-CatalogJsonToken -Value $Protocol
    $Result['unityReferenceCompilation']=$nativeToken
    $bytes=[Text.UTF8Encoding]::new($false).GetBytes($Result.ToString())
    $stream=[IO.FileStream]::new($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write)
    try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
    $readback=[Newtonsoft.Json.Linq.JObject]::Parse([IO.File]::ReadAllText($Path))
    if(-not [Newtonsoft.Json.Linq.JToken]::DeepEquals($Result,$readback)){throw 'Complete Catalog result changed type/value during file roundtrip.'}
    return ,$readback
}
function Test-CatalogResultSerialization {
    param([Parameter(Mandatory)][string]$ReportRoot)
    [void][IO.Directory]::CreateDirectory($ReportRoot)
    $names=[Collections.Generic.List[string]]::new()
    function Assert-CatalogMetadata([bool]$Condition,[string]$Name){
        if(-not $Condition){throw "Catalog metadata roundtrip failed: $Name"}
        $names.Add($Name)
    }
    $wrappedRoot=Join-Path ([IO.Path]::GetFullPath($ReportRoot)) 'Unity 路径 with spaces'
    $native=@{mono=(Join-Path $wrappedRoot 'mono.exe');monoPath=(Join-Path $wrappedRoot 'Facades');json=(Join-Path $wrappedRoot 'Newtonsoft.Json.dll');behavior=(Join-Path $wrappedRoot 'Catalog.Behavior.Mono.dll');version='6000.3.24f1';compiled=@('Toolkit','Release','Development','Editor','BuildEditor'|ForEach-Object {Join-Path $wrappedRoot ($_+'.dll')});executed=$false;scope='API reference compilation only; no native Unity invocation.'}
    $mono=@{exitCode=0;assertions=234;fingerprintMatched=$true;hostPacketAccepted=$true;monoPacketAcceptedByHost=$true}
    $protocol=Test-CatalogNativeResultProtocol

    $hostResult=[Newtonsoft.Json.Linq.JObject]::Parse('{"success":true,"assertionCount":234,"actualDataSOs":169,"excludedSOs":170,"tables":14,"spriteKeys":13,"sceneReferenceChecks":4,"fingerprint":"ce27e9286b49c4cd345e01d9a5dd94ddf27869526d10a904e12b35b6b60ba265","packetBytes":79993,"cases":["complete result fixture"],"nativeBuildVerified":false,"realDataWrites":0,"staging":"isolated staging","metadataSerialization":{"success":true}}')
    $before=$hostResult.DeepClone()
    $path=Join-Path $ReportRoot 'complete-result.json'
    $read=Write-CatalogResult -Result $hostResult -Mono $mono -Protocol $protocol -Native $native -Path $path
    foreach($field in $before.Properties()){Assert-CatalogMetadata ([Newtonsoft.Json.Linq.JToken]::DeepEquals($field.Value,$read[$field.Name])) ("host field "+$field.Name)}
    Assert-CatalogMetadata (@($read.Properties()).Count -eq @($before.Properties()).Count+3) 'complete top-level field set'
    foreach($name in @('mono','monoPath','json','behavior','version','scope')){
        $value=$read['unityReferenceCompilation'][$name]
        Assert-CatalogMetadata ($value.Type -eq [Newtonsoft.Json.Linq.JTokenType]::String -and [string]$value -ceq [string]$native[$name]) ("native string "+$name)
    }
    $compiled=$read['unityReferenceCompilation']['compiled']
    Assert-CatalogMetadata ($compiled.Type -eq [Newtonsoft.Json.Linq.JTokenType]::Array -and $compiled.Count -eq $native.compiled.Count) 'compiled array type/count'
    for($i=0;$i -lt $native.compiled.Count;$i++){Assert-CatalogMetadata ($compiled[$i].Type -eq [Newtonsoft.Json.Linq.JTokenType]::String -and [string]$compiled[$i] -ceq [string]$native.compiled[$i]) ("compiled path "+$i)}
    $executed=$read['unityReferenceCompilation']['executed']
    Assert-CatalogMetadata ($executed.Type -eq [Newtonsoft.Json.Linq.JTokenType]::Boolean -and $executed.Value -ceq $false) 'false executed stays boolean false'
    Assert-CatalogMetadata (@($read['unityReferenceCompilation'].Properties()).Count -eq $native.Count) 'all native fields retained'
    foreach($name in @('fingerprintMatched','hostPacketAccepted','monoPacketAcceptedByHost')){
        $value=$read['mono'][$name]
        Assert-CatalogMetadata ($value.Type -eq [Newtonsoft.Json.Linq.JTokenType]::Boolean -and $value.Value -ceq $true) ("mono boolean "+$name)
    }
    foreach($name in @('exitCode','assertions')){Assert-CatalogMetadata ($read['mono'][$name].Type -eq [Newtonsoft.Json.Linq.JTokenType]::Integer -and [int]$read['mono'][$name] -eq $mono[$name]) ("mono integer "+$name)}
    Assert-CatalogMetadata (@($read['mono'].Properties()).Count -eq $mono.Count) 'all mono fields retained'
    Assert-CatalogMetadata ($read['nativeResultProtocol']['success'].Type -eq [Newtonsoft.Json.Linq.JTokenType]::Boolean -and $read['nativeResultProtocol']['success'].Value -ceq $true) 'protocol boolean'
    Assert-CatalogMetadata ($read['nativeResultProtocol']['assertionCount'].Type -eq [Newtonsoft.Json.Linq.JTokenType]::Integer -and [int]$read['nativeResultProtocol']['assertionCount'] -eq 22) 'protocol assertions'
    $cases=$read['nativeResultProtocol']['cases']
    Assert-CatalogMetadata ($cases.Type -eq [Newtonsoft.Json.Linq.JTokenType]::Array -and $cases.Count -eq $protocol.cases.Count) 'protocol case array'
    for($i=0;$i -lt $protocol.cases.Count;$i++){Assert-CatalogMetadata ($cases[$i].Type -eq [Newtonsoft.Json.Linq.JTokenType]::String -and [string]$cases[$i] -ceq $protocol.cases[$i]) ("protocol case "+$i)}
    foreach($invalid in @('missing monoPath','string compiled','string false')){
        $bad=$native.Clone()
        if($invalid -eq 'missing monoPath'){$bad.Remove('monoPath')}
        if($invalid -eq 'string compiled'){$bad.compiled='not an array'}
        if($invalid -eq 'string false'){$bad.executed='false'}
        $badPath=Join-Path $ReportRoot ($invalid.Replace(' ','-')+'.json');$rejected=$false
        try{[void](Write-CatalogResult -Result ([Newtonsoft.Json.Linq.JObject]::new()) -Mono $mono -Protocol $protocol -Native $bad -Path $badPath)}catch{$rejected=$true}
        Assert-CatalogMetadata ($rejected -and -not [IO.File]::Exists($badPath)) ("reject before write "+$invalid)
    }
    return @{success=$true;assertionCount=$names.Count;cases=@($names);completeResultPath=$path;protocolAssertions=$protocol.assertionCount}
}
function Invoke-CatalogOwnershipTests {
    param([Parameter(Mandatory)][string]$ReportRoot)
    $root=[IO.Path]::GetFullPath($ReportRoot);[void][IO.Directory]::CreateDirectory($root)
    $inputs=Initialize-CatalogHost -IncludeTests
    $redirect=[Action[string,string]]{
        param($link,$target)
        if(-not $link.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or -not $target.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Synthetic redirect escaped output root.'}
        New-Item -ItemType Junction -Path $link -Target $target -ErrorAction Stop|Out-Null
    }
    $result=[CatalogBlobTests]::RunOwnership((Join-Path $root 'fixtures'),$redirect)
    $native=Invoke-CatalogNativeReferenceCompile -Output (Join-Path $root 'unity-reference') -OwnershipOnly
    $source=[IO.File]::ReadAllText((Join-Path $catalogRepository 'Assets/Editor/Build/CatalogBlobBuildProcessor.cs'))
    if(-not $source.Contains('CatalogBuildPlan.PlanOwnedCleanup(') -or -not $source.Contains('Cleanup(ctx);') -or -not $source.Contains('CatalogBuildPlan.CaptureCreatedInput(') -or $source.Contains('ctx.Created') -or $source.Contains('EndsWith(".meta"')){throw 'Actual cleanup/recovery integration regression.'}
    $result['affectedReferenceCompilation']=ConvertTo-CatalogJsonToken -Value $native
    $result['actualCleanupAndRecoverySharePolicy']=[Newtonsoft.Json.Linq.JValue]::new($true)
    $path=Join-Path $root 'ownership-result.json';[IO.File]::WriteAllText($path,$result.ToString(),[Text.UTF8Encoding]::new($false))
    $read=[Newtonsoft.Json.Linq.JObject]::Parse([IO.File]::ReadAllText($path))
    if(-not [Newtonsoft.Json.Linq.JToken]::DeepEquals($result,$read)){throw 'Ownership result roundtrip changed.'}
    $result.ToString()
}
function Invoke-CatalogBlobTests {
    param([Parameter(Mandatory)][string]$ReportRoot,[switch]$CompileOnly)
    if($env:EATWHAT_TEST_CATALOG_OWNERSHIP_ONLY -eq '1'){return Invoke-CatalogOwnershipTests -ReportRoot $ReportRoot}
    $root=[IO.Path]::GetFullPath($ReportRoot);New-Item -ItemType Directory -Path $root -Force|Out-Null
    $snapshot=Join-Path $root 'serialized-source.json'
    & python (Join-Path $PSScriptRoot 'Fixtures/ReadUnityCatalogSnapshot.py') --project-root $catalogRepository --output $snapshot
    if($LASTEXITCODE -ne 0){throw 'Actual serialized catalog snapshot reader failed.'}
    $inputs=Initialize-CatalogHost -IncludeTests
    if($CompileOnly){return @{action='CATALOG_HOST_COMPILED';sourceSnapshot=$snapshot}}
    $protocol=Test-CatalogNativeResultProtocol
    $serialization=Test-CatalogResultSerialization -ReportRoot (Join-Path $root 'metadata-contract')
    $result=[CatalogBlobTests]::Run($snapshot,(Join-Path $root 'behavior'))
    $native=Invoke-CatalogNativeReferenceCompile -Output (Join-Path $root 'unity-reference')
    $previousMono=$env:MONO_PATH
    try {
        $env:MONO_PATH=$native.monoPath+[IO.Path]::PathSeparator+[IO.Path]::GetDirectoryName($native.json)
        $lines=@(& $native.mono $native.behavior $snapshot (Join-Path $root 'behavior-mono') (Join-Path $root 'behavior/staging/catalog.bin') 2>&1);$monoExit=$LASTEXITCODE
        [IO.File]::WriteAllLines((Join-Path $root 'mono-behavior.log'),[string[]]$lines)
        if($monoExit -ne 0){throw 'Catalog Mono fixture failed; see mono-behavior.log'}
        $mono=Read-CatalogNativeResult -Lines $lines
        if([string]$mono['fingerprint'] -ne [string]$result['fingerprint']){throw 'Host/Mono catalog fingerprint mismatch'}
        [void][CatalogBlobSerializer]::Authenticate([IO.File]::ReadAllBytes((Join-Path $root 'behavior-mono/staging/catalog.bin')),[string]$result['fingerprint'],[EatWhat.Tools.Security.CatalogBlobCipher]::new([EatWhat.Tools.Security.ObfuscatedStaticKeyProvider]::new()))
        $monoMetadata=@{exitCode=$monoExit;assertions=[int]$mono['assertionCount'];fingerprintMatched=$true;hostPacketAccepted=[bool]$mono['hostPacketAccepted'].Value;monoPacketAcceptedByHost=$true}
    } finally { $env:MONO_PATH=$previousMono }
    $result['metadataSerialization']=ConvertTo-CatalogJsonToken -Value $serialization
    $result=Write-CatalogResult -Result $result -Mono $monoMetadata -Protocol $protocol -Native $native -Path (Join-Path $root 'result.json')
    $result.ToString()
}
function Invoke-CatalogBlobOptInExport {
    param([Parameter(Mandatory)][string]$SnapshotPath,[Parameter(Mandatory)][string]$Staging)
    if(-not(Test-Path -LiteralPath $SnapshotPath -PathType Leaf)){throw 'Explicit canonical graph snapshot required.'}
    $inputs=Initialize-CatalogHost
    $graph=[EatWhat.Tools.Persistence.SaveEnvelope]::ParseCanonicalObject([IO.File]::ReadAllText([IO.Path]::GetFullPath($SnapshotPath)))
    $root=Join-Path $catalogRepository '.ai-workspace/outputs'
    $fingerprint=[CatalogBlobStaging]::Export($graph,[IO.Path]::GetFullPath($Staging),$root)
    @{action='CATALOG_BLOB_EXPORTED';fingerprint=$fingerprint;staging=[IO.Path]::GetFullPath($Staging);authenticatedReadback=$true;productionPublish=$false}|ConvertTo-Json -Compress
}
