[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$authoringRoot = Join-Path $PSScriptRoot 'Authoring'
. (Join-Path $authoringRoot 'Authoring.Common.ps1')
$repositoryRoot = Get-AuthoringRepositoryRoot
$sourceRoot = Join-Path $repositoryRoot 'DataTables'

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Get-CsvHashes {
    param([string]$Root)
    $result = [ordered]@{}
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -Recurse -File -Filter '*.csv' | Sort-Object FullName)) {
        $relative = $file.FullName.Substring([System.IO.Path]::GetFullPath($Root).TrimEnd('\').Length).TrimStart('\').Replace('\', '/')
        $result[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    }
    return $result
}

function Assert-HashMapsEqual {
    param($Expected, $Actual, [string]$Message)
    Assert-True ($Expected.Count -eq $Actual.Count) "$Message (file count)"
    foreach ($key in $Expected.Keys) {
        Assert-True ($Actual.Contains($key)) "$Message (missing $key)"
        Assert-True ($Expected[$key] -eq $Actual[$key]) "$Message (changed $key)"
    }
}

function Write-Utf8Fixture {
    param([string]$Path, [string]$Text)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Text, [System.Text.UTF8Encoding]::new($false))
}

function Get-NodeJson {
    param([string]$Command, [hashtable]$Arguments)
    $lines = @(Invoke-AuthoringNode -Command $Command -Arguments $Arguments)
    $jsonLine = @($lines | Where-Object { $_ -match '^\{' } | Select-Object -Last 1)
    Assert-True ($jsonLine.Count -eq 1) "Command '$Command' did not return JSON."
    return ($jsonLine[0] | ConvertFrom-Json)
}

function Read-ZipText {
    param([string]$ZipPath, [string]$EntryPattern)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($ZipPath)
    try {
        $builder = [System.Text.StringBuilder]::new()
        foreach ($entry in @($archive.Entries | Where-Object { $_.FullName -like $EntryPattern } | Sort-Object FullName)) {
            $reader = [System.IO.StreamReader]::new($entry.Open(), [System.Text.Encoding]::UTF8, $true)
            try { [void]$builder.AppendLine($reader.ReadToEnd()) }
            finally { $reader.Dispose() }
        }
        return $builder.ToString()
    }
    finally {
        $archive.Dispose()
    }
}

function Add-ZipTextEntry {
    param(
        [System.IO.Compression.ZipArchive]$Archive,
        [string]$Name,
        [string]$Text
    )
    $entry = $Archive.CreateEntry($Name, [System.IO.Compression.CompressionLevel]::Optimal)
    $writer = [System.IO.StreamWriter]::new($entry.Open(), [System.Text.UTF8Encoding]::new($false))
    try { $writer.Write($Text) }
    finally { $writer.Dispose() }
}

function Replace-ZipXmlEntry {
    param([System.IO.Compression.ZipArchive]$Archive,[string]$Name,[xml]$Document)
    $old=$Archive.GetEntry($Name)
    if($null -eq $old){throw "Missing fixture entry: $Name"}
    $old.Delete()
    $new=$Archive.CreateEntry($Name,[System.IO.Compression.CompressionLevel]::Optimal)
    $writer=[IO.StreamWriter]::new($new.Open(),[Text.UTF8Encoding]::new($false))
    try{$Document.Save($writer)}finally{$writer.Dispose()}
}

function New-ContentGuardFixture {
    param(
        [string]$Path,
        [string]$RowsXml,
        [string]$Dimension = 'A1:B2'
    )
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::Open($Path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-ZipTextEntry -Archive $archive -Name '[Content_Types].xml' -Text @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
'@
        Add-ZipTextEntry -Archive $archive -Name '_rels/.rels' -Text @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
'@
        Add-ZipTextEntry -Archive $archive -Name 'xl/workbook.xml' -Text @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Fixture" sheetId="1" r:id="rId1"/></sheets></workbook>
'@
        Add-ZipTextEntry -Archive $archive -Name 'xl/_rels/workbook.xml.rels' -Text @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
'@
        Add-ZipTextEntry -Archive $archive -Name 'xl/worksheets/sheet1.xml' -Text @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><dimension ref="$Dimension"/><sheetData>$RowsXml</sheetData></worksheet>
"@
    }
    finally { $archive.Dispose() }
}

function Assert-ContentPublishRejected {
    param(
        [string]$Source,
        [string]$Candidate,
        [string]$Label,
        [string]$GuardRoot
    )
    $expected = Get-AuthoringFileSha256 -Path $Source
    $failure = $null
    try {
        Publish-AuthoringWorkbookChange -SourceWorkbook $Source -CandidateWorkbook $Candidate -Mode FormatOrCommentOnly -ExpectedSourceSha256 $expected -BackupPath (Join-Path $GuardRoot "$Label.backup.xlsx") -TaskOrigin 'AI-000185-TEST' -AuditPath (Join-Path $GuardRoot 'audit.jsonl') | Out-Null
    }
    catch { $failure = $_ }
    Assert-True ($null -ne $failure -and $failure.Exception.Message.Contains('[AUTHORING_CONTENT_PROTECTED]')) "$Label did not trigger content protection."
    Assert-True ((Get-AuthoringFileSha256 -Path $Source) -eq $expected) "$Label changed the protected source despite refusal."
}

$sourceHashesBefore = Get-CsvHashes -Root $sourceRoot
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhat-Authoring-Test-' + [guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

try {
    $workbook = Join-Path $tempRoot 'DataTables_Authoring.xlsx'
    $importer = Join-Path $authoringRoot 'Import-CsvToAuthoring.ps1'
    $exporter = Join-Path $authoringRoot 'Export-AuthoringToCsv.ps1'
    & $importer -DataTablesRoot $sourceRoot -OutputWorkbook $workbook | Out-Null
    Assert-True (Test-Path -LiteralPath $workbook -PathType Leaf) 'Workbook was not generated.'

    $workbookXml = Read-ZipText -ZipPath $workbook -EntryPattern 'xl/workbook.xml'
    $worksheetXml = Read-ZipText -ZipPath $workbook -EntryPattern 'xl/worksheets/sheet*.xml'
    $commentXml = Read-ZipText -ZipPath $workbook -EntryPattern 'xl/comments*.xml'
    $stylesXml = Read-ZipText -ZipPath $workbook -EntryPattern 'xl/styles.xml'
    Assert-True (([regex]::Matches($workbookXml, '<(?:[A-Za-z0-9]+:)?sheet ')).Count -eq 14) 'Workbook must contain exactly 14 sheets.'
    Assert-True (([regex]::Matches($worksheetXml, 'ySplit="1"')).Count -eq 14) 'Every sheet must freeze its first row.'
    Assert-True ($worksheetXml.Contains('dataValidations')) 'Workbook must contain reference or enum data validation.'
    Assert-True ($worksheetXml.Contains('conditionalFormatting')) 'Workbook must contain invalid-reference conditional formatting.'
    Assert-True ($commentXml.Contains('多值分隔符：')) 'Workbook must document each schema list separator.'
    Assert-True ($stylesXml.ToUpperInvariant().Contains('F4B183')) 'Workbook must contain the required-column header color.'

    $sourceDigest = Get-NodeJson -Command 'digest' -Arguments @{ 'data-root' = $sourceRoot }
    $exportRootOne = Join-Path $tempRoot 'export-one'
    $reportOne = Join-Path $tempRoot 'reports-one'
    & $exporter -InputWorkbook $workbook -OutputRoot $exportRootOne -BaselineRoot $sourceRoot -ReportDirectory $reportOne | Out-Null
    $provenanceOne = Get-Content -LiteralPath (Join-Path $reportOne 'cell-provenance.json') -Raw | ConvertFrom-Json
    Assert-True ($provenanceOne.cellCount -gt 0) 'CSV export did not record per-cell provenance.'
    Assert-True (@($provenanceOne.cells | Where-Object { -not $_.exactTextEqual }).Count -eq 0) 'CSV provenance reported a non-faithful derived value.'

    $firstExportHashes = Get-CsvHashes -Root $exportRootOne
    & $exporter -InputWorkbook $workbook -OutputRoot $exportRootOne -BaselineRoot $sourceRoot -ReportDirectory $reportOne | Out-Null
    $secondExportHashes = Get-CsvHashes -Root $exportRootOne
    Assert-HashMapsEqual -Expected $firstExportHashes -Actual $secondExportHashes -Message 'Repeated export was not idempotent.'

    $roundEventDirectory = Join-Path $tempRoot 'round-events'
    & $exporter -InputWorkbook $workbook -OutputRoot $exportRootOne -BaselineRoot $sourceRoot -ReportDirectory $reportOne -RoundId 'USER-U1-R99' -RoundEventDirectory $roundEventDirectory | Out-Null
    $roundEvent = Get-Content -LiteralPath (Join-Path $roundEventDirectory 'USER-U1-R99.json') -Raw | ConvertFrom-Json
    Assert-True ($roundEvent.status -eq 'EXPORTED') 'Authoring export must emit EXPORTED rather than false DONE.'
    Assert-True ([string]::IsNullOrWhiteSpace([string]$roundEvent.userDecision)) 'Authoring export must not invent a user decision.'
    Assert-True ($roundEvent.csvSummarySha256 -match '^[0-9A-F]{64}$') 'Authoring export event must include a CSV summary hash.'
    Assert-True (-not (Get-Content -LiteralPath $exporter -Raw).Contains('queue.ps1')) 'Authoring export must not invoke queue control actions.'

    $emptyWorkbook = Join-Path $tempRoot 'empty-kitchen-areas.xlsx'
    Get-NodeJson -Command 'clear-sheet' -Arguments @{ input = $workbook; output = $emptyWorkbook; sheet = 'KitchenAreas' } | Out-Null
    $emptyTarget = Join-Path $tempRoot 'empty-target'
    [System.IO.Directory]::CreateDirectory($emptyTarget) | Out-Null
    Copy-Item -Path (Join-Path $exportRootOne '*') -Destination $emptyTarget -Recurse -Force
    $kitchenPath = Join-Path $emptyTarget 'Cooking\KitchenAreas.csv'
    $kitchenHashBefore = (Get-FileHash -LiteralPath $kitchenPath -Algorithm SHA256).Hash
    & $exporter -InputWorkbook $emptyWorkbook -OutputRoot $emptyTarget -BaselineRoot $sourceRoot -ReportDirectory (Join-Path $tempRoot 'reports-empty') | Out-Null
    $kitchenHashAfter = (Get-FileHash -LiteralPath $kitchenPath -Algorithm SHA256).Hash
    Assert-True ($kitchenHashBefore -eq $kitchenHashAfter) 'An empty sheet overwrote its existing CSV.'

    $glyphTarget = Join-Path $tempRoot 'glyph-target'
    [System.IO.Directory]::CreateDirectory($glyphTarget) | Out-Null
    Copy-Item -Path (Join-Path $exportRootOne '*') -Destination $glyphTarget -Recurse -Force
    $localizationPath = Join-Path $glyphTarget 'Localization\Localization.csv'
    $newGlyph = [string][char]0x9F98
    [System.IO.File]::AppendAllText($localizationPath, "test.glyph,$newGlyph,fixture`r`n", [System.Text.UTF8Encoding]::new($false))
    $glyphReport = Join-Path $tempRoot 'G53-test.txt'
    Get-NodeJson -Command 'glyph' -Arguments @{ 'baseline-root' = $sourceRoot; 'comparison-root' = $glyphTarget; output = $glyphReport } | Out-Null
    $glyphText = [System.IO.File]::ReadAllText($glyphReport, [System.Text.Encoding]::UTF8)
    Assert-True ($glyphText.Contains($newGlyph)) 'G53 report did not capture the added glyph.'
    Assert-True ($glyphText.Contains('test.glyph')) 'G53 report did not capture the source localization key.'

    $realWorkbook = Join-Path $authoringRoot 'DataTables_Authoring.xlsx'
    if (Test-Path -LiteralPath $realWorkbook -PathType Leaf) {
        $lfExportRoot = Join-Path $tempRoot 'lf-export'
        $lfProvenancePath = Join-Path $tempRoot 'lf-provenance.json'
        Get-NodeJson -Command 'export' -Arguments @{
            'baseline-root' = $sourceRoot
            'data-root' = $sourceRoot
            input = $realWorkbook
            'output-root' = $lfExportRoot
            'provenance-output' = $lfProvenancePath
            'expected-input-sha' = (Get-AuthoringFileSha256 -Path $realWorkbook)
        } | Out-Null
        $lfProvenance = Get-Content -LiteralPath $lfProvenancePath -Raw | ConvertFrom-Json
        $lfCell = @($lfProvenance.cells | Where-Object { $_.sheet -eq 'Localization' -and $_.cell -eq 'B151' })
        Assert-True ($lfCell.Count -eq 1) 'LF regression fixture Localization!B151 was not traced.'
        Assert-True ($lfCell[0].sourceValue.Contains("`n") -and -not $lfCell[0].sourceValue.Contains("`r")) 'Localization!B151 source LF representation was not preserved.'
        $lfCsvText = [System.IO.File]::ReadAllText((Join-Path $lfExportRoot 'Localization\Localization.csv'), [System.Text.Encoding]::UTF8)
        Assert-True ($lfCsvText.Contains([string]$lfCell[0].sourceValue)) 'Derived Localization CSV rewrote the source cell line ending.'
    }

    $guardRoot = Join-Path $tempRoot 'content-guard'
    [System.IO.Directory]::CreateDirectory($guardRoot) | Out-Null
    $baseRows = '<row r="1"><c r="A1" t="inlineStr"><is><t>item_id</t></is></c><c r="B1" t="inlineStr"><is><t>name</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>item_1</t></is></c><c r="B2" t="inlineStr"><is><t>Alpha</t></is></c></row>'
    $guardBase = Join-Path $guardRoot 'base.xlsx'
    New-ContentGuardFixture -Path $guardBase -RowsXml $baseRows
    $baselineSnapshot = Get-AuthoringWorkbookContentSnapshot -WorkbookPath $guardBase
    Assert-True ($baselineSnapshot.nonEmptyCellCount -eq 4) 'Content snapshot did not retain all non-empty cells.'

    $destructiveFixtures = [ordered]@{
        delete = '<row r="1"><c r="A1" t="inlineStr"><is><t>item_id</t></is></c><c r="B1" t="inlineStr"><is><t>name</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>item_1</t></is></c></row>'
        text = '<row r="1"><c r="A1" t="inlineStr"><is><t>item_id</t></is></c><c r="B1" t="inlineStr"><is><t>name</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>item_1</t></is></c><c r="B2" t="inlineStr"><is><t>Beta</t></is></c></row>'
        formula = '<row r="1"><c r="A1" t="inlineStr"><is><t>item_id</t></is></c><c r="B1" t="inlineStr"><is><t>name</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>item_1</t></is></c><c r="B2"><f>1+1</f><v>2</v></c></row>'
        type = '<row r="1"><c r="A1" t="inlineStr"><is><t>item_id</t></is></c><c r="B1" t="inlineStr"><is><t>name</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>item_1</t></is></c><c r="B2" t="n"><v>7</v></c></row>'
        move = '<row r="1"><c r="A1" t="inlineStr"><is><t>item_id</t></is></c><c r="B1" t="inlineStr"><is><t>name</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>item_1</t></is></c></row><row r="3"><c r="B3" t="inlineStr"><is><t>Alpha</t></is></c></row>'
    }
    foreach ($label in $destructiveFixtures.Keys) {
        $source = Join-Path $guardRoot "$label-source.xlsx"
        $candidate = Join-Path $guardRoot "$label-candidate.xlsx"
        Copy-Item -LiteralPath $guardBase -Destination $source
        New-ContentGuardFixture -Path $candidate -RowsXml $destructiveFixtures[$label] -Dimension 'A1:B3'
        Assert-ContentPublishRejected -Source $source -Candidate $candidate -Label $label -GuardRoot $guardRoot
    }

    $formatSource = Join-Path $guardRoot 'format-source.xlsx'
    $formatCandidate = Join-Path $guardRoot 'format-candidate.xlsx'
    $formatRows = $baseRows.Replace('<c r="B2" ', '<c r="B2" s="9" ')
    Copy-Item -LiteralPath $guardBase -Destination $formatSource
    New-ContentGuardFixture -Path $formatCandidate -RowsXml $formatRows
    $formatBackup = Join-Path $guardRoot 'format.backup.xlsx'
    $formatPublish = Publish-AuthoringWorkbookChange -SourceWorkbook $formatSource -CandidateWorkbook $formatCandidate -Mode FormatOrCommentOnly -ExpectedSourceSha256 (Get-AuthoringFileSha256 -Path $formatSource) -BackupPath $formatBackup -TaskOrigin 'AI-000185-TEST' -AuditPath (Join-Path $guardRoot 'audit.jsonl')
    Assert-True ($formatPublish.contentDifferenceCount -eq 0) 'Format-only candidate produced a content diff.'
    $formatPublishedSha = Get-AuthoringFileSha256 -Path $formatSource
    Restore-AuthoringWorkbookBackup -TargetWorkbook $formatSource -BackupPath $formatBackup -ExpectedCurrentSha256 $formatPublishedSha -RollbackBackupPath (Join-Path $guardRoot 'format.rollback-backup.xlsx') -TaskOrigin 'AI-000185-TEST' -AuditPath (Join-Path $guardRoot 'audit.jsonl') | Out-Null
    Assert-True ((Get-AuthoringFileSha256 -Path $formatSource) -eq (Get-AuthoringFileSha256 -Path $guardBase)) 'Rollback did not restore the exact workbook bytes.'

    $blankSource = Join-Path $guardRoot 'blank-source.xlsx'
    $blankCandidate = Join-Path $guardRoot 'blank-candidate.xlsx'
    Copy-Item -LiteralPath $guardBase -Destination $blankSource
    New-ContentGuardFixture -Path $blankCandidate -RowsXml ($baseRows + '<row r="3"><c r="C3" s="1"/></row>') -Dimension 'A1:C3'
    $blankPublish = Publish-AuthoringWorkbookChange -SourceWorkbook $blankSource -CandidateWorkbook $blankCandidate -Mode BlankStructureOnly -ExpectedSourceSha256 (Get-AuthoringFileSha256 -Path $blankSource) -BackupPath (Join-Path $guardRoot 'blank.backup.xlsx') -TaskOrigin 'AI-000185-TEST-BLANK-ROW' -AuditPath (Join-Path $guardRoot 'audit.jsonl')
    Assert-True ($blankPublish.contentDifferenceCount -eq 0) 'Explicit blank structure changed protected content.'
    Assert-True ($blankPublish.structureDifferenceCount -gt 0) 'Explicit blank structure was not recorded in the audit receipt.'

    $fillBlankRows = $baseRows + '<row r="3"><c r="C3" s="1"/></row>'
    $fillBlankSource = Join-Path $guardRoot 'fill-blank-source.xlsx'
    $fillBlankCandidate = Join-Path $guardRoot 'fill-blank-candidate.xlsx'
    New-ContentGuardFixture -Path $fillBlankSource -RowsXml $fillBlankRows -Dimension 'A1:C3'
    New-ContentGuardFixture -Path $fillBlankCandidate -RowsXml ($baseRows + '<row r="3"><c r="C3" t="inlineStr"><is><t>Known source</t></is></c></row>') -Dimension 'A1:C3'
    $fillBlankPublish = Publish-AuthoringWorkbookChange -SourceWorkbook $fillBlankSource -CandidateWorkbook $fillBlankCandidate -Mode FillPreviouslyBlankCells -ExpectedSourceSha256 (Get-AuthoringFileSha256 -Path $fillBlankSource) -BackupPath (Join-Path $guardRoot 'fill-blank.backup.xlsx') -TaskOrigin 'AI-000192-TEST-BLANK-FILL' -AuditPath (Join-Path $guardRoot 'audit.jsonl')
    Assert-True ($fillBlankPublish.contentDifferenceCount -eq 1) 'Narrow blank-fill mode did not record the filled cell.'

    $fillNonemptySource = Join-Path $guardRoot 'fill-nonempty-source.xlsx'
    $fillNonemptyCandidate = Join-Path $guardRoot 'fill-nonempty-candidate.xlsx'
    Copy-Item -LiteralPath $guardBase -Destination $fillNonemptySource
    New-ContentGuardFixture -Path $fillNonemptyCandidate -RowsXml $destructiveFixtures.text
    $fillNonemptyFailure = $null
    try {
        Publish-AuthoringWorkbookChange -SourceWorkbook $fillNonemptySource -CandidateWorkbook $fillNonemptyCandidate -Mode FillPreviouslyBlankCells -ExpectedSourceSha256 (Get-AuthoringFileSha256 -Path $fillNonemptySource) -BackupPath (Join-Path $guardRoot 'fill-nonempty.backup.xlsx') -TaskOrigin 'AI-000192-TEST-NONEMPTY-REFUSAL' -AuditPath (Join-Path $guardRoot 'audit.jsonl') | Out-Null
    }
    catch { $fillNonemptyFailure = $_ }
    Assert-True ($null -ne $fillNonemptyFailure -and $fillNonemptyFailure.Exception.Message.Contains('[AUTHORING_CONTENT_PROTECTED]')) 'Narrow blank-fill mode allowed an existing non-empty value to change.'

    $driftSource = Join-Path $guardRoot 'drift-source.xlsx'
    Copy-Item -LiteralPath $guardBase -Destination $driftSource
    $staleExpected = Get-AuthoringFileSha256 -Path $driftSource
    Copy-Item -LiteralPath $formatCandidate -Destination $driftSource -Force
    $driftHash = Get-AuthoringFileSha256 -Path $driftSource
    $driftFailure = $null
    try {
        Publish-AuthoringWorkbookChange -SourceWorkbook $driftSource -CandidateWorkbook $guardBase -Mode FormatOrCommentOnly -ExpectedSourceSha256 $staleExpected -BackupPath (Join-Path $guardRoot 'drift.backup.xlsx') -TaskOrigin 'AI-000185-TEST' -AuditPath (Join-Path $guardRoot 'audit.jsonl') | Out-Null
    }
    catch { $driftFailure = $_ }
    Assert-True ($null -ne $driftFailure -and $driftFailure.Exception.Message.Contains('[AUTHORING_EXTERNAL_DRIFT]')) 'External drift was not rejected.'
    Assert-True ((Get-AuthoringFileSha256 -Path $driftSource) -eq $driftHash) 'External drift refusal overwrote the changed source.'

    # AI186: deterministic post-freeze and post-staging windows in the public API.
    $externalWorkbook = Join-Path $guardRoot 'external-new-value.xlsx'
    New-ContentGuardFixture -Path $externalWorkbook -RowsXml $destructiveFixtures.text
    foreach ($case in @('SourceAfterFreeze','SourceDuringStaging','LateBackup','StagingDrift','CandidateDuringStaging')) {
        $caseRoot = Join-Path $guardRoot $case
        [IO.Directory]::CreateDirectory($caseRoot) | Out-Null
        $caseSource=Join-Path $caseRoot 'source.xlsx'
        $caseCandidate=Join-Path $caseRoot 'candidate.xlsx'
        $caseBackup=Join-Path $caseRoot 'backup.xlsx'
        Copy-Item -LiteralPath $guardBase -Destination $caseSource
        Copy-Item -LiteralPath $formatCandidate -Destination $caseCandidate
        $caseExpected=Get-AuthoringFileSha256 $caseSource
        $fixtureExternalSha=Get-AuthoringFileSha256 $externalWorkbook
        $caseFault={
            param($phase,$record,$index,$context)
            if($case -eq 'SourceAfterFreeze' -and $phase -eq 'AfterCandidateFreeze'){Copy-Item $externalWorkbook $caseSource -Force}
            if($case -eq 'SourceDuringStaging' -and $phase -eq 'AfterStaging'){Copy-Item $externalWorkbook $caseSource -Force}
            if($case -eq 'LateBackup' -and $phase -eq 'AfterStaging'){[IO.File]::WriteAllText($caseBackup,'UNKNOWN_BACKUP',[Text.UTF8Encoding]::new($false))}
            if($case -eq 'StagingDrift' -and $phase -eq 'AfterStaging'){Copy-Item $externalWorkbook $record.stagingPath -Force}
            if($case -eq 'CandidateDuringStaging' -and $phase -eq 'AfterStaging'){Copy-Item $externalWorkbook $caseCandidate -Force}
        }.GetNewClosure()
        $caseFailure=$null
        try { Publish-AuthoringWorkbookChange -SourceWorkbook $caseSource -CandidateWorkbook $caseCandidate -Mode FormatOrCommentOnly -ExpectedSourceSha256 $caseExpected -BackupPath $caseBackup -TaskOrigin 'AI-000186-TEST' -AuditPath (Join-Path $caseRoot 'audit.jsonl') -TestFaultInjector $caseFault -TestFixtureRoot $caseRoot | Out-Null }
        catch { $caseFailure=$_ }
        Assert-True ($null -ne $caseFailure) "$case returned false publish success."
        $expectedHash=if($case -like 'Source*'){$fixtureExternalSha}else{$caseExpected}
        Assert-True ((Get-AuthoringFileSha256 $caseSource) -eq $expectedHash) "$case overwrote latest source bytes."
        if($case -eq 'LateBackup'){Assert-True (([IO.File]::ReadAllText($caseBackup)) -eq 'UNKNOWN_BACKUP') 'Late backup conflict was overwritten.'}
        else {Assert-True (-not (Test-Path $caseBackup)) "$case created a backup before refusing the write."}
        $auditText=if(Test-Path (Join-Path $caseRoot 'audit.jsonl')){[IO.File]::ReadAllText((Join-Path $caseRoot 'audit.jsonl'))}else{''}
        Assert-True (-not $auditText.Contains('"action":"WORKBOOK_PUBLISHED"')) "$case falsely audited a successful publish."
    }

    $realFormatRoot=Join-Path $tempRoot 'real-format'
    [IO.Directory]::CreateDirectory($realFormatRoot)|Out-Null
    $realSource=Join-Path $realFormatRoot 'source.xlsx'
    $realCandidate=Join-Path $realFormatRoot 'candidate.xlsx'
    Copy-Item (Join-Path $authoringRoot 'DataTables_Authoring.xlsx') $realSource
    $realHash=Get-AuthoringFileSha256 $realSource
    $formatReceipt=& (Join-Path $authoringRoot 'Format-AuthoringWorkbook.ps1') -InputWorkbook $realSource -CandidateWorkbook $realCandidate -ExpectedInputSha256 $realHash
    Assert-True ($formatReceipt.repairedStyleZero -eq 26) 'AI184 style0 loss count was not repaired.'
    $realBefore=Get-AuthoringWorkbookContentSnapshot $realSource
    $realAfter=Get-AuthoringWorkbookContentSnapshot $realCandidate
    Assert-True ((Compare-AuthoringWorkbookContent $realBefore $realAfter).isEqual) 'Format candidate changed values/types/formulas.'
    Assert-True ((Compare-AuthoringWorkbookStructure $realBefore $realAfter).isEqual) 'Format candidate changed stored blank structure.'
    $processingXml=Read-ZipText -ZipPath $realCandidate -EntryPattern 'xl/worksheets/sheet7.xml'
    Assert-True ($processingXml.Contains('sqref="B2:B5000"') -and $processingXml.Contains('B2)=0)')) 'ProcessingRecords output_item formula did not use B2.'
    $stepsXml=Read-ZipText -ZipPath $realCandidate -EntryPattern 'xl/worksheets/sheet10.xml'
    Assert-True ($stepsXml.Contains('TEXTSPLIT') -and $stepsXml.Contains('G2:G5000') -and -not $stepsXml.Contains('_xludf.TEXTSPLIT')) 'List reference rule is missing or still uses the stale per-cell formula.'
    for($sheetIndex=1;$sheetIndex -le 14;$sheetIndex++){
        $candidateSheet=Read-ZipText -ZipPath $realCandidate -EntryPattern "xl/worksheets/sheet$sheetIndex.xml"
        Assert-True (-not $candidateSheet.Contains('_xludf.TEXTSPLIT')) "Old skip-unchanged list rule remained in sheet$sheetIndex."
    }
    $stepsSheet=@($realBefore.sheets | Where-Object name -eq 'RecipeSteps')[0]
    $itemsSheet=@($realBefore.sheets | Where-Object name -eq 'Items')[0]
    $itemIds=@($itemsSheet.cells | Where-Object column -eq 'A' | ForEach-Object value)
    $validList=@($stepsSheet.cells | Where-Object cell -eq 'G2')[0].value
    $invalidList=@($stepsSheet.cells | Where-Object cell -eq 'G10')[0].value
    Assert-True (@($validList.Split(';') | Where-Object { $itemIds -notcontains $_ }).Count -eq 0) 'Known valid list fixture contains an unknown ID.'
    Assert-True ($invalidList -eq 'prd_cabbage_washed' -and $itemIds -notcontains $invalidList) 'Known invalid reference fixture was silently changed.'
    Assert-True ($stepsXml.Contains('sqref="G2:G5000"') -and $stepsXml.Contains('TEXTSPLIT(G2') -and $stepsXml.Contains('COUNTIF(')) 'Full-range list validation would not cover the invalid G10 ID.'
    $csvBeforeRoot=Join-Path $realFormatRoot 'csv-before'
    $csvAfterRoot=Join-Path $realFormatRoot 'csv-after'
    Get-NodeJson -Command 'export' -Arguments @{ 'data-root'=$sourceRoot; input=$realSource; 'output-root'=$csvBeforeRoot; 'baseline-root'=$sourceRoot; 'expected-input-sha'=$realHash } | Out-Null
    Get-NodeJson -Command 'export' -Arguments @{ 'data-root'=$sourceRoot; input=$realCandidate; 'output-root'=$csvAfterRoot; 'baseline-root'=$sourceRoot; 'expected-input-sha'=(Get-AuthoringFileSha256 $realCandidate) } | Out-Null
    Assert-HashMapsEqual (Get-CsvHashes $csvBeforeRoot) (Get-CsvHashes $csvAfterRoot) 'Formatting changed derived CSV bytes.'

    # Unknown hidden sheets and formulas are user content, even when not in a schema.
    $extraSource=Join-Path $realFormatRoot 'source-with-hidden.xlsx'
    $extraCandidate=Join-Path $realFormatRoot 'candidate-with-hidden.xlsx'
    Copy-Item $realSource $extraSource
    $extraZip=[IO.Compression.ZipFile]::Open($extraSource,[IO.Compression.ZipArchiveMode]::Update)
    try{
        $book=Read-AuthoringZipEntryText -Archive $extraZip -Name 'xl/workbook.xml'
        [xml]$bookXml=$book
        $mainNs='http://schemas.openxmlformats.org/spreadsheetml/2006/main'
        $relationNs='http://schemas.openxmlformats.org/officeDocument/2006/relationships'
        $bm=[Xml.XmlNamespaceManager]::new($bookXml.NameTable);$bm.AddNamespace('x',$mainNs)
        $sheets=$bookXml.SelectSingleNode('/x:workbook/x:sheets',$bm)
        $newSheet=$bookXml.CreateElement('sheet',$mainNs)
        $newSheet.SetAttribute('name','UserExtra');$newSheet.SetAttribute('sheetId','99');$newSheet.SetAttribute('state','hidden')
        [void]$newSheet.SetAttribute('id',$relationNs,'rId99')
        [void]$sheets.AppendChild($newSheet)
        Replace-ZipXmlEntry -Archive $extraZip -Name 'xl/workbook.xml' -Document $bookXml
        [xml]$rels=Read-AuthoringZipEntryText -Archive $extraZip -Name 'xl/_rels/workbook.xml.rels'
        $rel=$rels.CreateElement('Relationship','http://schemas.openxmlformats.org/package/2006/relationships')
        $rel.SetAttribute('Id','rId99');$rel.SetAttribute('Type','http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet');$rel.SetAttribute('Target','worksheets/sheet99.xml')
        [void]$rels.DocumentElement.AppendChild($rel)
        Replace-ZipXmlEntry -Archive $extraZip -Name 'xl/_rels/workbook.xml.rels' -Document $rels
        [xml]$types=Read-AuthoringZipEntryText -Archive $extraZip -Name '[Content_Types].xml'
        $override=$types.CreateElement('Override','http://schemas.openxmlformats.org/package/2006/content-types')
        $override.SetAttribute('PartName','/xl/worksheets/sheet99.xml');$override.SetAttribute('ContentType','application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml')
        [void]$types.DocumentElement.AppendChild($override)
        Replace-ZipXmlEntry -Archive $extraZip -Name '[Content_Types].xml' -Document $types
        Add-ZipTextEntry -Archive $extraZip -Name 'xl/worksheets/sheet99.xml' -Text '<?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><dimension ref="A1:B1"/><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>00123</t></is></c><c r="B1"><f>1+2</f><v>3</v></c></row></sheetData></worksheet>'
    }finally{$extraZip.Dispose()}
    $extraBefore=Get-AuthoringWorkbookContentSnapshot $extraSource
    $extraReceipt=& (Join-Path $authoringRoot 'Format-AuthoringWorkbook.ps1') -InputWorkbook $extraSource -CandidateWorkbook $extraCandidate -ExpectedInputSha256 (Get-AuthoringFileSha256 $extraSource)
    $extraAfter=Get-AuthoringWorkbookContentSnapshot $extraCandidate
    Assert-True ($extraReceipt.sheetCount -eq 15 -and $extraAfter.sheets[14].state -eq 'hidden') 'Unknown hidden sheet was lost.'
    Assert-True ((Compare-AuthoringWorkbookContent $extraBefore $extraAfter).isEqual) 'Hidden text/formula contents changed.'
    Assert-True ((Compare-AuthoringWorkbookStructure $extraBefore $extraAfter).isEqual) 'Hidden sheet structure changed.'

    $transactionRoot = Join-Path $tempRoot 'derived-transactions'
    [System.IO.Directory]::CreateDirectory($transactionRoot) | Out-Null

    $partialRoot = Join-Path $transactionRoot 'partial'
    $partialCandidates = Join-Path $partialRoot 'candidates'
    $partialTargets = Join-Path $partialRoot 'targets'
    $partialBackups = Join-Path $partialRoot 'backups'
    Write-Utf8Fixture -Path (Join-Path $partialCandidates 'a.csv') -Text 'new-a'
    Write-Utf8Fixture -Path (Join-Path $partialCandidates 'b.csv') -Text 'new-b'
    Write-Utf8Fixture -Path (Join-Path $partialTargets 'a.csv') -Text 'old-a'
    Write-Utf8Fixture -Path (Join-Path $partialTargets 'b.csv') -Text 'old-b'
    $partialPlan = New-AuthoringDerivedPublishPlan -TargetRoot $partialTargets -RelativePaths @('a.csv','b.csv') -BackupDirectory $partialBackups
    $partialFault = { param($phase,$record,$index,$context) if ($phase -eq 'BeforeWrite' -and $index -eq 1) { [System.IO.Directory]::CreateDirectory((Split-Path -Parent $record.backup)) | Out-Null; [System.IO.File]::WriteAllText($record.backup, 'late-conflict', [System.Text.UTF8Encoding]::new($false)) } }
    $partialFailure = $null
    try { Publish-AuthoringDerivedFileSet -CandidateRoot $partialCandidates -TargetRoot $partialTargets -RelativePaths @('a.csv','b.csv') -BackupDirectory $partialBackups -TaskOrigin 'AI-000192-TEST-PARTIAL' -AuditPath (Join-Path $partialRoot 'audit.jsonl') -PublishPlan $partialPlan -TestFaultInjector $partialFault -TestFixtureRoot $partialRoot | Out-Null }
    catch { $partialFailure = $_ }
    Assert-True ($null -ne $partialFailure -and $partialFailure.Exception.Message.Contains('[AUTHORING_BACKUP_EXISTS]')) 'Two-file partial publish did not fail at the second file.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $partialTargets 'a.csv'))) -eq 'old-a') 'First file was not byte-exactly restored after the second file failed.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $partialTargets 'b.csv'))) -eq 'old-b') 'Second file changed during partial-publish failure.'

    foreach($case in @('TargetDuringStaging','BackupDuringStaging')){
        $caseRoot=Join-Path $transactionRoot $case
        $targets=Join-Path $caseRoot 'targets';$candidates=Join-Path $caseRoot 'candidates';$backups=Join-Path $caseRoot 'backups'
        Write-Utf8Fixture (Join-Path $targets 'a.csv') 'frozen'
        Write-Utf8Fixture (Join-Path $candidates 'a.csv') 'candidate'
        $plan=New-AuthoringDerivedPublishPlan -TargetRoot $targets -RelativePaths @('a.csv') -BackupDirectory $backups
        $fixtureFault={
            param($phase,$record,$index,$context)
            if($phase -eq 'AfterStaging'){
                if($case -eq 'TargetDuringStaging'){[IO.File]::WriteAllText($record.target,'EXTERNAL_EDIT',[Text.UTF8Encoding]::new($false))}
                else {[IO.Directory]::CreateDirectory((Split-Path -Parent $record.backup))|Out-Null;[IO.File]::WriteAllText($record.backup,'UNKNOWN_BACKUP',[Text.UTF8Encoding]::new($false))}
            }
        }.GetNewClosure()
        $failure=$null
        try{Publish-AuthoringDerivedFileSet -CandidateRoot $candidates -TargetRoot $targets -RelativePaths @('a.csv') -BackupDirectory $backups -TaskOrigin 'AI-000186-TEST' -AuditPath (Join-Path $caseRoot 'audit.jsonl') -PublishPlan $plan -TestFaultInjector $fixtureFault -TestFixtureRoot $caseRoot | Out-Null}catch{$failure=$_}
        Assert-True ($null -ne $failure) "$case returned false CSV publish success."
        $wanted=if($case -eq 'TargetDuringStaging'){'EXTERNAL_EDIT'}else{'frozen'}
        Assert-True (([IO.File]::ReadAllText((Join-Path $targets 'a.csv'))) -eq $wanted) "$case overwrote the CSV target."
        if($case -eq 'BackupDuringStaging'){Assert-True (([IO.File]::ReadAllText((Join-Path $backups 'a.csv.backup'))) -eq 'UNKNOWN_BACKUP') 'CSV late backup was overwritten.'}
    }

    $verifyRoot = Join-Path $transactionRoot 'verify-failure'
    $verifyCandidates = Join-Path $verifyRoot 'candidates'
    $verifyTargets = Join-Path $verifyRoot 'targets'
    $verifyBackups = Join-Path $verifyRoot 'backups'
    Write-Utf8Fixture -Path (Join-Path $verifyCandidates 'a.csv') -Text 'new-a'
    Write-Utf8Fixture -Path (Join-Path $verifyTargets 'a.csv') -Text 'old-a'
    $verifyPlan = New-AuthoringDerivedPublishPlan -TargetRoot $verifyTargets -RelativePaths @('a.csv') -BackupDirectory $verifyBackups
    $verifyFault = { param($phase,$record,$index,$context) if ($phase -eq 'AfterWriteBeforeVerify') { throw '[TEST_VERIFY_FAILURE]' } }
    $verifyFailure = $null
    try { Publish-AuthoringDerivedFileSet -CandidateRoot $verifyCandidates -TargetRoot $verifyTargets -RelativePaths @('a.csv') -BackupDirectory $verifyBackups -TaskOrigin 'AI-000192-TEST-VERIFY' -AuditPath (Join-Path $verifyRoot 'audit.jsonl') -PublishPlan $verifyPlan -TestFaultInjector $verifyFault -TestFixtureRoot $verifyRoot | Out-Null }
    catch { $verifyFailure = $_ }
    Assert-True ($null -ne $verifyFailure -and $verifyFailure.Exception.Data['PrimaryError'].Contains('[TEST_VERIFY_FAILURE]')) 'Post-write verification fault did not preserve the primary error.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $verifyTargets 'a.csv'))) -eq 'old-a') 'Write-attempted file was not tracked and restored.'

    $externalRoot = Join-Path $transactionRoot 'external-drift'
    $externalCandidates = Join-Path $externalRoot 'candidates'
    $externalTargets = Join-Path $externalRoot 'targets'
    $externalBackups = Join-Path $externalRoot 'backups'
    Write-Utf8Fixture -Path (Join-Path $externalCandidates 'a.csv') -Text 'new-a'
    Write-Utf8Fixture -Path (Join-Path $externalTargets 'a.csv') -Text 'old-a'
    $externalPlan = New-AuthoringDerivedPublishPlan -TargetRoot $externalTargets -RelativePaths @('a.csv') -BackupDirectory $externalBackups
    $externalFault = { param($phase,$record,$index,$context) if ($phase -eq 'AfterWriteBeforeVerify') { [System.IO.File]::WriteAllText($record.target, 'external-edit', [System.Text.UTF8Encoding]::new($false)); throw '[TEST_EXTERNAL_EDIT]' } }
    $externalFailure = $null
    try { Publish-AuthoringDerivedFileSet -CandidateRoot $externalCandidates -TargetRoot $externalTargets -RelativePaths @('a.csv') -BackupDirectory $externalBackups -TaskOrigin 'AI-000192-TEST-EXTERNAL' -AuditPath (Join-Path $externalRoot 'audit.jsonl') -PublishPlan $externalPlan -TestFaultInjector $externalFault -TestFixtureRoot $externalRoot | Out-Null }
    catch { $externalFailure = $_ }
    Assert-True ($null -ne $externalFailure -and $externalFailure.Exception.Message.Contains('Rollback: incomplete')) 'External target drift was not reported as an incomplete rollback.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $externalTargets 'a.csv'))) -eq 'external-edit') 'Rollback overwrote an external target modification.'
    Assert-True (Test-Path -LiteralPath (Join-Path $externalBackups 'a.csv.backup')) 'Backup was not preserved after external target drift.'

    $backupDriftRoot = Join-Path $transactionRoot 'backup-drift'
    $backupDriftCandidates = Join-Path $backupDriftRoot 'candidates'
    $backupDriftTargets = Join-Path $backupDriftRoot 'targets'
    $backupDriftBackups = Join-Path $backupDriftRoot 'backups'
    Write-Utf8Fixture -Path (Join-Path $backupDriftCandidates 'a.csv') -Text 'new-a'
    Write-Utf8Fixture -Path (Join-Path $backupDriftTargets 'a.csv') -Text 'old-a'
    $backupDriftPlan = New-AuthoringDerivedPublishPlan -TargetRoot $backupDriftTargets -RelativePaths @('a.csv') -BackupDirectory $backupDriftBackups
    $backupDriftFault = { param($phase,$record,$index,$context) if ($phase -eq 'AfterWriteBeforeVerify') { throw '[TEST_BACKUP_DRIFT_TRIGGER]' }; if ($phase -eq 'BeforeRollback') { [System.IO.File]::WriteAllText($context.records[0].backup, 'external-backup-edit', [System.Text.UTF8Encoding]::new($false)) } }
    $backupDriftFailure = $null
    try { Publish-AuthoringDerivedFileSet -CandidateRoot $backupDriftCandidates -TargetRoot $backupDriftTargets -RelativePaths @('a.csv') -BackupDirectory $backupDriftBackups -TaskOrigin 'AI-000192-TEST-BACKUP-DRIFT' -AuditPath (Join-Path $backupDriftRoot 'audit.jsonl') -PublishPlan $backupDriftPlan -TestFaultInjector $backupDriftFault -TestFixtureRoot $backupDriftRoot | Out-Null }
    catch { $backupDriftFailure = $_ }
    Assert-True ($null -ne $backupDriftFailure -and $backupDriftFailure.Exception.Data['RollbackResults'].Contains('AUTHORING_ROLLBACK_BACKUP_DRIFT')) 'Backup drift was not preserved in rollback evidence.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $backupDriftTargets 'a.csv'))) -eq 'new-a') 'Backup drift rollback unexpectedly overwrote the published target.'

    $candidateDriftRoot = Join-Path $transactionRoot 'candidate-drift'
    $candidateDriftCandidates = Join-Path $candidateDriftRoot 'candidates'
    $candidateDriftTargets = Join-Path $candidateDriftRoot 'targets'
    $candidateDriftBackups = Join-Path $candidateDriftRoot 'backups'
    Write-Utf8Fixture -Path (Join-Path $candidateDriftCandidates 'a.csv') -Text 'new-a'
    Write-Utf8Fixture -Path (Join-Path $candidateDriftTargets 'a.csv') -Text 'old-a'
    $candidateDriftPlan = New-AuthoringDerivedPublishPlan -TargetRoot $candidateDriftTargets -RelativePaths @('a.csv') -BackupDirectory $candidateDriftBackups
    $candidateDriftFault = { param($phase,$record,$index,$context) if ($phase -eq 'AfterCandidateFreeze') { [System.IO.File]::WriteAllText($context.records[0].candidate, 'candidate-changed', [System.Text.UTF8Encoding]::new($false)) } }
    $candidateDriftFailure = $null
    try { Publish-AuthoringDerivedFileSet -CandidateRoot $candidateDriftCandidates -TargetRoot $candidateDriftTargets -RelativePaths @('a.csv') -BackupDirectory $candidateDriftBackups -TaskOrigin 'AI-000192-TEST-CANDIDATE-DRIFT' -AuditPath (Join-Path $candidateDriftRoot 'audit.jsonl') -PublishPlan $candidateDriftPlan -TestFaultInjector $candidateDriftFault -TestFixtureRoot $candidateDriftRoot | Out-Null }
    catch { $candidateDriftFailure = $_ }
    Assert-True ($null -ne $candidateDriftFailure -and $candidateDriftFailure.Exception.Message.Contains('[AUTHORING_CANDIDATE_DRIFT]')) 'Candidate drift after freeze was not refused.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $candidateDriftTargets 'a.csv'))) -eq 'old-a') 'Candidate drift refusal changed the target.'

    $auditFaultRoot = Join-Path $transactionRoot 'audit-failure'
    $auditFaultCandidates = Join-Path $auditFaultRoot 'candidates'
    $auditFaultTargets = Join-Path $auditFaultRoot 'targets'
    $auditFaultBackups = Join-Path $auditFaultRoot 'backups'
    foreach ($name in @('a.csv','b.csv')) { Write-Utf8Fixture -Path (Join-Path $auditFaultCandidates $name) -Text "new-$name"; Write-Utf8Fixture -Path (Join-Path $auditFaultTargets $name) -Text "old-$name" }
    $auditFaultPlan = New-AuthoringDerivedPublishPlan -TargetRoot $auditFaultTargets -RelativePaths @('a.csv','b.csv') -BackupDirectory $auditFaultBackups
    $auditFault = { param($phase,$record,$index,$context) if ($phase -eq 'AfterAllWritesBeforeAudit') { throw '[TEST_FINAL_AUDIT_FAILURE]' } }
    $auditFailure = $null
    try { Publish-AuthoringDerivedFileSet -CandidateRoot $auditFaultCandidates -TargetRoot $auditFaultTargets -RelativePaths @('a.csv','b.csv') -BackupDirectory $auditFaultBackups -TaskOrigin 'AI-000192-TEST-AUDIT' -AuditPath (Join-Path $auditFaultRoot 'audit.jsonl') -PublishPlan $auditFaultPlan -TestFaultInjector $auditFault -TestFixtureRoot $auditFaultRoot | Out-Null }
    catch { $auditFailure = $_ }
    Assert-True ($null -ne $auditFailure -and $auditFailure.Exception.Data['PrimaryError'].Contains('[TEST_FINAL_AUDIT_FAILURE]')) 'Final audit fault did not preserve the primary error.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $auditFaultTargets 'a.csv'))) -eq 'old-a.csv') 'Final audit failure did not restore a.csv.'
    Assert-True (([System.IO.File]::ReadAllText((Join-Path $auditFaultTargets 'b.csv'))) -eq 'old-b.csv') 'Final audit failure did not restore b.csv.'

    $repeatRoot = Join-Path $transactionRoot 'repeat-rollback'
    $repeatTarget = Join-Path $repeatRoot 'target.csv'
    $repeatBackup = Join-Path $repeatRoot 'target.csv.backup'
    Write-Utf8Fixture -Path $repeatTarget -Text 'new'
    Write-Utf8Fixture -Path $repeatBackup -Text 'old'
    $repeatRecord = [pscustomobject]@{ relativePath='target.csv'; target=$repeatTarget; targetExisted=$true; targetBeforeSha256=(Get-AuthoringFileSha256 -Path $repeatBackup); candidateSha256=(Get-AuthoringFileSha256 -Path $repeatTarget); backup=$repeatBackup; state='WRITTEN' }
    $repeatFirst = @(Invoke-AuthoringTransactionRollback -Records @($repeatRecord))
    $repeatSecond = @(Invoke-AuthoringTransactionRollback -Records @($repeatRecord))
    Assert-True ($repeatFirst[0].status -eq 'ROLLED_BACK' -and $repeatSecond[0].status -eq 'ALREADY_RESTORED') 'Repeated rollback was not idempotent.'

    $unauthorizedExisting = Join-Path $guardRoot 'unauthorized-existing.xlsx'
    Copy-Item -LiteralPath $guardBase -Destination $unauthorizedExisting
    $unauthorizedHash = Get-AuthoringFileSha256 -Path $unauthorizedExisting
    $unauthorizedFailure = $null
    try { & $importer -DataTablesRoot $sourceRoot -OutputWorkbook $unauthorizedExisting | Out-Null }
    catch { $unauthorizedFailure = $_ }
    Assert-True ($null -ne $unauthorizedFailure -and $unauthorizedFailure.Exception.Message.Contains('[AUTHORING_AUTHORIZATION_REQUIRED]')) 'Existing workbook rebuild was not default-denied.'
    Assert-True ((Get-AuthoringFileSha256 -Path $unauthorizedExisting) -eq $unauthorizedHash) 'Unauthorized rebuild changed the existing workbook.'

    $productionExportFailure = $null
    try { & $exporter -InputWorkbook $workbook -OutputRoot $sourceRoot -BaselineRoot $sourceRoot -ReportDirectory (Join-Path $tempRoot 'reports-production-refusal') | Out-Null }
    catch { $productionExportFailure = $_ }
    Assert-True ($null -ne $productionExportFailure -and $productionExportFailure.Exception.Message.Contains('[AUTHORING_AUTHORIZATION_REQUIRED]')) 'Authoritative CSV export was not default-denied.'

    $quick = & (Join-Path $PSScriptRoot 'Validate-DataTables.ps1') -DataTablesRoot $sourceRoot -Quick -Quiet -NoExit
    Assert-True $quick.isValid 'Shared validator -Quick failed for current DataTables.'
    Assert-True (@($quick.errors).Count -eq 0) 'Shared validator -Quick returned errors.'

    $parseFailures = [System.Collections.Generic.List[string]]::new()
    foreach ($script in @(Get-ChildItem -LiteralPath $authoringRoot -File -Filter '*.ps1') + @(Get-Item -LiteralPath $PSCommandPath)) {
        $parseTokens = $null
        $parseErrors = $null
        [void][System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$parseTokens, [ref]$parseErrors)
        foreach ($parseError in @($parseErrors)) { [void]$parseFailures.Add("$($script.Name): $($parseError.Message)") }
    }
    Assert-True ($parseFailures.Count -eq 0) ([string]::Join('; ', $parseFailures))

    $sourceHashesAfter = Get-CsvHashes -Root $sourceRoot
    Assert-HashMapsEqual -Expected $sourceHashesBefore -Actual $sourceHashesAfter -Message 'Authoritative CSV changed during isolated tests.'

    [pscustomobject]@{
        passed = $true
        sheetCount = 14
        sourceDigest = $sourceDigest.aggregate
        repeatedExportStable = $true
        provenanceCells = $provenanceOne.cellCount
        exactLineEndingProvenance = $true
        contentGuardCases = @($destructiveFixtures.Keys).Count
        formatOnlyAllowed = $true
        explicitBlankStructureAllowed = $true
        narrowBlankFillAllowed = $true
        nonemptyFillRefused = $true
        externalDriftRefused = $true
        existingWorkbookDefaultDenied = $true
        authoritativeCsvDefaultDenied = $true
        rollbackVerified = $true
        partialPublishRollbackVerified = $true
        verificationFailureRollbackVerified = $true
        externalTargetPreserved = $true
        backupDriftPreserved = $true
        candidateDriftRefused = $true
        finalAuditRollbackVerified = $true
        repeatedRollbackIdempotent = $true
        roundExportEvent = 'EXPORTED_ONLY'
        emptySheetPreserved = $true
        glyphFixture = $newGlyph
        sharedValidator = 'PASS'
        authoritativeCsvUnchanged = $true
        formatSourceWindowRefused = $true
        formatStagingAndBackupWindowsRefused = $true
        styleZeroRepaired = $formatReceipt.repairedStyleZero
        formattedCsvUnchanged = $true
        hiddenFormulaAndTextPreserved = $true
        derivedStagingWindowsRefused = $true
    } | ConvertTo-Json -Depth 4
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
