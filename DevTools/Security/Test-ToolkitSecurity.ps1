[CmdletBinding()]
param([string]$UnityEditorPath)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$version=[regex]::Match((Get-Content -LiteralPath (Join-Path $repoRoot 'ProjectSettings/ProjectVersion.txt') -Raw),'m_EditorVersion:\s*([^\r\n]+)').Groups[1].Value.Trim()
if(-not $UnityEditorPath){$UnityEditorPath="C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"}
if(-not (Test-Path -LiteralPath $UnityEditorPath) -or -not (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion.StartsWith($version,[StringComparison]::Ordinal)){throw 'Unity installation version mismatch.'}
$data=Join-Path (Split-Path -Parent $UnityEditorPath) 'Data'
$mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe'
$csc=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$refs=@((Join-Path $data 'NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll'),(Join-Path $data 'NetStandard/ref/2.1.0/netstandard.dll'))
$source=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/Scripts/Tools/Security') -Filter '*.cs' -File|ForEach-Object FullName)+@('ISaveCodec.cs','EncryptedJsonCodec.cs'|ForEach-Object{Join-Path $repoRoot "Assets/Scripts/Tools/Persistence/$_"})
$fixture=Join-Path $PSScriptRoot 'Fixtures/SecurityTests.cs'
$probe=Join-Path $PSScriptRoot 'Fixtures/SecurityCapabilityProbe.cs'
$reportRoot=if($env:EATWHAT_SECURITY_REPORT_ROOT){$env:EATWHAT_SECURITY_REPORT_ROOT}else{Join-Path ([IO.Path]::GetTempPath()) 'EatWhat-ToolkitSecurity'}
$run=Join-Path $reportRoot ('r-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force|Out-Null
function Compile([string]$Target,[string]$Output,[string[]]$Files,[string[]]$AdditionalRefs){
 $arguments=@('/nologo','/noconfig','/nostdlib+',"/target:$Target","/out:$Output")+@($refs+$AdditionalRefs|ForEach-Object{"/reference:$_"})+$Files
 $lines=@(& $mono $csc @arguments 2>&1)
 [IO.File]::WriteAllLines(($Output+'.compile.log'),[string[]]$lines)
 if($LASTEXITCODE -ne 0 -or -not(Test-Path -LiteralPath $Output)){throw "Unity reference compilation failed; inspect $Output.compile.log"}
}
$productionText=($source|ForEach-Object{Get-Content -LiteralPath $_ -Raw})-join "`n"
if($productionText -match 'UnityEngine|UnityEditor|ShortCycle|Generated\.DataTables|System\.Text\.Json|Newtonsoft|System\.Reflection|Assembly\.Load|File\.|Directory\.|PlayerPrefs|persistentDataPath'){throw 'Security/codec dependency or IO contract violation.'}
$dll=Join-Path $run 'ToolkitSecurity.dll';$exe=Join-Path $run 'SecurityTests.exe';$probeExe=Join-Path $run 'CapabilityProbe.exe'
Compile 'library' $dll $source @()
Compile 'exe' $exe @($fixture) @($dll)
Compile 'exe' $probeExe @($probe) @()
$gcm=@(& $mono $probeExe 2>&1);if($LASTEXITCODE -ne 0){throw 'Bundled Mono capability probe failed to run.'}
$gcm|Set-Content -LiteralPath (Join-Path $run 'gcm-mono.log') -Encoding utf8
Add-Type -Path ($source+@($fixture))
$hostResult=[ToolkitSecurityTests]::Run()
$hostCases=@([ToolkitSecurityTests]::PassedCases)
$hostPacket=[Convert]::ToBase64String([ToolkitSecurityTests]::EncodeCrossSample())
$monoOutput=@(& $mono $exe $hostPacket 2>&1);$monoExit=$LASTEXITCODE
$monoOutput|Set-Content -LiteralPath (Join-Path $run 'suite-mono.log') -Encoding utf8
if($monoExit -ne 0){throw "Bundled Mono security suite failed; inspect $run/suite-mono.log"}
$match=[regex]::Match(($monoOutput -join "`n"),'CROSS_BLOB=([A-Za-z0-9+/=]+)')
if(-not $match.Success -or -not [ToolkitSecurityTests]::DecodeCrossSample([Convert]::FromBase64String($match.Groups[1].Value))){throw 'Bundled Mono to host interoperability failed.'}
$hostGcmSupported=$null
try{$hostGcmSupported=[System.Security.Cryptography.AesGcm]::IsSupported}catch{}
$result=[ordered]@{
 success=$true;algorithm='AES-256-CBC + HMAC-SHA256 (explicit version2)';productionSourceCount=$source.Count
 host=@{framework=[Runtime.InteropServices.RuntimeInformation]::FrameworkDescription;result=$hostResult;assertionCount=$hostCases.Count;gcmSupported=$hostGcmSupported}
 mono=@{path=$mono;version=(@(& $mono --version)[0]);suiteExit=$monoExit;assertionCount=[int]([regex]::Match(($monoOutput -join "`n"),'SECURITY_ASSERTIONS=(\d+)').Groups[1].Value)}
 unity=@{version=$version;referenceCompilePassed=$true;references=@($refs|ForEach-Object{@{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $_).Length}});gcmProbeOutput=($gcm -join "`n");gcmRuntimeSupported=(($gcm -join "`n") -match 'GCM_SUPPORTED=true')}
 hostToMonoPassed=$true;monoToHostPassed=$true;cases=$hostCases;runDirectory=$run
 il2cppBuildVerified=$false;editorConsolePlayVerified=$false;realSaveOrDataWrites=0
}
$result|ConvertTo-Json -Depth 8
