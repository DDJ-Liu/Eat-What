[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InputWorkbook,
    [Parameter(Mandatory)][string]$CandidateWorkbook,
    [Parameter(Mandatory)][string]$ExpectedInputSha256,
    [string]$DataTablesRoot,
    [string[]]$UpdateSchemaTables,
    [string[]]$InitializeSchemaTables = @()
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Authoring.Common.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not $DataTablesRoot) { $DataTablesRoot = Join-Path (Get-AuthoringRepositoryRoot) 'DataTables' }
$source = [IO.Path]::GetFullPath($InputWorkbook)
$candidate = [IO.Path]::GetFullPath($CandidateWorkbook)
$expected = $ExpectedInputSha256.ToUpperInvariant()
if ($source -eq $candidate -or (Test-Path -LiteralPath $candidate)) { throw '[HEADER_COMMENT_TARGET_EXISTS] Candidate must be new and distinct.' }
if ((Get-AuthoringFileSha256 $source) -ne $expected) { throw '[HEADER_COMMENT_SOURCE_DRIFT] Input workbook changed before comment generation.' }
$before = Get-AuthoringWorkbookContentSnapshot $source
$plan = Invoke-AuthoringNode -Command 'header-comment-plan' -Arguments @{'data-root'=[IO.Path]::GetFullPath($DataTablesRoot)}
$plan = $plan | ConvertFrom-Json -Depth 30
if ($plan.tableCount -ne 22 -or $plan.fieldCount -ne 224) { throw '[HEADER_COMMENT_PLAN_COVERAGE] Expected 22 tables and 224 fields.' }
$updateTables = if ($PSBoundParameters.ContainsKey('UpdateSchemaTables')) { @($UpdateSchemaTables) } else { @($plan.tables.tableName) }
if ($updateTables.Count -eq 0) { throw '[HEADER_COMMENT_UPDATE_SCOPE] Explicit update selection must contain at least one schema table.' }
foreach ($name in $updateTables) {
    if (@($plan.tables | Where-Object tableName -CEQ $name).Count -ne 1 -or @($updateTables | Where-Object { $_ -ceq $name }).Count -ne 1) {
        throw "[HEADER_COMMENT_UPDATE_SCOPE] Unknown or duplicate schema table: $name"
    }
}
foreach ($name in $InitializeSchemaTables) {
    if (@($plan.tables | Where-Object tableName -CEQ $name).Count -ne 1 -or @($InitializeSchemaTables | Where-Object { $_ -ceq $name }).Count -ne 1) {
        throw "[HEADER_COMMENT_INITIALIZE_SCOPE] Unknown or duplicate schema table: $name"
    }
    if ($updateTables -cnotcontains $name) { throw "[HEADER_COMMENT_INITIALIZE_SCOPE] Initialization table is outside update selection: $name" }
}
foreach ($table in $plan.tables) {
    $sheet = @($before.sheets | Where-Object { $_.name -ceq [string]$table.tableName })
    if ($sheet.Count -ne 1) { throw "[HEADER_COMMENT_SHEET] Expected one $($table.tableName) sheet." }
    foreach ($field in $table.comments) {
        $header = @($sheet[0].cells | Where-Object { $_.cell -ceq [string]$field.address })
        if ($header.Count -ne 1 -or [string]$header[0].value -cne [string]$field.header) {
            throw "[HEADER_COMMENT_HEADER_DRIFT] $($table.tableName)!$($field.address) must be '$($field.header)'."
        }
    }
}

