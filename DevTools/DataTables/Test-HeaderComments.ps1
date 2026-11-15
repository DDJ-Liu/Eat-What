[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Authoring/Authoring.Common.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "[HEADER_COMMENT_TEST] $message" }
}
function Get-ThreadTexts([string]$path) {
    $zip = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $texts = [Collections.Generic.List[string]]::new()
        foreach ($entry in @($zip.Entries | Where-Object { $_.FullName -match '^xl/threadedcomments/threadedcomment.*[.]xml$' })) {
            $reader = [IO.StreamReader]::new($entry.Open())
            try { $xml = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
            foreach ($node in $xml.DocumentElement.ChildNodes) { [void]$texts.Add([string]$node.InnerText) }
        }
        return $texts.ToArray()
    } finally { $zip.Dispose() }
}
function Write-XmlPart($zip, [string]$name, [xml]$xml) {
    $old = $zip.GetEntry($name)
    if (-not $old) { throw "[HEADER_COMMENT_TEST_PART] $name" }
    $old.Delete()
    $entry = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
    $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
    try { $xml.Save($writer) } finally { $writer.Dispose() }
}
function Read-XmlPart($zip, [string]$name) {
    $entry = $zip.GetEntry($name)
    if (-not $entry) { throw "[HEADER_COMMENT_TEST_PART] $name" }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { return [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
}

$source = (Resolve-Path (Join-Path $PSScriptRoot 'Authoring/DataTables_Authoring.xlsx')).Path
$sourceSha = Get-AuthoringFileSha256 $source
$root = Join-Path ([IO.Path]::GetTempPath()) ('AI189-test-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$first = Join-Path $root 'first.xlsx'
$second = Join-Path $root 'second.xlsx'
$withUser = Join-Path $root 'with-user.xlsx'
$preserved = Join-Path $root 'preserved.xlsx'
$fixtureSource = Join-Path $root 'legacy-fixture.xlsx'
try {
    $plan = (Invoke-AuthoringNode -Command 'header-comment-plan' -Arguments @{'data-root'=(Resolve-Path (Join-Path $PSScriptRoot '../../DataTables')).Path}) | ConvertFrom-Json -Depth 30
    Assert ($plan.tableCount -eq 22 -and $plan.fieldCount -eq 224) 'Schema coverage must be 22 tables / 224 fields.'
    $expectedTexts = @($plan.tables | ForEach-Object { $_.comments } | ForEach-Object { $_.text })
    Assert (@($expectedTexts | Select-Object -Unique).Count -eq 224) 'Every field must have a distinct table-aware explanation.'
    $steps = @($plan.tables | Where-Object tableName -eq 'RecipeSteps')[0]
    $ingredients = @($steps.comments | Where-Object field -eq 'standard_ingredients')[0].text
    Assert ($ingredients.Contains('ing_') -and $ingredients.Contains('prd_') -and $ingredients.Contains("多值分隔符：';'") -and $ingredients.Contains('Items.item_id')) 'Mixed Ingredients and semicolon contract missing.'
    $areas = @($plan.tables | Where-Object tableName -eq 'KitchenAreas')[0]
    $preset = @($areas.comments | Where-Object field -eq 'preset_tools')[0].text
    Assert ($preset.Contains('已弃用') -and $preset.Contains("多值分隔符：'|'")) 'Legacy preset_tools contract missing.'

    # AI540: own TEMP input with exact historical Type/Reference templates.
    & (Join-Path $PSScriptRoot 'Authoring/New-AuthoringWorkbook.ps1') -OutputWorkbook $fixtureSource | Out-Null
    $fixtureBefore=Get-AuthoringWorkbookContentSnapshot $fixtureSource
    $fixtureZip=[IO.Compression.ZipFile]::Open($fixtureSource,[IO.Compression.ZipArchiveMode]::Update)
    $fixtureExpectedReferences=0
    try{
        $fixtureBook=Read-XmlPart $fixtureZip 'xl/workbook.xml';$fixtureBookRels=Read-XmlPart $fixtureZip 'xl/_rels/workbook.xml.rels'
        $fixtureParts=@{}
        foreach($fixtureSheet in $fixtureBook.SelectNodes('//*[local-name()="sheets"]/*[local-name()="sheet"]')){
            $fixtureId=$fixtureSheet.GetAttribute('id','http://schemas.openxmlformats.org/officeDocument/2006/relationships')
            $fixtureRel=@($fixtureBookRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Id') -ceq $fixtureId})[0]
            $fixtureParts[$fixtureSheet.GetAttribute('name')]=[Uri]::new([Uri]'http://fixture/xl/workbook.xml',$fixtureRel.GetAttribute('Target')).AbsolutePath.TrimStart('/')
        }
        foreach($fixtureTable in $plan.tables){
            $fixtureSheetPart=$fixtureParts[[string]$fixtureTable.tableName]
            $fixtureRelsPart=[IO.Path]::GetDirectoryName($fixtureSheetPart).Replace([char]92,[char]47)+'/_rels/'+[IO.Path]::GetFileName($fixtureSheetPart)+'.rels'
            $fixtureSheetRels=Read-XmlPart $fixtureZip $fixtureRelsPart
            $fixtureThreadRel=@($fixtureSheetRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type').EndsWith('/threadedComment',[StringComparison]::Ordinal)})[0]
            $fixtureLegacyRel=@($fixtureSheetRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type').EndsWith('/comments',[StringComparison]::Ordinal)})[0]
            $fixtureThreadPart=[Uri]::new([Uri]('http://fixture/'+$fixtureSheetPart),$fixtureThreadRel.GetAttribute('Target')).AbsolutePath.TrimStart('/')
            $fixtureLegacyPart=[Uri]::new([Uri]('http://fixture/'+$fixtureSheetPart),$fixtureLegacyRel.GetAttribute('Target')).AbsolutePath.TrimStart('/')
            $fixtureThreads=Read-XmlPart $fixtureZip $fixtureThreadPart;$fixtureLegacy=Read-XmlPart $fixtureZip $fixtureLegacyPart
            $fixtureAuthors=$fixtureLegacy.SelectSingleNode('//*[local-name()="authors"]');$fixtureLegacyList=$fixtureLegacy.SelectSingleNode('//*[local-name()="commentList"]')
            foreach($fixtureField in $fixtureTable.comments){
                $fixturePrimary=@($fixtureThreads.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('ref') -ceq $fixtureField.address -and -not $_.HasAttribute('parentId')})
                if($fixturePrimary.Count -ne 1 -or $fixturePrimary[0].InnerText -cne $fixtureField.text){throw '[HEADER_FIXTURE_INPUT] Expected one newly generated explanation for each header.'}
                $fixturePrimary=$fixturePrimary[0];$fixtureId=$fixturePrimary.GetAttribute('id')
                $fixtureAuthorList=@($fixtureAuthors.ChildNodes)
                $fixtureCompanion=@($fixtureLegacyList.ChildNodes|Where-Object{$_.GetAttribute('ref') -ceq $fixtureField.address -and $fixtureAuthorList[[int]$_.GetAttribute('authorId')].InnerText -ceq ('tc='+$fixtureId)})
                if($fixtureCompanion.Count -ne 1){throw '[HEADER_FIXTURE_COMPANION] Expected the exact generated legacy companion.'}
                $fixturePrimary.FirstChild.InnerText=[string]$fixtureField.legacyToolTexts[0]
                $fixtureText=$fixtureCompanion[0].SelectSingleNode('./*[local-name()="text"]');$fixtureText.RemoveAll();$fixtureT=$fixtureLegacy.CreateElement('t',$fixtureLegacy.DocumentElement.NamespaceURI);$fixtureT.InnerText=[string]$fixtureField.legacyToolTexts[0];[void]$fixtureText.AppendChild($fixtureT)
                foreach($fixtureReference in @($fixtureField.legacyToolTexts|Select-Object -Skip 1)){
                    $fixtureNewId='{'+[guid]::NewGuid().ToString().ToUpperInvariant()+'}'
                    $fixtureThread=$fixtureThreads.CreateElement('xltc','threadedComment',$fixtureThreads.DocumentElement.NamespaceURI)
                    foreach($fixtureAttribute in @('ref','personId','dT')){$fixtureThread.SetAttribute($fixtureAttribute,$fixturePrimary.GetAttribute($fixtureAttribute))};$fixtureThread.SetAttribute('id',$fixtureNewId)
                    $fixtureBody=$fixtureThreads.CreateElement('xltc','text',$fixtureThreads.DocumentElement.NamespaceURI);$fixtureBody.InnerText=[string]$fixtureReference;[void]$fixtureThread.AppendChild($fixtureBody);[void]$fixtureThreads.DocumentElement.AppendChild($fixtureThread)
                    $fixtureAuthorId=$fixtureAuthors.ChildNodes.Count;$fixtureAuthor=$fixtureLegacy.CreateElement('author',$fixtureLegacy.DocumentElement.NamespaceURI);$fixtureAuthor.InnerText='tc='+$fixtureNewId;[void]$fixtureAuthors.AppendChild($fixtureAuthor)
                    $fixtureComment=$fixtureLegacy.CreateElement('comment',$fixtureLegacy.DocumentElement.NamespaceURI);$fixtureComment.SetAttribute('ref',[string]$fixtureField.address);$fixtureComment.SetAttribute('authorId',[string]$fixtureAuthorId)
                    $fixtureText=$fixtureLegacy.CreateElement('text',$fixtureLegacy.DocumentElement.NamespaceURI);$fixtureT=$fixtureLegacy.CreateElement('t',$fixtureLegacy.DocumentElement.NamespaceURI);$fixtureT.InnerText=[string]$fixtureReference;[void]$fixtureText.AppendChild($fixtureT);[void]$fixtureComment.AppendChild($fixtureText);[void]$fixtureLegacyList.AppendChild($fixtureComment)
                    $fixtureExpectedReferences++
                }
            }
            Write-XmlPart $fixtureZip $fixtureThreadPart $fixtureThreads;Write-XmlPart $fixtureZip $fixtureLegacyPart $fixtureLegacy
        }
    }finally{$fixtureZip.Dispose()}
    $fixtureAfter=Get-AuthoringWorkbookContentSnapshot $fixtureSource
    $fixtureContent=Compare-AuthoringWorkbookContent $fixtureBefore $fixtureAfter;$fixtureStructure=Compare-AuthoringWorkbookStructure $fixtureBefore $fixtureAfter
    $fixtureTexts=@(Get-ThreadTexts $fixtureSource);$fixtureTypeCount=@($fixtureTexts|Where-Object{$_.StartsWith('Type:')}).Count;$fixtureReferenceCount=@($fixtureTexts|Where-Object{$_.StartsWith('Reference:')}).Count
    $fixtureKnown=@($plan.tables|ForEach-Object comments|ForEach-Object legacyToolTexts)
    if($fixtureTypeCount -ne 224 -or $fixtureReferenceCount -ne $fixtureExpectedReferences -or $fixtureReferenceCount -le 0 -or @($fixtureTexts|Where-Object{$fixtureKnown -cnotcontains $_}).Count -ne 0 -or -not $fixtureContent.isEqual -or -not $fixtureStructure.isEqual){throw '[HEADER_FIXTURE_LEGACY] Real exact Type/Reference inputs and unchanged content/structure are required.'}
    $fixtureSha=Get-AuthoringFileSha256 $fixtureSource
    [pscustomobject]@{action='AI540_HEADER_FIXTURE';types=$fixtureTypeCount;references=$fixtureReferenceCount;exactKnownTemplates=$true;contentDifferenceCount=$fixtureContent.differenceCount;structureDifferenceCount=$fixtureStructure.differenceCount;fixtureSha256=$fixtureSha;protectedProductionSha256=$sourceSha}|ConvertTo-Json -Compress
    $result = & (Join-Path $PSScriptRoot 'Authoring/Update-HeaderComments.ps1') -InputWorkbook $fixtureSource -CandidateWorkbook $first -ExpectedInputSha256 $fixtureSha
    Assert ($result.fieldCount -eq 224 -and $result.contentDifferenceCount -eq 0 -and $result.structureDifferenceCount -eq 0) 'Candidate altered workbook content or structure.'
    $texts = @(Get-ThreadTexts $first)
    Assert (@($texts | Where-Object { $_.StartsWith('[AI189_HEADER_DOC]') }).Count -eq 224) 'Exactly one generated explanation per schema header is required.'
    Assert (@($texts | Where-Object { $_.StartsWith('Type:') -or $_.StartsWith('Reference:') }).Count -eq 0) 'Old tool-generated warning threads must be merged.'
    foreach ($text in $expectedTexts) { Assert ($texts -ccontains $text) 'Generated workbook omitted an expected field explanation.' }

    $repeat = & (Join-Path $PSScriptRoot 'Authoring/Update-HeaderComments.ps1') -InputWorkbook $first -CandidateWorkbook $second -ExpectedInputSha256 (Get-AuthoringFileSha256 $first)
    Assert ($repeat.updatedThreads -eq 0 -and $repeat.removedToolThreads -eq 0) 'Repeated execution must not add or replace threads.'
    Assert (@(Get-ThreadTexts $second | Where-Object { $_.StartsWith('[AI189_HEADER_DOC]') }).Count -eq 224) 'Repeated execution stacked generated threads.'

    [IO.File]::Copy($first, $withUser, $false)
    $zip = [IO.Compression.ZipFile]::Open($withUser, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $persons = Read-XmlPart $zip 'xl/persons/person.xml'
        $ns = $persons.DocumentElement.NamespaceURI
        $person = $persons.CreateElement('person', $ns)
        $person.SetAttribute('displayName', 'Human Fixture')
        $person.SetAttribute('id', '{5ED12451-3F3B-4D24-8D58-982B64EF08F1}')
        [void]$persons.DocumentElement.AppendChild($person)
        Write-XmlPart $zip 'xl/persons/person.xml' $persons
        $threads = Read-XmlPart $zip 'xl/threadedcomments/threadedcomment.xml'
        $ns = $threads.DocumentElement.NamespaceURI
        $user = $threads.CreateElement('threadedComment', $ns)
        $user.SetAttribute('ref', 'A1')
        $user.SetAttribute('personId', '{5ED12451-3F3B-4D24-8D58-982B64EF08F1}')
        $user.SetAttribute('id', '{6FC8D1A5-213C-470B-8B79-8D70E5DF32EF}')
        $body = $threads.CreateElement('text', $ns)
        $body.InnerText = 'Type: USER_NOTE_MUST_SURVIVE'
        [void]$user.AppendChild($body)
        [void]$threads.DocumentElement.AppendChild($user)
        Write-XmlPart $zip 'xl/threadedcomments/threadedcomment.xml' $threads
    } finally { $zip.Dispose() }
    $userResult = & (Join-Path $PSScriptRoot 'Authoring/Update-HeaderComments.ps1') -InputWorkbook $withUser -CandidateWorkbook $preserved -ExpectedInputSha256 (Get-AuthoringFileSha256 $withUser)
    Assert ($userResult.preservedUserThreads -ge 1) 'User-authored header thread was not recognized as protected.'
    Assert (@(Get-ThreadTexts $preserved | Where-Object { $_ -eq 'Type: USER_NOTE_MUST_SURVIVE' }).Count -eq 1) 'User-authored remark was removed or changed.'
    Assert (@(Get-ThreadTexts $preserved | Where-Object { $_.StartsWith('[AI189_HEADER_DOC]') }).Count -eq 224) 'User remark affected generated coverage.'
    Assert ((Get-AuthoringFileSha256 $source) -eq $sourceSha) 'Production workbook changed during isolated test.'
    [pscustomobject]@{ action='HEADER_COMMENTS_TEST_PASSED'; tableCount=22; fieldCount=224; sourceSha256=$sourceSha; userRemarksPreserved=1; repeatAddedThreads=0 } | ConvertTo-Json -Compress
} finally {
    foreach ($file in @($first,$second,$withUser,$preserved,$fixtureSource)) { if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force } }
    if ((Resolve-Path $root).Path.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase)) { Remove-Item -LiteralPath $root }
}
