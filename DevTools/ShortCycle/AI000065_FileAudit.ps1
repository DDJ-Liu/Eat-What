$ErrorActionPreference='Stop'
# Historical audit: archived evidence is extracted to a cache, never over production assets.
function Resolve-AI65Evidence([string]$path) {
 if(Test-Path -LiteralPath $path){return $path}
 $archiveTool=Join-Path $PSScriptRoot '../Workflow/Archive-Stage.ps1'
 return ((& $archiveTool -Action Extract -Stage '2026-09-17' -OriginalPath $path | Out-String) | ConvertFrom-Json).path
}
function Docs65([string]$path) {
 $d=@{}; $raw=Get-Content -LiteralPath $path -Raw -Encoding UTF8
 foreach($m in [regex]::Matches($raw,'(?ms)^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- !u!|\z)')){$d[$m.Groups[2].Value]=@{kind=$m.Groups[1].Value;text=$m.Groups[3].Value}}
 return $d
}
$before65=Docs65 (Resolve-AI65Evidence 'DevTools/Scenes/backup/AI-000065_C-T5b/ShortCycle_P0P1.unity')
$after65=Docs65 'Assets/Scenes/Cooking/ShortCycle_P0P1.unity'
$materialDelta65=@()
foreach($id65 in $before65.Keys){
 if(!$after65.ContainsKey($id65)){continue}
 $b65=$before65[$id65].text; $a65=$after65[$id65].text
 $bm65=[regex]::Match($b65,'m_sharedMaterial: ([^\r\n]+)'); $am65=[regex]::Match($a65,'m_sharedMaterial: ([^\r\n]+)')
 if($bm65.Success -and $am65.Success -and $bm65.Value -ne $am65.Value){$materialDelta65+=@{id=$id65;before=$bm65.Value;after=$am65.Value}}
}
$pre65=Get-Content -LiteralPath (Resolve-AI65Evidence '.ai-workspace/outputs/CK01-J/AI-000064_Preflight.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$frozenChanged65=@($pre65.frozen | Where-Object { !(Test-Path -LiteralPath $_.path) -or (Get-FileHash -LiteralPath $_.path -Algorithm SHA256).Hash -ne $_.hash })
$paths65=@('Captures/AI-000065/P0_Cover.png','Captures/AI-000065/P0_Browse.png','Captures/AI-000065/P0_Catalog.png','Captures/AI-000065/P0_Clue.png','Captures/AI-000065/P0_Detail.png','Captures/AI-000065/P1_Kitchen.png','Captures/AI-000065/P0_Return.png')
$png65=@(foreach($p65 in $paths65){$f65=Get-Item -LiteralPath (Resolve-AI65Evidence $p65); $bytes65=[IO.File]::ReadAllBytes($f65.FullName); [pscustomobject]@{path=$p65;size=$f65.Length;signature=[BitConverter]::ToString($bytes65[0..7]);modified=$f65.LastWriteTime.ToString('o')}})
[pscustomobject]@{directTextMaterialDelta=$materialDelta65;frozenChecked=$pre65.frozen.Count;frozenChanged=$frozenChanged65;png=$png65;sceneHash=(Get-FileHash -LiteralPath 'Assets/Scenes/Cooking/ShortCycle_P0P1.unity').Hash} | ConvertTo-Json -Depth 8
