[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [string]$BaseCharacterList = 'Assets/Fonts/Heavy/ShortCycle_Heavy_WithFallback.asset',
    [string]$StaticGlyphList = 'Assets/Font/Heavy/ShortCycle_Heavy_Static.asset',
    [string]$LocalizationCsv = 'DataTables/Localization/Localization.csv',
    [Parameter(Mandatory)][string]$OutputDirectory,
    [switch]$SkipNarrativeReport
)
$ErrorActionPreference='Stop'
$ProjectRoot=[IO.Path]::GetFullPath($ProjectRoot)
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$script:sourceRecords=[Collections.Generic.List[object]]::new()
$utf8=[Text.UTF8Encoding]::new($false,$true)
function FullPath([string]$Path) {
    [IO.Path]::GetFullPath($(if([IO.Path]::IsPathRooted($Path)){$Path}else{Join-Path $ProjectRoot $Path}))
}
function ReadSource([string]$Path) {
    $full=FullPath $Path
    $bytes=[IO.File]::ReadAllBytes($full)
    $script:sourceRecords.Add([ordered]@{path=$full;bytes=$bytes.Length;sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))})
    $utf8.GetString($bytes).TrimStart([char]0xFEFF)
}
function WriteJson([string]$Name,$Value) {
    [void][IO.Directory]::CreateDirectory($OutputDirectory)
    $path=Join-Path $OutputDirectory $Name
    $temporary=$path+'.pending-'+[guid]::NewGuid().ToString('N')
    [IO.File]::WriteAllText($temporary,($Value|ConvertTo-Json -Depth 18),[Text.UTF8Encoding]::new($false))
    [IO.File]::Move($temporary,$path,$true)
}
function ReadCsv([string]$Path) {
    $parsed=[EatWhat.DataTables.DataTableCsvParser]::Parse((ReadSource $Path))
    if(-not $parsed.IsValid){throw ("CSV parse failed: "+$Path+" : "+($parsed.Errors -join ' | '))}
    return $parsed.Table
}
function ReadCodepoints([string]$Path) {
    $text=ReadSource $Path
    $set=[Collections.Generic.HashSet[uint]]::new()
    if([IO.Path]::GetExtension($Path) -eq '.asset') {
        $values=@([regex]::Matches($text,'(?m)^\s*m_Unicode:\s*(\d+)\s*$')|ForEach-Object{$_.Groups[1].Value})
    } else {
        $values=@($text -split '\r?\n'|Where-Object{$_.Trim() -and -not $_.Trim().StartsWith('#')})
    }
    foreach($value in $values) {
        $token=$value.Trim()
        if($token -match '^U\+([0-9A-Fa-f]{4,6})$'){$point=[Convert]::ToUInt32($Matches[1],16)}
        elseif($token -match '^\d+$'){$point=[uint]::Parse($token)}
        else{throw "Invalid codepoint in $Path : $token"}
        if($point -gt 0x10FFFF -or ($point -ge 0xD800 -and $point -le 0xDFFF)){throw "Not a Unicode scalar in $Path : $token"}
        [void]$set.Add($point)
    }
    if($set.Count -eq 0){throw "No readable character entries: $Path"}
    return ,$set
}
function VisibleText([string]$Text) {
    # Card FONT-ATLAS-AUTO-a excludes tag names. Unknown/literal <...> stays.
    $tags='b|i|u|s|color|alpha|size|font|font-weight|material|mark|link|sprite|style|align|indent|line-indent|line-height|margin|margin-left|margin-right|cspace|mspace|pos|voffset|width|rotate|nobr|lowercase|uppercase|allcaps|smallcaps|sub|sup|strikethrough|page|space|gradient'
    $result=[Text.StringBuilder]::new()
    foreach($part in [regex]::Split($Text,'(?is)(<noparse>.*?</noparse>)')) {
        if($part -match '^(?is)<noparse>(.*?)</noparse>$'){[void]$result.Append($Matches[1]);continue}
        $part=[regex]::Replace($part,'(?i)<br\s*/?>',[string][char]10)
        # TMP_Text supports RGB/RGBA short colors and six/eight hex digits.
        $part=[regex]::Replace($part,'(?i)<#(?:[0-9a-f]{3}|[0-9a-f]{4}|[0-9a-f]{6}|[0-9a-f]{8})>|</scale>','')
        $number='[+-]?(?:\d+(?:\.\d*)?|\.\d+)'
        $scalePattern='(?i)<scale=(?:"(?<number>'+$number+')"|(?<number>'+$number+'))>'
        $part=[regex]::Replace($part,$scalePattern,[Text.RegularExpressions.MatchEvaluator]{
            param($match)
            [single]$scaleValue=0
            if([single]::TryParse($match.Groups['number'].Value,[Globalization.NumberStyles]::Float,[Globalization.CultureInfo]::InvariantCulture,[ref]$scaleValue) -and
                -not [single]::IsInfinity($scaleValue) -and $scaleValue -ne [int16]::MinValue){return ''}
            return $match.Value
        })
        [void]$result.Append([regex]::Replace($part,('(?i)</?(?:'+$tags+')(?:\s+[^<>]*|=[^<>]*)?\s*>'),''))
    }
    $result.ToString()
}
function AddScalars([string]$Text,$Set) {
    $value=VisibleText $Text
    for($index=0;$index -lt $value.Length;$index++) {
        $ch=$value[$index]
        if([char]::IsHighSurrogate($ch) -and $index+1 -lt $value.Length -and [char]::IsLowSurrogate($value[$index+1])) {
            [void]$Set.Add([uint][char]::ConvertToUtf32($ch,$value[++$index]))
        } elseif(-not [char]::IsSurrogate($ch)){[void]$Set.Add([uint]$ch)}
    }
}
function Entries($Values) {
    @($Values|Sort-Object|ForEach-Object{
        $cp=[uint]$_
        [ordered]@{codepoint=('U+{0:X4}' -f $cp);character=[char]::ConvertFromUtf32([int]$cp)}
    })
}
try {
    foreach($inputPath in @($BaseCharacterList,$StaticGlyphList,$LocalizationCsv)) {
        if((FullPath $inputPath).StartsWith($OutputDirectory+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){
            throw 'Outputs must not contain/overwrite input files.'
        }
    }
    $parserPath=Join-Path $ProjectRoot 'Assets/Editor/DataTables/DataTableValidationCore.cs'
    if(-not ('EatWhat.DataTables.DataTableCsvParser' -as [type])){Add-Type -Path $parserPath}
    $base=ReadCodepoints $BaseCharacterList
    $existing=ReadCodepoints $StaticGlyphList
    $table=ReadCsv $LocalizationCsv
    $keyIndex=$table.GetHeaderIndex('loc_key');$textIndex=$table.GetHeaderIndex('zh_cn')
    if($keyIndex -lt 0 -or $textIndex -lt 0){throw 'Localization CSV requires loc_key and zh_cn headers.'}
    $localized=[Collections.Generic.HashSet[uint]]::new()
    $lookup=[Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    foreach($row in $table.Rows) {
        if($row.Values.Count -le [Math]::Max($keyIndex,$textIndex)){throw 'Localization row is shorter than its required columns.'}
        $key=$row.Values[$keyIndex];$text=$row.Values[$textIndex]
        if($lookup.ContainsKey($key)){throw "Duplicate Localization loc_key: $key"}
        $lookup.Add($key,$text);AddScalars $text $localized
    }
    $required=[Collections.Generic.HashSet[uint]]::new($base);$required.UnionWith($localized)
    $missing=@($required|Where-Object{-not $existing.Contains($_)}|Sort-Object)
    # TMP_FontAsset.AddSynthesizedCharacters in the installed UGUI package.
    $layoutControls=@(3,9,10,11,13,0x061C,0x200B,0x200E,0x200F,0x2028,0x2029,0x2060)
    $rasterMissing=@($missing|Where-Object{$_ -notin $layoutControls})
    $decision=[ordered]@{
        schemaVersion=1;status='OK';requiresRebuild=($rasterMissing.Count -gt 0)
        reason=$(if($rasterMissing.Count){'NEW_RENDERABLE_CODEPOINTS'}else{'NO_NEW_RENDERABLE_CODEPOINTS'})
        policy='legacy-character-table-plus-all-Localization.zh_cn-visible-text-v1'
        parser='Existing DataTableCsvParser';narrativeExcludedFromDecision=$true
        baseCharacterCount=$base.Count;localizationCharacterCount=$localized.Count
        staticCharacterCount=$existing.Count;requiredCharacterCount=$required.Count
        addedCodepoints=@(Entries $rasterMissing);missingSerializedCodepoints=@(Entries $missing)
        synthesizedLayoutControls=@(Entries @($missing|Where-Object{$_ -in $layoutControls}))
        requiredCodepoints=@($required|Sort-Object|ForEach-Object{'U+{0:X4}' -f $_})
        existingCharactersWillBeRetained=$true;sources=@($script:sourceRecords.ToArray())
        compatibilityNote='Legacy Builder collects raw tag syntax. This card excludes recognized rich-text tags in the decision only; Builder unchanged. Layout controls are not raster rebuild requests.'
    }
    $narrative=$null
    if(-not $SkipNarrativeReport) {
        $schemaPaths=@('Narrative/Characters','Narrative/Dialogues','Narrative/DialogueLines','Narrative/CGs','Street/Scenes','Street/AmbientPools','Street/SceneStates','Street/SceneStatePlacements')
        $all=[Collections.Generic.HashSet[uint]]::new();$tables=[Collections.Generic.List[object]]::new()
        foreach($name in $schemaPaths) {
            $schema=ReadSource ('DataTables/Schemas/'+$name+'.schema.json')|ConvertFrom-Json -DateKind String
            $csv=ReadCsv ('DataTables/'+$schema.csvPath)
            $points=[Collections.Generic.HashSet[uint]]::new();$refs=[Collections.Generic.List[object]]::new();$unresolved=[Collections.Generic.List[string]]::new()
            $idIndex=$csv.GetHeaderIndex($schema.primaryKeyField)
            foreach($column in @($schema.columns|Where-Object{$_.type -eq 'loc_key'})) {
                $colIndex=$csv.GetHeaderIndex($column.csvColumn)
                if($colIndex -lt 0){throw "Missing narrative column: $name/$($column.csvColumn)"}
                foreach($row in $csv.Rows) {
                    if($row.Values.Count -le $colIndex){throw "Short narrative row: $name"}
                    $key=$row.Values[$colIndex]
                    if([string]::IsNullOrEmpty($key)){continue}
                    if(-not $lookup.ContainsKey($key)){$unresolved.Add($key);continue}
                    AddScalars $lookup[$key] $points
                    $refs.Add([ordered]@{rowId=$(if($idIndex -ge 0){$row.Values[$idIndex]}else{$null});column=$column.csvColumn;locKey=$key;text=$lookup[$key]})
                }
            }
            $all.UnionWith($points)
            $tables.Add([ordered]@{table=$schema.tableName;csvPath=$schema.csvPath;rowCount=$csv.Rows.Count;referencedText=@($refs.ToArray());missingLocalizationKeys=@($unresolved.ToArray()|Sort-Object -Unique);textCodepointCount=$points.Count;missingFromStatic=@(Entries @($points|Where-Object{-not $existing.Contains($_)}))})
        }
        $gaps=@($all|Where-Object{-not $existing.Contains($_)}|Sort-Object)
        $narrative=[ordered]@{schemaVersion=1;reportOnly=$true;includedInRebuildDecision=$false;scope='Eight schema-defined tables: loc_key references resolved through Localization.zh_cn; IDs, commands, filenames and comments excluded.';tableCount=8;textCodepointCount=$all.Count;missingStaticCount=$gaps.Count;missingFromStatic=@(Entries $gaps);tables=@($tables.ToArray());sources=@($script:sourceRecords.ToArray())}
    }
    WriteJson 'font-glyph-decision.json' $decision
    if($null -ne $narrative){WriteJson 'narrative-glyph-gaps.json' $narrative}
    $decision|ConvertTo-Json -Depth 18
} catch {
    $failure=[ordered]@{schemaVersion=1;status='ERROR';requiresRebuild=$null;error=$_.Exception.Message;sources=@($script:sourceRecords.ToArray())}
    WriteJson 'font-glyph-decision.json' $failure
    throw
}
