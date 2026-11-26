[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputRoot)
$ErrorActionPreference='Stop'
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
[void][IO.Directory]::CreateDirectory($OutputRoot)
$repo=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$tool=Join-Path $PSScriptRoot 'Get-FontGlyphDecision.ps1'
$utf8=[Text.UTF8Encoding]::new($false)
$results=[Collections.Generic.List[object]]::new()
function Put([string]$Path,[string]$Text){[IO.File]::WriteAllText($Path,$Text,$utf8)}
function RunCase([string]$Id,[string]$Csv,[uint[]]$Existing) {
    $root=Join-Path $OutputRoot $Id
    [void][IO.Directory]::CreateDirectory($root)
    Put (Join-Path $root 'base.txt') 'U+7532'
    Put (Join-Path $root 'static.txt') (($Existing|ForEach-Object{'U+{0:X4}' -f $_}) -join [Environment]::NewLine)
    Put (Join-Path $root 'Localization.csv') $Csv
    $raw=& $tool -ProjectRoot $repo -BaseCharacterList (Join-Path $root 'base.txt') -StaticGlyphList (Join-Path $root 'static.txt') -LocalizationCsv (Join-Path $root 'Localization.csv') -OutputDirectory (Join-Path $root 'report') -SkipNarrativeReport
    return ($raw|ConvertFrom-Json -DateKind String)
}
$nl=[string][char]10
$baseline='loc_key,zh_cn,comment'+$nl+'old,甲,'+$nl
# 1. New key with exactly one new rendered scalar.
$a=RunCase '01-new-character' ($baseline+'new,乙,'+$nl) @([uint]0x7532)
if(-not $a.requiresRebuild -or @($a.addedCodepoints).Count -ne 1 -or $a.addedCodepoints[0].codepoint -ne 'U+4E59'){throw 'Case1: expected only U+4E59.'}
$results.Add(@{id=1;passed=$true;name='new key/new glyph rebuild';added=@($a.addedCodepoints)})
# 2. Key identity alone never requests a new atlas.
$b=RunCase '02-key-only' ($baseline+'new,甲甲,'+$nl) @([uint]0x7532)
if($b.requiresRebuild -or @($b.addedCodepoints).Count){throw 'Case2: existing characters must skip.'}
$results.Add(@{id=2;passed=$true;name='new key/existing glyphs skip'})
# 3. Unchanged input skips.
$c=RunCase '03-unchanged' $baseline @([uint]0x7532)
if($c.requiresRebuild -or $c.status -ne 'OK'){throw 'Case3: unchanged input must skip.'}
$results.Add(@{id=3;passed=$true;name='unchanged input skip'})
# 4. Actual project CSV parser handles multiline quoting; markup is not glyph text.
$emoji=[char]::ConvertFromUtf32(0x1F600)
$value='<color=#fff><b>甲</b></color><#F00>甲</color><#F007>甲</color><#fF0000>甲</color><#ff000080>甲</color><scale=1.25>甲</scale><scale=".5">甲</scale> ，'+$nl+[char]9+$emoji
$csv='loc_key,zh_cn,comment'+$nl+'old,"'+$value.Replace('"','""')+'",'+$nl
$d=RunCase '04-text-rules' $csv @([uint]0x7532,[uint]0x20,[uint]0xFF0C,[uint]0x1F600)
if($d.requiresRebuild -or @($d.addedCodepoints).Count -or 'U+0062' -in $d.requiredCodepoints -or 'U+1F600' -notin $d.requiredCodepoints -or 'U+000A' -notin $d.requiredCodepoints){throw 'Case4: markup, whitespace, punctuation, newline or scalar semantics differ.'}
$literal='<noparse><b>甲</b></noparse><xyz>甲</xyz>甲 < 甲 > 甲<#oops>'
$literalExisting=@([uint]0x7532,[uint]0x20)+@('<b>/xyz#oops'.ToCharArray()|ForEach-Object{[uint]$_})+@([uint][char]'>')
$literalCsv='loc_key,zh_cn,comment'+$nl+'literal,"'+$literal+'",'+$nl
$literalResult=RunCase '04-literal-text' $literalCsv $literalExisting
if($literalResult.requiresRebuild -or 'U+0062' -notin $literalResult.requiredCodepoints -or 'U+0078' -notin $literalResult.requiredCodepoints -or 'U+0023' -notin $literalResult.requiredCodepoints){throw 'Case4: unknown/literal/noparse text must remain visible.'}
$results.Add(@{id=4;passed=$true;name='quoted multiline/whitespace/punctuation/TMP colors/scale/literal/noparse/supplementary scalar';controls=@($d.synthesizedLayoutControls)})
# 5. Missing input replaces an old false decision with ERROR/null and throws.
$root=Join-Path $OutputRoot '05-missing'
[void][IO.Directory]::CreateDirectory((Join-Path $root 'report'))
Put (Join-Path $root 'static.txt') 'U+7532'
Put (Join-Path $root 'Localization.csv') $baseline
Put (Join-Path $root 'report/font-glyph-decision.json') '{"status":"OK","requiresRebuild":false}'
$threw=$false;$errorText=$null
try{& $tool -ProjectRoot $repo -BaseCharacterList (Join-Path $root 'missing.txt') -StaticGlyphList (Join-Path $root 'static.txt') -LocalizationCsv (Join-Path $root 'Localization.csv') -OutputDirectory (Join-Path $root 'report') -SkipNarrativeReport|Out-Null}catch{$threw=$true;$errorText=$_.Exception.Message}
$e=Get-Content -Raw -Encoding utf8 (Join-Path $root 'report/font-glyph-decision.json')|ConvertFrom-Json
if(-not $threw -or $e.status -ne 'ERROR' -or $null -ne $e.requiresRebuild){throw 'Case5: missing input must not leave a skip verdict.'}
$results.Add(@{id=5;passed=$true;name='missing input fails without false skip';error=$errorText})
$summary=[ordered]@{success=$true;caseCount=5;passed=5;projectCsvParser=$true;cases=@($results.ToArray());fixtureRoot=$OutputRoot;realTablesOrFontsWritten=$false}
$summary|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $OutputRoot 'fixture-results.json') -Encoding utf8
$summary|ConvertTo-Json -Depth 12
