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
try {
    $plan = (Invoke-AuthoringNode -Command 'header-comment-plan' -Arguments @{'data-root'=(Resolve-Path (Join-Path $PSScriptRoot '../../DataTables')).Path}) | ConvertFrom-Json -Depth 30
    Assert ($plan.tableCount -eq 14 -and $plan.fieldCount -eq 119) 'Schema coverage must be 14 tables / 119 fields.'
    $expectedTexts = @($plan.tables | ForEach-Object { $_.comments } | ForEach-Object { $_.text })
    Assert (@($expectedTexts | Select-Object -Unique).Count -eq 119) 'Every field must have a distinct table-aware explanation.'
    $steps = @($plan.tables | Where-Object tableName -eq 'RecipeSteps')[0]
    $ingredients = @($steps.comments | Where-Object field -eq 'standard_ingredients')[0].text
    Assert ($ingredients.Contains('ing_') -and $ingredients.Contains('prd_') -and $ingredients.Contains("多值分隔符：';'") -and $ingredients.Contains('Items.item_id')) 'Mixed Ingredients and semicolon contract missing.'
    $areas = @($plan.tables | Where-Object tableName -eq 'KitchenAreas')[0]
    $preset = @($areas.comments | Where-Object field -eq 'preset_tools')[0].text
    Assert ($preset.Contains('已弃用') -and $preset.Contains("多值分隔符：'|'")) 'Legacy preset_tools contract missing.'

    $result = & (Join-Path $PSScriptRoot 'Authoring/Update-HeaderComments.ps1') -InputWorkbook $source -CandidateWorkbook $first -ExpectedInputSha256 $sourceSha
    Assert ($result.fieldCount -eq 119 -and $result.contentDifferenceCount -eq 0 -and $result.structureDifferenceCount -eq 0) 'Candidate altered workbook content or structure.'
    $texts = @(Get-ThreadTexts $first)
    Assert (@($texts | Where-Object { $_.StartsWith('[AI189_HEADER_DOC]') }).Count -eq 119) 'Exactly one generated explanation per schema header is required.'
    Assert (@($texts | Where-Object { $_.StartsWith('Type:') -or $_.StartsWith('Reference:') }).Count -eq 0) 'Old tool-generated warning threads must be merged.'
    foreach ($text in $expectedTexts) { Assert ($texts -ccontains $text) 'Generated workbook omitted an expected field explanation.' }

    $repeat = & (Join-Path $PSScriptRoot 'Authoring/Update-HeaderComments.ps1') -InputWorkbook $first -CandidateWorkbook $second -ExpectedInputSha256 (Get-AuthoringFileSha256 $first)
    Assert ($repeat.updatedThreads -eq 0 -and $repeat.removedToolThreads -eq 0) 'Repeated execution must not add or replace threads.'
    Assert (@(Get-ThreadTexts $second | Where-Object { $_.StartsWith('[AI189_HEADER_DOC]') }).Count -eq 119) 'Repeated execution stacked generated threads.'

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
    Assert (@(Get-ThreadTexts $preserved | Where-Object { $_.StartsWith('[AI189_HEADER_DOC]') }).Count -eq 119) 'User remark affected generated coverage.'
    Assert ((Get-AuthoringFileSha256 $source) -eq $sourceSha) 'Production workbook changed during isolated test.'
    [pscustomobject]@{ action='HEADER_COMMENTS_TEST_PASSED'; tableCount=14; fieldCount=119; sourceSha256=$sourceSha; userRemarksPreserved=1; repeatAddedThreads=0 } | ConvertTo-Json -Compress
} finally {
    foreach ($file in @($first,$second,$withUser,$preserved)) { if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force } }
    if ((Resolve-Path $root).Path.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase)) { Remove-Item -LiteralPath $root }
}