function Read-ZipXml($zip, [string]$name) {
    $entry = $zip.GetEntry($name)
    if (-not $entry) { throw "[HEADER_COMMENT_MISSING_PART] $name" }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { return [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Write-ZipXml($zip, [string]$name, [xml]$document, [switch]$AllowNew) {
    $entry = $zip.GetEntry($name)
    if (-not $entry -and -not $AllowNew) { throw "[HEADER_COMMENT_MISSING_PART] $name" }
    if ($entry) { $entry.Delete() }
    $entry = $zip.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal)
    $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
    try { $document.Save($writer) } finally { $writer.Dispose() }
}
function Resolve-Part([string]$basePart, [string]$target) {
    $base = [IO.Path]::GetDirectoryName($basePart).Replace('\','/')
    $path = $target.Replace('\','/')
    if (-not $path.StartsWith('/')) { $path = $base + '/' + $path }
    $parts = [Collections.Generic.List[string]]::new()
    foreach ($segment in $path.Split('/')) {
        if ($segment -eq '' -or $segment -eq '.') { continue }
        if ($segment -eq '..') { if ($parts.Count -eq 0) { throw '[HEADER_COMMENT_PATH_ESCAPE]' }; $parts.RemoveAt($parts.Count - 1) }
        else { [void]$parts.Add($segment) }
    }
    return ($parts -join '/')
}
function Relationship-Target([xml]$rels, [string]$typeSuffix, [string]$id) {
    foreach ($node in $rels.DocumentElement.ChildNodes) {
        if ($id -and $node.GetAttribute('Id') -ne $id) { continue }
        if ([string]$node.GetAttribute('Type') -like ('*' + $typeSuffix)) { return [string]$node.GetAttribute('Target') }
    }
    throw "[HEADER_COMMENT_RELATIONSHIP] $typeSuffix $id"
}
function Is-ToolText($node, $field) {
    if (-not $script:toolPersonIds.Contains([string]$node.GetAttribute('personId'))) { return $false }
    $text = [string]$node.InnerText
    return $text -ceq [string]$field.text -or @($field.legacyToolTexts) -ccontains $text
}
function Is-ToolLegacyCompanion([string]$legacyText, [string]$threadText) {
    $wrapper = @(
        '[Threaded comment]', '',
        'Your version of Excel allows you to read this threaded comment; however, any edits to it will get removed if the file is opened in a newer version of Excel. Learn more: https://go.microsoft.com/fwlink/?linkid=870924',
        '', 'Comment:', '    '
    ) -join [char]10
    return $legacyText -ceq $threadText -or $legacyText -ceq ($wrapper + $threadText)
}
function Get-CommentInventory([string]$path) {
    $items = [Collections.Generic.List[string]]::new()
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        foreach ($entry in $archive.Entries) {
            $part = $entry.FullName
            if ($part -notmatch '^xl/(threadedcomments/[^/]+[.]xml|comments[^/]*[.]xml|persons/[^/]+[.]xml)$') { continue }
            $document = Read-ZipXml $archive $part
            [void]$items.Add($part + '|PART')
            if ($part -like 'xl/threadedcomments/*') {
                foreach ($node in $document.DocumentElement.ChildNodes) { [void]$items.Add($part + '|THREAD|' + $node.OuterXml) }
            } elseif ($part -like 'xl/comments*') {
                foreach ($node in $document.SelectNodes('//*[local-name()="authors"]/*[local-name()="author"]')) {
                    [void]$items.Add($part + '|AUTHOR|' + $node.OuterXml)
                }
                foreach ($node in $document.SelectNodes('//*[local-name()="commentList"]/*[local-name()="comment"]')) {
                    [void]$items.Add($part + '|LEGACY|' + $node.OuterXml)
                }
            } else {
                foreach ($node in $document.DocumentElement.ChildNodes) { [void]$items.Add($part + '|PERSON|' + $node.OuterXml) }
            }
        }
    } finally { $archive.Dispose() }
    return $items.ToArray()
}
function Assert-CommentProtection([string]$sourcePath, [string]$candidatePath, $allowedBefore, $allowedAfter) {
    $beforeItems = [Collections.Generic.List[string]]::new()
    $afterItems = [Collections.Generic.List[string]]::new()
    foreach ($item in @(Get-CommentInventory $sourcePath)) { [void]$beforeItems.Add($item) }
    foreach ($item in @(Get-CommentInventory $candidatePath)) { [void]$afterItems.Add($item) }
    foreach ($item in $allowedBefore) {
        if (-not $beforeItems.Remove($item)) { throw '[HEADER_COMMENT_AUDIT] Declared source mutation was not present.' }
    }
    foreach ($item in $allowedAfter) {
        if (-not $afterItems.Remove($item)) { throw '[HEADER_COMMENT_AUDIT] Declared candidate mutation was not present.' }
    }
    $counts = [Collections.Generic.Dictionary[string,int]]::new([StringComparer]::Ordinal)
    foreach ($item in $beforeItems) {
        if (-not $counts.ContainsKey($item)) { $counts[$item] = 0 }
        $counts[$item]++
    }
    foreach ($item in $afterItems) {
        if (-not $counts.ContainsKey($item)) { throw '[HEADER_COMMENT_AUDIT] Candidate added an undeclared remark.' }
        $counts[$item]--
    }
    if (@($counts.Values | Where-Object { $_ -ne 0 }).Count -gt 0) {
        throw '[HEADER_COMMENT_AUDIT] Unknown remark, author, thread, reply or legacy relation changed.'
    }
    return [pscustomobject]@{ protectedNodes=$beforeItems.Count; declaredBefore=$allowedBefore.Count; declaredAfter=$allowedAfter.Count }
}

[IO.Directory]::CreateDirectory((Split-Path -Parent $candidate)) | Out-Null
[IO.File]::Copy($source, $candidate, $false)
$updated = 0
$removed = 0
$preserved = 0
$changed = $false
$allowedBefore = [Collections.Generic.List[string]]::new()
$allowedAfter = [Collections.Generic.List[string]]::new()
$initialized = 0
try {
    $zip = [IO.Compression.ZipFile]::Open($candidate, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $book = Read-ZipXml $zip 'xl/workbook.xml'
        $bookRels = Read-ZipXml $zip 'xl/_rels/workbook.xml.rels'
        $contentTypes = Read-ZipXml $zip '[Content_Types].xml'
        $packageChanged = $false
        function Add-CommentContentType([string]$part, [string]$type) {
            $node = $contentTypes.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('PartName') -ceq ('/' + $part) }
            if ($node) { if ($node.GetAttribute('ContentType') -cne $type) { throw "[HEADER_COMMENT_CONTENT_TYPE] $part" }; return }
            $node = $contentTypes.CreateElement('Override', $contentTypes.DocumentElement.NamespaceURI)
            $node.SetAttribute('PartName', '/' + $part); $node.SetAttribute('ContentType', $type)
            [void]$contentTypes.DocumentElement.AppendChild($node)
        }
        function Add-CommentRelationship([xml]$document, [string]$type, [string]$target) {
            $node = $document.CreateElement('Relationship', $document.DocumentElement.NamespaceURI)
            $node.SetAttribute('Id', 'AI189_' + [guid]::NewGuid().ToString('N'))
            $node.SetAttribute('Type', $type); $node.SetAttribute('Target', $target)
            [void]$document.DocumentElement.AppendChild($node)
        }
        $threadNs = 'http://schemas.microsoft.com/office/spreadsheetml/2018/threadedcomments'
        $mainNs = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
        $personsNew = $null -eq $zip.GetEntry('xl/persons/person.xml')
        if ($personsNew -and $InitializeSchemaTables.Count -gt 0) {
            [xml]$persons = '<xltc:personList xmlns:xltc="' + $threadNs + '"/>'
            [void]$allowedAfter.Add('xl/persons/person.xml|PART')
            Add-CommentContentType 'xl/persons/person.xml' 'application/vnd.ms-excel.person+xml'
            Add-CommentRelationship $bookRels 'http://schemas.microsoft.com/office/2017/10/relationships/person' 'persons/person.xml'
            $packageChanged = $true
        } else { $persons = Read-ZipXml $zip 'xl/persons/person.xml' }
        $script:toolPersonIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($person in $persons.DocumentElement.ChildNodes) {
            if ($person.GetAttribute('displayName') -eq 'Eat-What Data Authoring') {
                [void]$script:toolPersonIds.Add([string]$person.GetAttribute('id'))
            }
        }
        if ($script:toolPersonIds.Count -eq 0) {
            if ($InitializeSchemaTables.Count -eq 0) { throw '[HEADER_COMMENT_TOOL_PERSON] Known author not found; preserve comments for manual review.' }
            $person = $persons.CreateElement('xltc', 'person', $threadNs)
            $person.SetAttribute('displayName', 'Eat-What Data Authoring'); $person.SetAttribute('id', '{' + [guid]::NewGuid().ToString().ToUpperInvariant() + '}')
            [void]$persons.DocumentElement.AppendChild($person)
            [void]$script:toolPersonIds.Add($person.GetAttribute('id'))
            [void]$allowedAfter.Add('xl/persons/person.xml|PERSON|' + $person.OuterXml)
            Write-ZipXml $zip 'xl/persons/person.xml' $persons -AllowNew
        }
        $toolPersonId = @($script:toolPersonIds)[0]
        $sheetParts = @{}
        foreach ($sheet in $book.SelectNodes('//*[local-name()="sheets"]/*[local-name()="sheet"]')) {
            $id = $sheet.GetAttribute('id','http://schemas.openxmlformats.org/officeDocument/2006/relationships')
            $target = Relationship-Target $bookRels '/worksheet' $id
            $sheetParts[[string]$sheet.GetAttribute('name')] = Resolve-Part 'xl/workbook.xml' $target
        }
        foreach ($table in $plan.tables) {
            if ($updateTables -cnotcontains [string]$table.tableName) { continue }
            $canInitialize = $InitializeSchemaTables -ccontains [string]$table.tableName
            $sheetPart = $sheetParts[[string]$table.tableName]
            if (-not $sheetPart) { throw "[HEADER_COMMENT_SHEET] $($table.tableName)" }
            $folder = [IO.Path]::GetDirectoryName($sheetPart).Replace('\','/')
            $filename = [IO.Path]::GetFileName($sheetPart)
            $relsPart = $folder + '/_rels/' + $filename + '.rels'
            $relsNew = $null -eq $zip.GetEntry($relsPart)
            if ($relsNew -and $canInitialize) { [xml]$sheetRels = '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>' }
            else { $sheetRels = Read-ZipXml $zip $relsPart }
            $sheetPackageChanged = $relsNew
            foreach ($commentKind in @('comments','threadedComment')) {
                $relationship = @($sheetRels.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('Type').EndsWith('/' + $commentKind, [StringComparison]::Ordinal) })
                if ($relationship.Count -eq 0 -and $canInitialize) {
                    $part = if ($commentKind -eq 'comments') { 'xl/comments_AI189_' + [guid]::NewGuid().ToString('N') + '.xml' } else { 'xl/threadedcomments/AI189_' + [guid]::NewGuid().ToString('N') + '.xml' }
                    if ($commentKind -eq 'comments') {
                        [xml]$document = '<comments xmlns="' + $mainNs + '"><authors/><commentList/></comments>'
                        $type = 'application/vnd.openxmlformats-officedocument.spreadsheetml.comments+xml'
                        $relType = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships/comments'
                    } else {
                        [xml]$document = '<xltc:ThreadedComments xmlns:xltc="' + $threadNs + '"/>'
                        $type = 'application/vnd.ms-excel.threadedcomments+xml'
                        $relType = 'http://schemas.microsoft.com/office/2017/10/relationships/threadedComment'
                    }
                    Write-ZipXml $zip $part $document -AllowNew
                    [void]$allowedAfter.Add($part + '|PART')
                    Add-CommentContentType $part $type
                    Add-CommentRelationship $sheetRels $relType ('../' + $part.Substring(3))
                    $sheetPackageChanged = $true
                }
            }
            if ($sheetPackageChanged) { Write-ZipXml $zip $relsPart $sheetRels -AllowNew; $packageChanged = $true }
            $legacyPart = Resolve-Part $sheetPart (Relationship-Target $sheetRels '/comments' '')
            $threadPart = Resolve-Part $sheetPart (Relationship-Target $sheetRels '/threadedComment' '')
            $legacy = Read-ZipXml $zip $legacyPart
            $threads = Read-ZipXml $zip $threadPart
            $authors = @($legacy.SelectNodes('//*[local-name()="authors"]/*[local-name()="author"]'))
            $legacyList = $legacy.SelectSingleNode('//*[local-name()="commentList"]')
            $tableChanged = $false
            foreach ($field in $table.comments) {
                $address = [string]$field.address
                $newText = [string]$field.text
                $atAddress = @($threads.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('ref') -eq $address -and -not $_.HasAttribute('parentId') })
                $owned = @($atAddress | Where-Object { Is-ToolText $_ $field })
                $preserved += $atAddress.Count - $owned.Count
                if ($owned.Count -eq 0) {
                    $legacyAtAddress = @($legacyList.ChildNodes | Where-Object { $_.GetAttribute('ref') -eq $address })
                    if (-not $canInitialize -or $atAddress.Count -gt 0 -or $legacyAtAddress.Count -gt 0) { throw "[HEADER_COMMENT_NO_TOOL_THREAD] $($table.tableName)!$address; user comment preserved, manual review needed." }
                    $id = '{' + [guid]::NewGuid().ToString().ToUpperInvariant() + '}'
                    $thread = $threads.CreateElement('xltc', 'threadedComment', $threadNs)
                    $thread.SetAttribute('ref', $address); $thread.SetAttribute('id', $id); $thread.SetAttribute('personId', $toolPersonId); $thread.SetAttribute('dT', [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ss.fffZ'))
                    $text = $threads.CreateElement('xltc', 'text', $threadNs); $text.InnerText = $newText; [void]$thread.AppendChild($text); [void]$threads.DocumentElement.AppendChild($thread)
                    [void]$allowedAfter.Add($threadPart + '|THREAD|' + $thread.OuterXml)
                    $author = $legacy.CreateElement('author', $mainNs); $author.InnerText = 'tc=' + $id
                    [void]$legacy.SelectSingleNode('//*[local-name()="authors"]').AppendChild($author)
                    [void]$allowedAfter.Add($legacyPart + '|AUTHOR|' + $author.OuterXml)
                    $comment = $legacy.CreateElement('comment', $mainNs); $comment.SetAttribute('ref', $address); $comment.SetAttribute('authorId', [string]$authors.Count)
                    $text = $legacy.CreateElement('text', $mainNs); $t = $legacy.CreateElement('t', $mainNs); $t.InnerText = $newText
                    [void]$text.AppendChild($t); [void]$comment.AppendChild($text); [void]$legacyList.AppendChild($comment)
                    [void]$allowedAfter.Add($legacyPart + '|LEGACY|' + $comment.OuterXml)
                    $authors = @($legacy.SelectNodes('//*[local-name()="authors"]/*[local-name()="author"]'))
                    $initialized++; $tableChanged = $true
                    continue
                }
                $primary = $owned[0]
                $primaryId = [string]$primary.GetAttribute('id')
                $primaryText = $primary.SelectSingleNode('./*[local-name()="text"]')
                if (-not $primaryText) { throw "[HEADER_COMMENT_TEXT] $($table.tableName)!$address" }
                $primaryOldText = [string]$primaryText.InnerText
                $primaryLegacy = @($legacyList.ChildNodes | Where-Object {
                    if ($_.GetAttribute('ref') -ne $address) { return $false }
                    $index = [int]$_.GetAttribute('authorId')
                    return $index -lt $authors.Count -and [string]$authors[$index].InnerText -ceq ('tc=' + $primaryId)
                })
                if (@($primaryLegacy | Where-Object { -not (Is-ToolLegacyCompanion ([string]$_.InnerText) $primaryOldText) }).Count -gt 0) {
                    throw "[HEADER_COMMENT_LEGACY_UNKNOWN] $($table.tableName)!$address has an edited legacy companion; manual review needed."
                }
                if ($primaryText.InnerText -ne $newText) {
                    [void]$allowedBefore.Add($threadPart + '|THREAD|' + $primary.OuterXml)
                    $primaryText.InnerText = $newText
                    $primary.SetAttribute('dT', [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"))
                    [void]$allowedAfter.Add($threadPart + '|THREAD|' + $primary.OuterXml)
                    $tableChanged = $true
                    $updated++
                }
                $removedIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
                foreach ($secondary in @($owned | Select-Object -Skip 1)) {
                    $secondaryId = [string]$secondary.GetAttribute('id')
                    $secondaryLegacy = @($legacyList.ChildNodes | Where-Object {
                        if ($_.GetAttribute('ref') -ne $address) { return $false }
                        $index = [int]$_.GetAttribute('authorId')
                        return $index -lt $authors.Count -and [string]$authors[$index].InnerText -ceq ('tc=' + $secondaryId)
                    })
                    if (@($threads.DocumentElement.ChildNodes | Where-Object { $_.GetAttribute('parentId') -eq $secondaryId }).Count -gt 0 -or
                        @($secondaryLegacy | Where-Object { -not (Is-ToolLegacyCompanion ([string]$_.InnerText) ([string]$secondary.InnerText)) }).Count -gt 0) {
                        $preserved++
                        continue
                    }
                    [void]$allowedBefore.Add($threadPart + '|THREAD|' + $secondary.OuterXml)
                    [void]$threads.DocumentElement.RemoveChild($secondary)
                    [void]$removedIds.Add($secondaryId)
                    $tableChanged = $true
                    $removed++
                }
                foreach ($old in @($legacyList.ChildNodes | Where-Object { $_.GetAttribute('ref') -eq $address })) {
                    $authorIndex = [int]$old.GetAttribute('authorId')
                    if ($authorIndex -ge $authors.Count) { throw "[HEADER_COMMENT_AUTHOR] $($table.tableName)!$address" }
                    $author = [string]$authors[$authorIndex].InnerText
                    $id = if ($author -match '^tc=(.+)$') { $Matches[1] } else { '' }
                    if ($id -eq $primaryId) {
                        $oldText = $old.SelectSingleNode('./*[local-name()="text"]')
                        if ($oldText.InnerText -ne $newText) {
                            [void]$allowedBefore.Add($legacyPart + '|LEGACY|' + $old.OuterXml)
                            $oldText.RemoveAll()
                            $t = $legacy.CreateElement('t', $legacy.DocumentElement.NamespaceURI)
                            $t.InnerText = $newText
                            [void]$oldText.AppendChild($t)
                            [void]$allowedAfter.Add($legacyPart + '|LEGACY|' + $old.OuterXml)
                            $tableChanged = $true
                        }
                    } elseif ($id -and $removedIds.Contains($id)) {
                        [void]$allowedBefore.Add($legacyPart + '|LEGACY|' + $old.OuterXml)
                        [void]$legacyList.RemoveChild($old)
                        $tableChanged = $true
                    }
                }
            }
            if ($tableChanged) {
                Write-ZipXml $zip $threadPart $threads
                Write-ZipXml $zip $legacyPart $legacy
                $changed = $true
            }
        }
        if ($packageChanged) { Write-ZipXml $zip 'xl/_rels/workbook.xml.rels' $bookRels; Write-ZipXml $zip '[Content_Types].xml' $contentTypes }
    } finally { $zip.Dispose() }
    if ((Get-AuthoringFileSha256 $source) -ne $expected) { throw '[HEADER_COMMENT_SOURCE_DRIFT] Input workbook changed during comment generation.' }
    $after = Get-AuthoringWorkbookContentSnapshot $candidate
    $content = Compare-AuthoringWorkbookContent $before $after
    $structure = Compare-AuthoringWorkbookStructure $before $after
    if (-not $content.isEqual -or -not $structure.isEqual) { throw "[HEADER_COMMENT_CONTENT_DRIFT] content=$($content.differenceCount) structure=$($structure.differenceCount)" }
    $audit = Assert-CommentProtection $source $candidate $allowedBefore $allowedAfter
    [pscustomobject]@{
        action='HEADER_COMMENT_CANDIDATE'; source=$source; candidate=$candidate; sourceSha256=$expected
        candidateSha256=(Get-AuthoringFileSha256 $candidate); tableCount=$plan.tableCount; fieldCount=$plan.fieldCount
        updateTableCount=$updateTables.Count; updateTables=@($updateTables)
        updatedThreads=$updated; removedToolThreads=$removed; preservedUserThreads=$preserved
        initializedThreads=$initialized
        changed=$changed; contentDifferenceCount=0; structureDifferenceCount=0; commentProtectionAudit=$audit
    }
} catch {
    if (Test-Path -LiteralPath $candidate) { Remove-Item -LiteralPath $candidate -Force }
    throw
}
