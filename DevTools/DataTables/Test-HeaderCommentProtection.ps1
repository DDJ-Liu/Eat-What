[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Authoring/Authoring.Common.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Assert([bool]$condition, [string]$message) {
    if (-not $condition) { throw "[HEADER_PROTECTION_TEST] $message" }
}
function Read-Part($zip, [string]$name) {
    $entry = $zip.GetEntry($name)
    if (-not $entry) { throw "[HEADER_PROTECTION_PART] $name" }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { return [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Write-Part($zip, [string]$name, [xml]$document) {
    $entry = $zip.GetEntry($name)
    if (-not $entry) { throw "[HEADER_PROTECTION_PART] $name" }
    $entry.Delete()
    $writer = [IO.StreamWriter]::new($zip.CreateEntry($name).Open(), [Text.UTF8Encoding]::new($false))
    try { $document.Save($writer) } finally { $writer.Dispose() }
}
function Add-Person($zip, [string]$id) {
    $part = 'xl/persons/person.xml'
    $document = Read-Part $zip $part
    $person = $document.CreateElement('person', $document.DocumentElement.NamespaceURI)
    $person.SetAttribute('id', $id)
    $person.SetAttribute('displayName', 'Human Protection Fixture')
    [void]$document.DocumentElement.AppendChild($person)
    Write-Part $zip $part $document
}
function Add-Thread($zip, [string]$part, [string]$reference, [string]$id, [string]$personId, [string]$body, [string]$parentId = '') {
    $document = Read-Part $zip $part
    $node = $document.CreateElement('threadedComment', $document.DocumentElement.NamespaceURI)
    $node.SetAttribute('ref', $reference)
    $node.SetAttribute('id', $id)
    $node.SetAttribute('personId', $personId)
    if ($parentId) { $node.SetAttribute('parentId', $parentId) }
    $text = $document.CreateElement('text', $document.DocumentElement.NamespaceURI)
    $text.InnerText = $body
    [void]$node.AppendChild($text)
    [void]$document.DocumentElement.AppendChild($node)
    Write-Part $zip $part $document
}
function Add-OrphanLegacy($zip, [string]$marker) {
    $part = 'xl/comments1.xml'
    $document = Read-Part $zip $part
    $authors = $document.SelectSingleNode('//*[local-name()="authors"]')
    $list = $document.SelectSingleNode('//*[local-name()="commentList"]')
    $author = $document.CreateElement('author', $document.DocumentElement.NamespaceURI)
    $author.InnerText = 'tc={D6116A0D-7531-4BC4-AB0F-4682C1178197}'
    $index = $authors.ChildNodes.Count
    [void]$authors.AppendChild($author)
    $note = $document.CreateElement('comment', $document.DocumentElement.NamespaceURI)
    $note.SetAttribute('ref', 'A1')
    $note.SetAttribute('authorId', [string]$index)
    $body = $document.CreateElement('text', $document.DocumentElement.NamespaceURI)
    $t = $document.CreateElement('t', $document.DocumentElement.NamespaceURI)
    $t.InnerText = $marker
    [void]$body.AppendChild($t)
    [void]$note.AppendChild($body)
    [void]$list.AppendChild($note)
    Write-Part $zip $part $document
}
function Copy-And-Edit([string]$source, [string]$target, [scriptblock]$edit) {
    [IO.File]::Copy($source, $target, $false)
    $zip = [IO.Compression.ZipFile]::Open($target, [IO.Compression.ZipArchiveMode]::Update)
    try { & $edit $zip } finally { $zip.Dispose() }
}
function Run-Candidate([string]$source, [string]$target) {
    return & (Join-Path $PSScriptRoot 'Authoring/Update-HeaderComments.ps1') -InputWorkbook $source -CandidateWorkbook $target -ExpectedInputSha256 (Get-AuthoringFileSha256 $source)
}
function Assert-Refused([string]$source, [string]$target) {
    $sha = Get-AuthoringFileSha256 $source
    $refused = $false
    try { $null = Run-Candidate $source $target }
    catch {
        $refused = $true
        Assert ($_.Exception.Message -match '^\[HEADER_COMMENT_(NO_TOOL_THREAD|LEGACY_UNKNOWN|AUDIT)\]') 'Uncertain ownership must fail with a specific protection error.'
    }
    Assert $refused 'Uncertain ownership incorrectly returned a candidate.'
    Assert (-not (Test-Path -LiteralPath $target)) 'Refused candidate was left behind.'
    Assert ((Get-AuthoringFileSha256 $source) -eq $sha) 'Refusal changed its source.'
}
function Get-CommentXmlText([string]$path) {
    $zip = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        return [string]::Join([char]10, @(
            foreach ($entry in $zip.Entries) {
                if ($entry.FullName -match '^xl/(threadedcomments/[^/]+[.]xml|comments[0-9]+[.]xml)$') {
                    $reader = [IO.StreamReader]::new($entry.Open())
                    try { $reader.ReadToEnd() } finally { $reader.Dispose() }
                }
            }
        ))
    } finally { $zip.Dispose() }
}

$production = (Resolve-Path (Join-Path $PSScriptRoot 'Authoring/DataTables_Authoring.xlsx')).Path
$productionSha = Get-AuthoringFileSha256 $production
$root = Join-Path ([IO.Path]::GetTempPath()) ('AI193-header-protection-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$cases = [Collections.Generic.List[string]]::new()
try {
    $controlSource = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../.ai-workspace/outputs/control/U1-R1-20260924/heartbeat-20261004-0248/comment-preservation-probe-v3/source-with-unknown-notes.xlsx'))
    if (Test-Path -LiteralPath $controlSource) {
        Assert-Refused $controlSource (Join-Path $root 'control-candidate.xlsx')
        [void]$cases.Add('controller-public-reproduction-refused')
    }

    $editedType = Join-Path $root 'edited-type.xlsx'
    Copy-And-Edit $production $editedType {
        param($zip)
        $part = 'xl/threadedcomments/threadedcomment.xml'
        $document = Read-Part $zip $part
        $node = @($document.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('ref') -eq 'A1' -and -not $_.HasAttribute('parentId') })[0]
        $node.SelectSingleNode('./*[local-name()="text"]').InnerText += ([char]10 + 'SAME_AUTHOR_UNKNOWN_TYPE_NOTE')
        Write-Part $zip $part $document
    }
    Assert-Refused $editedType (Join-Path $root 'edited-type-candidate.xlsx')
    [void]$cases.Add('same-author-type-append-refused')

    $orphan = Join-Path $root 'orphan.xlsx'
    Copy-And-Edit $production $orphan { param($zip) Add-OrphanLegacy $zip 'UNKNOWN_ORPHAN_LEGACY_MUST_SURVIVE' }
    $orphanCandidate = Join-Path $root 'orphan-candidate.xlsx'
    $orphanResult = Run-Candidate $orphan $orphanCandidate
    Assert ($orphanResult.fieldCount -eq 119 -and $orphanResult.commentProtectionAudit.protectedNodes -gt 0) 'Orphan fixture lost the independent audit.'
    Assert ((Get-CommentXmlText $orphanCandidate).Contains('UNKNOWN_ORPHAN_LEGACY_MUST_SURVIVE')) 'Unrelated tc= legacy note was removed.'
    [void]$cases.Add('orphan-legacy-preserved')

    $first = Join-Path $root 'first.xlsx'
    $firstResult = Run-Candidate $production $first
    Assert ($firstResult.fieldCount -eq 119 -and $firstResult.removedToolThreads -eq 50) 'Original tool comments were not merged.'
    $repeat = Join-Path $root 'repeat.xlsx'
    $repeatResult = Run-Candidate $first $repeat
    Assert ($repeatResult.updatedThreads -eq 0 -and $repeatResult.removedToolThreads -eq 0) 'Second run stacked or rewrote tool threads.'
    [void]$cases.Add('original-and-idempotent')

    $personId = '{5ED12451-3F3B-4D24-8D58-982B64EF08F1}'
    $user = Join-Path $root 'user-replies.xlsx'
    Copy-And-Edit $first $user {
        param($zip)
        Add-Person $zip $personId
        $part = 'xl/threadedcomments/threadedcomment.xml'
        $document = Read-Part $zip $part
        $primary = @($document.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('ref') -eq 'A1' -and -not $_.HasAttribute('parentId') })[0]
        Add-Thread $zip $part 'A1' '{6FC8D1A5-213C-470B-8B79-8D70E5DF32EF}' $personId 'Type: HUMAN_NOTE_MUST_SURVIVE'
        Add-Thread $zip $part 'A1' '{D899AB85-FBDA-42E5-A361-ED1CB182458E}' $personId 'PRIMARY_REPLY_MUST_SURVIVE' $primary.GetAttribute('id')
        Add-Thread $zip $part 'Z1' '{32871F80-4B9C-47D4-A4B9-B30008870268}' $personId 'OUTSIDE_SCHEMA_COLUMN_MUST_SURVIVE'
    }
    $userCandidate = Join-Path $root 'user-candidate.xlsx'
    $userResult = Run-Candidate $user $userCandidate
    $userXml = Get-CommentXmlText $userCandidate
    foreach ($marker in @('Type: HUMAN_NOTE_MUST_SURVIVE','PRIMARY_REPLY_MUST_SURVIVE','OUTSIDE_SCHEMA_COLUMN_MUST_SURVIVE')) {
        Assert ($userXml.Contains($marker)) "Protected user/root/reply/outside-schema comment missing: $marker"
    }
    Assert ($userResult.commentProtectionAudit.protectedNodes -gt 0) 'User comments did not participate in protection audit.'
    [void]$cases.Add('other-author-primary-reply-and-extra-column-preserved')

    $secondary = Join-Path $root 'secondary-reply.xlsx'
    Copy-And-Edit $production $secondary {
        param($zip)
        Add-Person $zip $personId
        $part = @($zip.Entries | Where-Object { $_.FullName -match '^xl/threadedcomments/[^/]+[.]xml$' } | ForEach-Object {
            $entryName = $_.FullName
            $document = Read-Part $zip $entryName
            $reference = @($document.DocumentElement.ChildNodes | Where-Object { $_.InnerText.StartsWith('Reference:') -and -not $_.HasAttribute('parentId') })[0]
            if ($reference) { [pscustomobject]@{part=$entryName; id=$reference.GetAttribute('id'); address=$reference.GetAttribute('ref')} }
        })[0]
        if (-not $part) { throw '[HEADER_PROTECTION_FIXTURE] No secondary Reference thread.' }
        Add-Thread $zip $part.part $part.address '{892EF46B-7CF6-4F86-9F1D-9505830BD891}' $personId 'SECONDARY_REPLY_MUST_SURVIVE' $part.id
    }
    $secondaryCandidate = Join-Path $root 'secondary-candidate.xlsx'
    $secondaryResult = Run-Candidate $secondary $secondaryCandidate
    Assert ((Get-CommentXmlText $secondaryCandidate).Contains('SECONDARY_REPLY_MUST_SURVIVE')) 'Secondary reply or parent was removed.'
    Assert ($secondaryResult.removedToolThreads -eq 49) 'Secondary with reply must not be merged away.'
    [void]$cases.Add('secondary-reply-preserved')

    $editedReference = Join-Path $root 'edited-reference.xlsx'
    Copy-And-Edit $production $editedReference {
        param($zip)
        $part = @($zip.Entries | Where-Object { $_.FullName -match '^xl/threadedcomments/[^/]+[.]xml$' } | ForEach-Object {
            $entryName = $_.FullName
            $document = Read-Part $zip $entryName
            $reference = @($document.DocumentElement.ChildNodes | Where-Object { $_.InnerText.StartsWith('Reference:') -and -not $_.HasAttribute('parentId') })[0]
            if ($reference) { [pscustomobject]@{part=$entryName; id=$reference.GetAttribute('id')} }
        })[0]
        $document = Read-Part $zip $part.part
        $reference = @($document.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('id') -eq $part.id })[0]
        $reference.SelectSingleNode('./*[local-name()="text"]').InnerText += ([char]10 + 'SAME_AUTHOR_UNKNOWN_REFERENCE_NOTE')
        Write-Part $zip $part.part $document
    }
    $editedReferenceCandidate = Join-Path $root 'edited-reference-candidate.xlsx'
    $editedReferenceResult = Run-Candidate $editedReference $editedReferenceCandidate
    Assert ((Get-CommentXmlText $editedReferenceCandidate).Contains('SAME_AUTHOR_UNKNOWN_REFERENCE_NOTE')) 'Same-author unknown Reference tail was lost.'
    Assert ($editedReferenceResult.removedToolThreads -eq 49) 'Unknown secondary must not be merged away.'
    [void]$cases.Add('same-author-reference-append-preserved')

    $editedDoc = Join-Path $root 'edited-ai189.xlsx'
    Copy-And-Edit $first $editedDoc {
        param($zip)
        $part = 'xl/threadedcomments/threadedcomment.xml'
        $document = Read-Part $zip $part
        $node = @($document.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('ref') -eq 'A1' -and -not $_.HasAttribute('parentId') })[0]
        $node.SelectSingleNode('./*[local-name()="text"]').InnerText += ([char]10 + 'AI189_UNKNOWN_ADDITION_MUST_SURVIVE')
        Write-Part $zip $part $document
    }
    Assert-Refused $editedDoc (Join-Path $root 'edited-ai189-candidate.xlsx')
    [void]$cases.Add('ai189-prefix-append-refused')

    Assert ((Get-AuthoringFileSha256 $production) -eq $productionSha) 'Production workbook changed.'
    [pscustomobject]@{
        action='HEADER_PROTECTION_TEST_PASSED'; cases=$cases.ToArray()
        sourceSha256=$productionSha; candidateSha256=(Get-AuthoringFileSha256 $first)
        audit=$firstResult.commentProtectionAudit; productionUnchanged=$true
    } | ConvertTo-Json -Depth 5 -Compress
} finally {
    if ((Test-Path -LiteralPath $root) -and $root.StartsWith([IO.Path]::GetTempPath(), [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }
}
