[CmdletBinding()]
param([string]$UnityEditorPath)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$version=[regex]::Match((Get-Content -LiteralPath (Join-Path $repo 'ProjectSettings/ProjectVersion.txt') -Raw),'m_EditorVersion:\s*([^\r\n]+)').Groups[1].Value.Trim()
if(-not $UnityEditorPath){$UnityEditorPath="C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe"}
if(-not (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion.StartsWith($version,[StringComparison]::Ordinal)){throw 'Unity installation mismatch.'}
$data=Join-Path (Split-Path -Parent $UnityEditorPath) 'Data';$mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe';$csc=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$package=@(Get-ChildItem -LiteralPath (Join-Path $repo 'Library/PackageCache') -Directory -Filter 'com.unity.nuget.newtonsoft-json*')
if($package.Count -ne 1){throw 'Expected exactly one installed Newtonsoft package.'}
$json=Join-Path $package[0].FullName 'Runtime/Newtonsoft.Json.dll'
$refs=@((Join-Path $data 'NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll'),(Join-Path $data 'NetStandard/ref/2.1.0/netstandard.dll'),$json)
$prod=@('Security','Persistence'|ForEach-Object{Get-ChildItem -LiteralPath (Join-Path $repo "Assets/Scripts/Tools/$_") -File -Filter '*.cs'|ForEach-Object FullName})
$fixture=Join-Path $PSScriptRoot 'Fixtures/PersistenceTests.cs'
$base=if($env:EATWHAT_PERSISTENCE_REPORT_ROOT){$env:EATWHAT_PERSISTENCE_REPORT_ROOT}else{Join-Path ([IO.Path]::GetTempPath()) 'EatWhat-Persistence'}
$run=Join-Path $base ('r-'+[guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $run -Force|Out-Null
function Compile([string]$Output,[string]$Target,[string[]]$Files,[string]$Defines){
 $args=@('/nologo','/noconfig','/nostdlib+',"/out:$Output","/target:$Target")+@($refs|ForEach-Object{"/reference:$_"})
 if($Defines){$args+="/define:$Defines"};$args+=$Files
 $lines=@(& $mono $csc @args 2>&1);$lines|Set-Content -LiteralPath ($Output+'.compile.log') -Encoding utf8
 if($LASTEXITCODE -ne 0){throw "Persistence compile failed: $Output.compile.log"}
}
Compile (Join-Path $run 'ToolkitPersistence.dll') 'library' $prod ''
$hostRefs=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' -File|ForEach-Object FullName)+@($json)
Add-Type -Path $json
Add-Type -Path ($prod+@($fixture)) -ReferencedAssemblies $hostRefs
$hostResult=[ToolkitPersistenceTests]::Run((Join-Path $run 'host-files'));$cases=@([ToolkitPersistenceTests]::PassedCases)
$previous=$env:MONO_PATH;$modes=@()
try{
 $env:MONO_PATH=Split-Path -Parent $json
 foreach($mode in @('release','UNITY_EDITOR','DEVELOPMENT_BUILD')){
  $exe=Join-Path $run ("Persistence-$mode.exe");$defines=if($mode -eq 'release'){''}else{$mode};Compile $exe 'exe' ($prod+@($fixture)) $defines
  $lines=@(& $mono $exe (Join-Path $run ("mono-$mode-files")) 2>&1);$exit=$LASTEXITCODE;$lines|Set-Content -LiteralPath ($exe+'.runtime.log') -Encoding utf8
  if($exit -ne 0){throw "Persistence Mono suite failed: $exe.runtime.log"}
  $modes+=@{mode=$mode;exitCode=$exit;assertions=[int]([regex]::Match(($lines -join "`n"),'PERSISTENCE_ASSERTIONS=(\d+)').Groups[1].Value)}
 }
}finally{$env:MONO_PATH=$previous}
[ordered]@{success=$true;productionSourceCount=$prod.Count;hostResult=$hostResult;hostAssertions=$cases.Count;cases=$cases;monoModes=$modes;unityVersion=$version;unityReferenceCompilePassed=$true;jsonReference=@{path=$json;sha256=(Get-FileHash -LiteralPath $json -Algorithm SHA256).Hash;bytes=(Get-Item -LiteralPath $json).Length};hostJsonAssembly=[Newtonsoft.Json.Linq.JObject].Assembly.Location;references=$refs;runDirectory=$run;isolatedRealFileIO=$true;realUserSaveAccess=$false;editorPlayBuildVerified=$false}|ConvertTo-Json -Depth 7
