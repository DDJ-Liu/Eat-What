$ErrorActionPreference = 'Stop'

function Get-AuthoringRepositoryRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
}

function Get-AuthoringRuntime {
    $nodePath = $env:CODEX_ARTIFACT_NODE
    $nodeModulesPath = $env:CODEX_ARTIFACT_NODE_MODULES

    if ([string]::IsNullOrWhiteSpace($nodePath)) {
        $nodePath = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
    }
    if ([string]::IsNullOrWhiteSpace($nodeModulesPath)) {
        $nodeModulesPath = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\node\node_modules'
    }

    if (-not (Test-Path -LiteralPath $nodePath -PathType Leaf)) {
        throw "Bundled Node.js was not found at '$nodePath'. Set CODEX_ARTIFACT_NODE explicitly."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $nodeModulesPath '@oai\artifact-tool') -PathType Container)) {
        throw "Bundled @oai/artifact-tool was not found under '$nodeModulesPath'. Set CODEX_ARTIFACT_NODE_MODULES explicitly."
    }

    return [pscustomobject]@{
        Node = [System.IO.Path]::GetFullPath($nodePath)
        NodeModules = [System.IO.Path]::GetFullPath($nodeModulesPath)
    }
}

function Invoke-AuthoringNode {
    param(
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][hashtable]$Arguments
    )

    $runtime = Get-AuthoringRuntime
    $entryPoint = Join-Path $PSScriptRoot 'authoring.mjs'
    $junctionPath = Join-Path $PSScriptRoot 'node_modules'
    $createdJunction = $false

    try {
        if (-not (Test-Path -LiteralPath $junctionPath)) {
            New-Item -ItemType Junction -Path $junctionPath -Target $runtime.NodeModules | Out-Null
            $createdJunction = $true
        }
        elseif (-not (Test-Path -LiteralPath (Join-Path $junctionPath '@oai\artifact-tool') -PathType Container)) {
            throw "Existing '$junctionPath' does not expose @oai/artifact-tool; refusing to replace it."
        }

        $nodeArguments = [System.Collections.Generic.List[string]]::new()
        [void]$nodeArguments.Add($entryPoint)
        [void]$nodeArguments.Add($Command)
        foreach ($key in @($Arguments.Keys | Sort-Object)) {
            $value = $Arguments[$key]
            if ($null -eq $value) { continue }
            [void]$nodeArguments.Add("--$key")
            [void]$nodeArguments.Add([string]$value)
        }

        $output = @(& $runtime.Node @nodeArguments)
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "Authoring command '$Command' failed with exit code $exitCode."
        }
        return @($output)
    }
    finally {
        if ($createdJunction -and (Test-Path -LiteralPath $junctionPath)) {
            [System.IO.Directory]::Delete($junctionPath, $false)
        }
    }
}

function Copy-AuthoringSchemas {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$TargetRoot
    )

    $sourceSchemas = Join-Path $SourceRoot 'Schemas'
    $targetSchemas = Join-Path $TargetRoot 'Schemas'
    if ([System.IO.Path]::GetFullPath($sourceSchemas) -eq [System.IO.Path]::GetFullPath($targetSchemas)) {
        return
    }

    if (Test-Path -LiteralPath $targetSchemas) {
        Remove-Item -LiteralPath $targetSchemas -Recurse -Force
    }
    [System.IO.Directory]::CreateDirectory($targetSchemas) | Out-Null
    Copy-Item -Path (Join-Path $sourceSchemas '*') -Destination $targetSchemas -Recurse -Force
}

function Invoke-AuthoringValidation {
    param(
        [Parameter(Mandatory)][string]$DataTablesRoot,
        [string]$ReportDirectory
    )

    $repositoryRoot = Get-AuthoringRepositoryRoot
    $validator = Join-Path $repositoryRoot 'DevTools\DataTables\Validate-DataTables.ps1'
    $parameters = @{
        DataTablesRoot = $DataTablesRoot
        Quick = $true
        Quiet = $true
        NoExit = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($ReportDirectory)) {
        $parameters.ReportDirectory = $ReportDirectory
    }

    $report = & $validator @parameters
    if (-not $report.isValid) {
        $summary = [string]::Join([Environment]::NewLine, @($report.errors))
        throw "DataTables validation failed.$([Environment]::NewLine)$summary"
    }
    return $report
}

function Get-AuthoringFileSha256 {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Authoring file does not exist: $fullPath"
    }
    return (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Read-AuthoringZipEntryText {
    param(
        [Parameter(Mandatory)][System.IO.Compression.ZipArchive]$Archive,
        [Parameter(Mandatory)][string]$Name,
        [switch]$Optional
    )

    $entry = $Archive.GetEntry($Name.Replace('\', '/'))
    if ($null -eq $entry) {
        if ($Optional) { return $null }
        throw "Workbook archive entry is missing: $Name"
    }
    $reader = [System.IO.StreamReader]::new($entry.Open(), [System.Text.Encoding]::UTF8, $true)
    try { return $reader.ReadToEnd() }
    finally { $reader.Dispose() }
}

function Get-AuthoringWorkbookContentSnapshot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$WorkbookPath)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $fullPath = [System.IO.Path]::GetFullPath($WorkbookPath)
    $sourceSha256 = Get-AuthoringFileSha256 -Path $fullPath
    $archive = [System.IO.Compression.ZipFile]::OpenRead($fullPath)
    try {
        [xml]$workbookXml = Read-AuthoringZipEntryText -Archive $archive -Name 'xl/workbook.xml'
        [xml]$relationsXml = Read-AuthoringZipEntryText -Archive $archive -Name 'xl/_rels/workbook.xml.rels'
        $workbookNs = [System.Xml.XmlNamespaceManager]::new($workbookXml.NameTable)
        $workbookNs.AddNamespace('s', 'http://schemas.openxmlformats.org/spreadsheetml/2006/main')
        $relationNs = [System.Xml.XmlNamespaceManager]::new($relationsXml.NameTable)
        $relationNs.AddNamespace('r', 'http://schemas.openxmlformats.org/package/2006/relationships')
        $relationshipTargets = @{}
        foreach ($relation in @($relationsXml.SelectNodes('/r:Relationships/r:Relationship', $relationNs))) {
            $relationshipTargets[[string]$relation.Id] = [string]$relation.Target
        }

        $sharedStrings = [System.Collections.Generic.List[string]]::new()
        $sharedText = Read-AuthoringZipEntryText -Archive $archive -Name 'xl/sharedStrings.xml' -Optional
        if ($null -ne $sharedText) {
            [xml]$sharedXml = $sharedText
            $sharedNs = [System.Xml.XmlNamespaceManager]::new($sharedXml.NameTable)
            $sharedNs.AddNamespace('s', 'http://schemas.openxmlformats.org/spreadsheetml/2006/main')
            foreach ($item in @($sharedXml.SelectNodes('/s:sst/s:si', $sharedNs))) {
                $parts = @($item.SelectNodes('.//s:t', $sharedNs) | ForEach-Object { $_.InnerText })
                [void]$sharedStrings.Add([string]::Join('', $parts))
            }
        }

        $sheets = [System.Collections.Generic.List[object]]::new()
        $sheetIndex = 0
        foreach ($sheetNode in @($workbookXml.SelectNodes('/s:workbook/s:sheets/s:sheet', $workbookNs))) {
            $sheetIndex++
            $relationshipId = $sheetNode.GetAttribute('id', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships')
            if (-not $relationshipTargets.ContainsKey($relationshipId)) {
                throw "Workbook sheet '$($sheetNode.name)' has no relationship target."
            }
            $target = $relationshipTargets[$relationshipId].Replace('\', '/')
            if ($target.StartsWith('/')) { $entryName = $target.TrimStart('/') }
            elseif ($target.StartsWith('xl/')) { $entryName = $target }
            else { $entryName = "xl/$target" }
            while ($entryName.Contains('/../')) {
                $parts = [System.Collections.Generic.List[string]]::new()
                foreach ($part in $entryName.Split('/')) {
                    if ($part -eq '..') {
                        if ($parts.Count -gt 0) { $parts.RemoveAt($parts.Count - 1) }
                    }
                    elseif ($part -ne '.') { [void]$parts.Add($part) }
                }
                $entryName = [string]::Join('/', $parts)
            }

            [xml]$sheetXml = Read-AuthoringZipEntryText -Archive $archive -Name $entryName
            $sheetNs = [System.Xml.XmlNamespaceManager]::new($sheetXml.NameTable)
            $sheetNs.AddNamespace('s', 'http://schemas.openxmlformats.org/spreadsheetml/2006/main')
            $cells = [System.Collections.Generic.List[object]]::new()
            $dimensionNode = $sheetXml.SelectSingleNode('/s:worksheet/s:dimension', $sheetNs)
            $dimensionRef = if ($null -eq $dimensionNode) { $null } else { [string]$dimensionNode.ref }
            $rowNumbers = @($sheetXml.SelectNodes('/s:worksheet/s:sheetData/s:row', $sheetNs) | ForEach-Object { [int]$_.r })
            $storedCellAddresses = [System.Collections.Generic.List[string]]::new()
            foreach ($cellNode in @($sheetXml.SelectNodes('/s:worksheet/s:sheetData/s:row/s:c', $sheetNs))) {
                $address = [string]$cellNode.r
                [void]$storedCellAddresses.Add($address)
                $rawType = [string]$cellNode.t
                if ([string]::IsNullOrWhiteSpace($rawType)) { $rawType = 'n' }
                $valueNode = $cellNode.SelectSingleNode('s:v', $sheetNs)
                $formulaNode = $cellNode.SelectSingleNode('s:f', $sheetNs)
                $inlineNode = $cellNode.SelectSingleNode('s:is', $sheetNs)
                $rawValue = if ($null -eq $valueNode) { $null } else { [string]$valueNode.InnerText }
                $formula = if ($null -eq $formulaNode) { $null } else { [string]$formulaNode.InnerText }
                $formulaAttributes = [ordered]@{}
                if ($null -ne $formulaNode) {
                    foreach ($attribute in @($formulaNode.Attributes | Sort-Object Name)) {
                        $formulaAttributes[[string]$attribute.Name] = [string]$attribute.Value
                    }
                }

                $valueType = $rawType
                $semanticValue = $rawValue
                switch ($rawType) {
                    's' {
                        $valueType = 'sharedString'
                        $sharedIndex = -1
                        if ([int]::TryParse([string]$rawValue, [ref]$sharedIndex) -and $sharedIndex -ge 0 -and $sharedIndex -lt $sharedStrings.Count) {
                            $semanticValue = $sharedStrings[$sharedIndex]
                        }
                    }
                    'inlineStr' {
                        $valueType = 'inlineString'
                        $semanticValue = if ($null -eq $inlineNode) { '' } else {
                            [string]::Join('', @($inlineNode.SelectNodes('.//s:t', $sheetNs) | ForEach-Object { $_.InnerText }))
                        }
                    }
                    'str' { $valueType = 'formulaString' }
                    'b' { $valueType = 'boolean' }
                    'e' { $valueType = 'error' }
                    'd' { $valueType = 'date' }
                    'n' { $valueType = 'number' }
                }
                $hasContent = -not [string]::IsNullOrEmpty([string]$semanticValue) -or $null -ne $formulaNode
                if (-not $hasContent) { continue }

                $column = if ($address -match '^([A-Z]+)') { $Matches[1] } else { $null }
                $row = if ($address -match '([0-9]+)$') { [int]$Matches[1] } else { $null }
                $content = [ordered]@{
                    rawType = $rawType
                    valueType = $valueType
                    value = $semanticValue
                    formula = $formula
                    formulaAttributes = [pscustomobject]$formulaAttributes
                    cachedValue = $rawValue
                }
                $fingerprintJson = $content | ConvertTo-Json -Depth 8 -Compress
                $fingerprintBytes = [System.Text.Encoding]::UTF8.GetBytes($fingerprintJson)
                $fingerprintHash = [System.Security.Cryptography.SHA256]::HashData($fingerprintBytes)
                [void]$cells.Add([pscustomobject][ordered]@{
                    cell = $address
                    row = $row
                    column = $column
                    rawType = $rawType
                    valueType = $valueType
                    value = $semanticValue
                    formula = $formula
                    formulaAttributes = [pscustomobject]$formulaAttributes
                    cachedValue = $rawValue
                    fingerprint = [Convert]::ToHexString($fingerprintHash)
                })
            }
            [void]$sheets.Add([pscustomobject][ordered]@{
                name = [string]$sheetNode.name
                order = $sheetIndex
                state = if ([string]::IsNullOrWhiteSpace([string]$sheetNode.state)) { 'visible' } else { [string]$sheetNode.state }
                dimensionRef = $dimensionRef
                rowNumbers = $rowNumbers
                storedCellAddresses = $storedCellAddresses.ToArray()
                cells = $cells.ToArray()
            })
        }

        $contentObject = [ordered]@{ sheets = @($sheets | ForEach-Object { [ordered]@{ name=$_.name; order=$_.order; state=$_.state; cells=$_.cells } }) }
        $contentJson = $contentObject | ConvertTo-Json -Depth 20 -Compress
        $contentHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($contentJson)))
        $structureObject = [ordered]@{ sheets = @($sheets | ForEach-Object { [ordered]@{ name=$_.name; order=$_.order; state=$_.state; dimensionRef=$_.dimensionRef; rowNumbers=$_.rowNumbers; storedCellAddresses=$_.storedCellAddresses } }) }
        $structureJson = $structureObject | ConvertTo-Json -Depth 12 -Compress
        $structureHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($structureJson)))
        return [pscustomobject][ordered]@{
            schemaVersion = 1
            sourcePath = $fullPath
            sourceSha256 = $sourceSha256
            contentSha256 = $contentHash
            structureSha256 = $structureHash
            sheetCount = $sheets.Count
            nonEmptyCellCount = @($sheets | ForEach-Object { @($_.cells).Count } | Measure-Object -Sum).Sum
            sheets = $sheets.ToArray()
        }
    }
    finally { $archive.Dispose() }
}

function Compare-AuthoringWorkbookStructure {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$BaselineSnapshot,
        [Parameter(Mandatory)]$CandidateSnapshot
    )

    $differences = [System.Collections.Generic.List[object]]::new()
    $baselineSheets = @{}
    $candidateSheets = @{}
    foreach ($sheet in @($BaselineSnapshot.sheets)) { $baselineSheets[[string]$sheet.name] = $sheet }
    foreach ($sheet in @($CandidateSnapshot.sheets)) { $candidateSheets[[string]$sheet.name] = $sheet }
    foreach ($name in @($baselineSheets.Keys + $candidateSheets.Keys | Sort-Object -Unique)) {
        if (-not $baselineSheets.ContainsKey($name) -or -not $candidateSheets.ContainsKey($name)) { continue }
        $before = $baselineSheets[$name]
        $after = $candidateSheets[$name]
        foreach ($field in @('dimensionRef','rowNumbers','storedCellAddresses')) {
            $beforeJson = $before.$field | ConvertTo-Json -Depth 6 -Compress
            $afterJson = $after.$field | ConvertTo-Json -Depth 6 -Compress
            if ($beforeJson -cne $afterJson) {
                [void]$differences.Add([pscustomobject]@{ kind='BLANK_STRUCTURE_CHANGED'; sheet=$name; field=$field; before=$before.$field; after=$after.$field })
            }
        }
    }
    return [pscustomobject][ordered]@{
        isEqual = ($differences.Count -eq 0)
        baselineStructureSha256 = [string]$BaselineSnapshot.structureSha256
        candidateStructureSha256 = [string]$CandidateSnapshot.structureSha256
        differenceCount = $differences.Count
        differences = $differences.ToArray()
    }
}

function Write-AuthoringWorkbookContentSnapshot {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$WorkbookPath,
        [Parameter(Mandatory)][string]$OutputPath
    )

    $snapshot = Get-AuthoringWorkbookContentSnapshot -WorkbookPath $WorkbookPath
    $fullOutput = [System.IO.Path]::GetFullPath($OutputPath)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullOutput)) | Out-Null
    [System.IO.File]::WriteAllText($fullOutput, ($snapshot | ConvertTo-Json -Depth 20), [System.Text.UTF8Encoding]::new($false))
    return $snapshot
}

function Compare-AuthoringWorkbookContent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$BaselineSnapshot,
        [Parameter(Mandatory)]$CandidateSnapshot
    )

    $differences = [System.Collections.Generic.List[object]]::new()
    $baselineSheets = @{}
    $candidateSheets = @{}
    foreach ($sheet in @($BaselineSnapshot.sheets)) { $baselineSheets[[string]$sheet.name] = $sheet }
    foreach ($sheet in @($CandidateSnapshot.sheets)) { $candidateSheets[[string]$sheet.name] = $sheet }
    foreach ($name in @($baselineSheets.Keys + $candidateSheets.Keys | Sort-Object -Unique)) {
        if (-not $baselineSheets.ContainsKey($name)) {
            [void]$differences.Add([pscustomobject]@{ kind='SHEET_ADDED'; sheet=$name; cell=$null; before=$null; after=$candidateSheets[$name] })
            continue
        }
        if (-not $candidateSheets.ContainsKey($name)) {
            [void]$differences.Add([pscustomobject]@{ kind='SHEET_REMOVED'; sheet=$name; cell=$null; before=$baselineSheets[$name]; after=$null })
            continue
        }
        $beforeSheet = $baselineSheets[$name]
        $afterSheet = $candidateSheets[$name]
        if ([int]$beforeSheet.order -ne [int]$afterSheet.order -or [string]$beforeSheet.state -cne [string]$afterSheet.state) {
            [void]$differences.Add([pscustomobject]@{
                kind='SHEET_ASSOCIATION_CHANGED'; sheet=$name; cell=$null
                before=[pscustomobject]@{ order=$beforeSheet.order; state=$beforeSheet.state }
                after=[pscustomobject]@{ order=$afterSheet.order; state=$afterSheet.state }
            })
        }
        $beforeCells = @{}
        $afterCells = @{}
        foreach ($cell in @($beforeSheet.cells)) { $beforeCells[[string]$cell.cell] = $cell }
        foreach ($cell in @($afterSheet.cells)) { $afterCells[[string]$cell.cell] = $cell }
        foreach ($address in @($beforeCells.Keys + $afterCells.Keys | Sort-Object -Unique)) {
            if (-not $beforeCells.ContainsKey($address)) {
                [void]$differences.Add([pscustomobject]@{ kind='CELL_ADDED'; sheet=$name; cell=$address; before=$null; after=$afterCells[$address] })
            }
            elseif (-not $afterCells.ContainsKey($address)) {
                [void]$differences.Add([pscustomobject]@{ kind='CELL_REMOVED'; sheet=$name; cell=$address; before=$beforeCells[$address]; after=$null })
            }
            elseif ([string]$beforeCells[$address].fingerprint -cne [string]$afterCells[$address].fingerprint) {
                [void]$differences.Add([pscustomobject]@{ kind='CELL_CHANGED'; sheet=$name; cell=$address; before=$beforeCells[$address]; after=$afterCells[$address] })
            }
        }
    }
    return [pscustomobject][ordered]@{
        isEqual = ($differences.Count -eq 0)
        baselineContentSha256 = [string]$BaselineSnapshot.contentSha256
        candidateContentSha256 = [string]$CandidateSnapshot.contentSha256
        differenceCount = $differences.Count
        differences = $differences.ToArray()
    }
}

function Add-AuthoringAuditRecord {
    param(
        [Parameter(Mandatory)][string]$AuditPath,
        [Parameter(Mandatory)]$Record
    )
    $fullPath = [System.IO.Path]::GetFullPath($AuditPath)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullPath)) | Out-Null
    $line = ($Record | ConvertTo-Json -Depth 20 -Compress) + [Environment]::NewLine
    [System.IO.File]::AppendAllText($fullPath, $line, [System.Text.UTF8Encoding]::new($false))
}

function Test-AuthoringPathWithinRoot {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Root
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    return $fullPath -eq $fullRoot -or $fullPath.StartsWith($fullRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-AuthoringFaultInjectionScope {
    param(
        [scriptblock]$TestFaultInjector,
        [string]$TestFixtureRoot,
        [Parameter(Mandatory)][string[]]$Paths
    )

    if ($null -eq $TestFaultInjector) { return }
    if ([string]::IsNullOrWhiteSpace($TestFixtureRoot)) {
        throw '[AUTHORING_TEST_SCOPE_REQUIRED] Fault injection requires an explicit isolated fixture root.'
    }
    foreach ($path in $Paths) {
        if (-not (Test-AuthoringPathWithinRoot -Path $path -Root $TestFixtureRoot)) {
            throw "[AUTHORING_TEST_SCOPE_VIOLATION] Fault injection path is outside the isolated fixture root: $path"
        }
    }
}

function Invoke-AuthoringTestFault {
    param(
        [scriptblock]$TestFaultInjector,
        [Parameter(Mandatory)][string]$Phase,
        $Record,
        [int]$Index,
        $Context
    )

    if ($null -ne $TestFaultInjector) {
        & $TestFaultInjector $Phase $Record $Index $Context
    }
}

function Get-AuthoringOptionalFileState {
    param([Parameter(Mandatory)][string]$Path)

    $exists = Test-Path -LiteralPath $Path -PathType Leaf
    return [pscustomobject]@{
        exists = $exists
        sha256 = if ($exists) { Get-AuthoringFileSha256 -Path $Path } else { $null }
    }
}

function Test-AuthoringBlankFillOnly {
    param(
        [Parameter(Mandatory)]$BaselineSnapshot,
        [Parameter(Mandatory)]$ContentDiff
    )

    $sheets = @{}
    foreach ($sheet in @($BaselineSnapshot.sheets)) { $sheets[[string]$sheet.name] = $sheet }
    foreach ($difference in @($ContentDiff.differences)) {
        if ([string]$difference.kind -cne 'CELL_ADDED' -or -not $sheets.ContainsKey([string]$difference.sheet)) { return $false }
        $knownBlankAddresses = @($sheets[[string]$difference.sheet].storedCellAddresses)
        if ($knownBlankAddresses -notcontains [string]$difference.cell) { return $false }
    }
    return $true
}

# One physical write gate shared by workbook and derived CSV publication.
# The mutex serializes cooperating publishers; held file handles deny byte writes.
# Delete sharing is required by atomic Replace, so displaced bytes are also verified
# and preserved if a non-cooperating process renames the target in the final window.
function Invoke-AuthoringGuardedWrite {
    param($Record, [string]$Stage, $Touched, [scriptblock]$TestFaultInjector, [int]$Index, $Context)
    $Record | Add-Member -NotePropertyName stagingPath -NotePropertyValue $Stage -Force
    Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterStaging' -Record $Record -Index $Index -Context $Context
    $key = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes([IO.Path]::GetFullPath($Record.target).ToUpperInvariant())))
    $mutex = [Threading.Mutex]::new($false, "Local\EatWhatAuthoring-$key")
    $held = $false
    $sourceHandle = $null; $stageHandle = $null; $backupHandle = $null
    $ownedBackup = $false
    $displaced = "$($Record.target).displaced-$([guid]::NewGuid().ToString('N')).tmp"
    try {
        try { $held = $mutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $held = $true }
        if (-not $held) { throw '[AUTHORING_WRITE_BUSY] Another publisher owns this target.' }
        $stageHandle = [IO.File]::Open($Stage, 'Open', 'Read', ([IO.FileShare]::Read -bor [IO.FileShare]::Delete))
        if ($Record.targetExisted) { $sourceHandle = [IO.File]::Open($Record.target, 'Open', 'Read', ([IO.FileShare]::Read -bor [IO.FileShare]::Delete)) }
        $current = Get-AuthoringOptionalFileState -Path $Record.target
        if ($current.exists -ne [bool]$Record.targetExisted -or $current.sha256 -ne $Record.targetBeforeSha256) { throw '[AUTHORING_EXTERNAL_DRIFT] Target changed during staging; refusing physical write.' }
        if ((Get-AuthoringFileSha256 -Path $Stage) -ne $Record.candidateSha256) { throw '[AUTHORING_STAGING_MISMATCH] Staging bytes changed.' }
        if ((Get-AuthoringFileSha256 -Path $Record.candidate) -ne $Record.candidateSha256) { throw '[AUTHORING_CANDIDATE_DRIFT] Candidate changed during staging.' }
        if (Test-Path -LiteralPath $Record.backup) { throw '[AUTHORING_BACKUP_EXISTS] Late backup conflict; preserving unknown backup.' }
        if ($Record.targetExisted) {
            # CreateNew is atomic and never overwrites a late backup; keep it locked
            # throughout replacement instead of passing it as File.Replace's backup.
            $backupHandle = [IO.File]::Open($Record.backup, 'CreateNew', 'ReadWrite', 'Read')
            $ownedBackup = $true
            $sourceHandle.CopyTo($backupHandle); $backupHandle.Flush($true)
            $backupHandle.Position = 0
            if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($backupHandle)) -ne $Record.targetBeforeSha256) { throw '[AUTHORING_EXTERNAL_DRIFT] Backup does not match frozen source.' }
            if ((Get-AuthoringFileSha256 -Path $Record.target) -ne $Record.targetBeforeSha256) { throw '[AUTHORING_EXTERNAL_DRIFT] Target changed before physical replace.' }
        }
        # File.Replace on Windows needs these names free of our own read handles.
        # Keep the named mutex and the exclusive backup reservation across Replace;
        # verify the displaced target to detect non-cooperating rename races.
        if ($null -ne $sourceHandle) { $sourceHandle.Dispose(); $sourceHandle = $null }
        $stageHandle.Dispose(); $stageHandle = $null
        $Record.writeAttempted = $true; $Record.state = 'WRITE_ATTEMPTED'
        [void]$Touched.Add($Record)
        if ($Record.targetExisted) {
            [IO.File]::Replace($Stage, $Record.target, $displaced, $true)
            if ((Get-AuthoringFileSha256 -Path $displaced) -ne $Record.targetBeforeSha256) {
                $Record | Add-Member -NotePropertyName externalRenameEvidence -NotePropertyValue $displaced
                throw '[AUTHORING_EXTERNAL_RENAME_RACE] Displaced bytes are not the authorized baseline; preserving evidence, refusing success.'
            }
            [IO.File]::Delete($displaced)
        } else { [IO.File]::Move($Stage, $Record.target, $false) }
        $Record.state = 'WRITTEN'
    }
    finally {
        if ($null -ne $backupHandle) { $backupHandle.Dispose() }
        if ($null -ne $sourceHandle) { $sourceHandle.Dispose() }
        if ($null -ne $stageHandle) { $stageHandle.Dispose() }
        if ($ownedBackup -and -not $Record.writeAttempted -and (Test-Path -LiteralPath $Record.backup) -and (Get-AuthoringFileSha256 -Path $Record.backup) -eq $Record.targetBeforeSha256) { [IO.File]::Delete($Record.backup) }
        if ($held) { $mutex.ReleaseMutex() }; $mutex.Dispose()
    }
}

function Invoke-AuthoringTransactionRollback {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object[]]$Records,
        [scriptblock]$TestFaultInjector,
        $Context
    )

    $results = [System.Collections.Generic.List[object]]::new()
    for ($index = $Records.Count - 1; $index -ge 0; $index--) {
        $record = $Records[$index]
        $result = [ordered]@{ relativePath=$record.relativePath; status='PENDING'; error=$null; observedTargetSha256=$null; observedBackupSha256=$null }
        try {
            Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'BeforeRollbackRecord' -Record $record -Index $index -Context $Context
            if ($null -ne $record.PSObject.Properties['externalRenameEvidence']) { throw "[AUTHORING_ROLLBACK_EXTERNAL_RACE] Preserving target and external bytes at $($record.externalRenameEvidence); manual recovery required." }
            $targetState = Get-AuthoringOptionalFileState -Path $record.target
            $backupState = Get-AuthoringOptionalFileState -Path $record.backup
            $result.observedTargetSha256 = $targetState.sha256
            $result.observedBackupSha256 = $backupState.sha256

            if ([bool]$record.targetExisted) {
                if ($targetState.exists -and $targetState.sha256 -eq $record.targetBeforeSha256) {
                    $result.status = 'ALREADY_RESTORED'
                }
                elseif (-not $targetState.exists -or $targetState.sha256 -ne $record.candidateSha256) {
                    throw '[AUTHORING_ROLLBACK_TARGET_DRIFT] Current target is not the task-published candidate; preserving it.'
                }
                elseif (-not $backupState.exists) {
                    throw '[AUTHORING_ROLLBACK_BACKUP_MISSING] Required backup is missing; preserving the target.'
                }
                elseif ($backupState.sha256 -ne $record.targetBeforeSha256) {
                    throw '[AUTHORING_ROLLBACK_BACKUP_DRIFT] Backup bytes differ from the frozen target baseline; preserving both files.'
                }
                else {
                    $restore = "$($record.target).restore-$([guid]::NewGuid().ToString('N')).tmp"
                    $discard = "$($record.target).rollback-discard-$([guid]::NewGuid().ToString('N')).tmp"
                    try {
                        Copy-Item -LiteralPath $record.backup -Destination $restore
                        if ((Get-AuthoringFileSha256 -Path $restore) -ne $record.targetBeforeSha256) { throw '[AUTHORING_ROLLBACK_STAGE_MISMATCH] Restore staging bytes changed.' }
                        $currentBeforeReplace = Get-AuthoringFileSha256 -Path $record.target
                        $backupBeforeReplace = Get-AuthoringFileSha256 -Path $record.backup
                        if ($currentBeforeReplace -ne $record.candidateSha256) { throw '[AUTHORING_ROLLBACK_TARGET_DRIFT] Target changed during rollback.' }
                        if ($backupBeforeReplace -ne $record.targetBeforeSha256) { throw '[AUTHORING_ROLLBACK_BACKUP_DRIFT] Backup changed during rollback.' }
                        [System.IO.File]::Replace($restore, $record.target, $discard, $true)
                    }
                    finally {
                        if (Test-Path -LiteralPath $restore) { Remove-Item -LiteralPath $restore -Force }
                        if (Test-Path -LiteralPath $discard) { Remove-Item -LiteralPath $discard -Force }
                    }
                    if ((Get-AuthoringFileSha256 -Path $record.target) -ne $record.targetBeforeSha256) { throw '[AUTHORING_ROLLBACK_VERIFY_FAILED] Restored target differs from the frozen baseline.' }
                    $result.status = 'ROLLED_BACK'
                }
            }
            elseif (-not $targetState.exists) {
                $result.status = 'ALREADY_REMOVED'
            }
            elseif ($targetState.sha256 -ne $record.candidateSha256) {
                throw '[AUTHORING_ROLLBACK_TARGET_DRIFT] New target no longer belongs to this task; preserving it.'
            }
            else {
                Remove-Item -LiteralPath $record.target -Force
                if (Test-Path -LiteralPath $record.target) { throw '[AUTHORING_ROLLBACK_VERIFY_FAILED] New target still exists after rollback.' }
                $result.status = 'ROLLED_BACK'
            }
            $record.state = $result.status
            Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterRollbackRecord' -Record $record -Index $index -Context $Context
        }
        catch {
            $result.status = 'ROLLBACK_BLOCKED'
            $result.error = $_.Exception.Message
            $record.state = 'ROLLBACK_BLOCKED'
        }
        [void]$results.Add([pscustomobject]$result)
    }
    return $results.ToArray()
}

function New-AuthoringPublishFailureException {
    param(
        [Parameter(Mandatory)][System.Exception]$PrimaryException,
        [Parameter(Mandatory)][object[]]$RollbackResults,
        [string]$AuditError
    )

    $rollbackErrors = @($RollbackResults | Where-Object { $_.status -eq 'ROLLBACK_BLOCKED' })
    $rollbackSummary = if ($rollbackErrors.Count -eq 0) { 'complete' } else { "incomplete ($($rollbackErrors.Count) blocked)" }
    $message = "[AUTHORING_PUBLISH_FAILED] $($PrimaryException.Message) Rollback: $rollbackSummary."
    if (-not [string]::IsNullOrWhiteSpace($AuditError)) { $message += " Failure audit: $AuditError" }
    $exception = [System.InvalidOperationException]::new($message, $PrimaryException)
    $exception.Data['PrimaryError'] = $PrimaryException.Message
    $exception.Data['RollbackResults'] = ($RollbackResults | ConvertTo-Json -Depth 12 -Compress)
    if (-not [string]::IsNullOrWhiteSpace($AuditError)) { $exception.Data['AuditError'] = $AuditError }
    return $exception
}

function Publish-AuthoringWorkbookChange {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SourceWorkbook,
        [Parameter(Mandatory)][string]$CandidateWorkbook,
        [Parameter(Mandatory)][ValidateSet('FormatOrCommentOnly','BlankStructureOnly','FillPreviouslyBlankCells','AuthorizedContent')][string]$Mode,
        [Parameter(Mandatory)][string]$ExpectedSourceSha256,
        [Parameter(Mandatory)][string]$BackupPath,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$TaskOrigin,
        [Parameter(Mandatory)][string]$AuditPath,
        [scriptblock]$TestFaultInjector,
        [string]$TestFixtureRoot
    )

    $source = [System.IO.Path]::GetFullPath($SourceWorkbook)
    $candidate = [System.IO.Path]::GetFullPath($CandidateWorkbook)
    $backup = [System.IO.Path]::GetFullPath($BackupPath)
    if ($source -eq $candidate) { throw '[AUTHORING_INVALID_TARGET] Candidate workbook must differ from source workbook.' }
    Assert-AuthoringFaultInjectionScope -TestFaultInjector $TestFaultInjector -TestFixtureRoot $TestFixtureRoot -Paths @($source, $candidate, $backup, $AuditPath)
    if (Test-Path -LiteralPath $backup) { throw "[AUTHORING_BACKUP_EXISTS] Refusing to overwrite backup: $backup" }
    $expected = $ExpectedSourceSha256.ToUpperInvariant()
    $observed = Get-AuthoringFileSha256 -Path $source
    if ($observed -ne $expected) {
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record ([ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_PUBLISH_REFUSED'; reason='EXTERNAL_DRIFT'; taskOrigin=$TaskOrigin; source=$source; expectedSourceSha256=$expected; observedSourceSha256=$observed })
        throw "[AUTHORING_EXTERNAL_DRIFT] Workbook changed before evaluation; expected $expected, observed $observed."
    }
    $candidateSha256 = Get-AuthoringFileSha256 -Path $candidate
    $baselineSnapshot = Get-AuthoringWorkbookContentSnapshot -WorkbookPath $source
    $candidateSnapshot = Get-AuthoringWorkbookContentSnapshot -WorkbookPath $candidate
    if ($candidateSnapshot.sourceSha256 -ne $candidateSha256 -or (Get-AuthoringFileSha256 -Path $candidate) -ne $candidateSha256) {
        throw '[AUTHORING_CANDIDATE_DRIFT] Candidate changed while its protected content snapshot was captured.'
    }
    $diff = Compare-AuthoringWorkbookContent -BaselineSnapshot $baselineSnapshot -CandidateSnapshot $candidateSnapshot
    $structureDiff = Compare-AuthoringWorkbookStructure -BaselineSnapshot $baselineSnapshot -CandidateSnapshot $candidateSnapshot
    $blankFillAllowed = $Mode -eq 'FillPreviouslyBlankCells' -and $structureDiff.isEqual -and (Test-AuthoringBlankFillOnly -BaselineSnapshot $baselineSnapshot -ContentDiff $diff)
    if ($Mode -ne 'AuthorizedContent' -and -not $diff.isEqual -and -not $blankFillAllowed) {
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record ([ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_PUBLISH_REFUSED'; reason='NONEMPTY_CONTENT_CHANGE'; taskOrigin=$TaskOrigin; mode=$Mode; source=$source; candidate=$candidate; differenceCount=$diff.differenceCount; differences=$diff.differences })
        throw "[AUTHORING_CONTENT_PROTECTED] $($diff.differenceCount) non-empty cell/sheet association change(s) are not allowed in mode '$Mode'."
    }

    $secondObserved = Get-AuthoringFileSha256 -Path $source
    if ($secondObserved -ne $expected) {
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record ([ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_PUBLISH_REFUSED'; reason='EXTERNAL_DRIFT_PREWRITE'; taskOrigin=$TaskOrigin; source=$source; expectedSourceSha256=$expected; observedSourceSha256=$secondObserved })
        throw "[AUTHORING_EXTERNAL_DRIFT] Workbook changed before atomic replace; expected $expected, observed $secondObserved."
    }

    Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterCandidateFreeze' -Record $null -Index -1 -Context $null
    if ((Get-AuthoringFileSha256 -Path $candidate) -ne $candidateSha256) { throw '[AUTHORING_CANDIDATE_DRIFT] Candidate changed before workbook publish.' }
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $backup)) | Out-Null
    $staging = Join-Path (Split-Path -Parent $source) ('.' + [System.IO.Path]::GetFileName($source) + '.authoring-' + [guid]::NewGuid().ToString('N') + '.tmp')
    $record = [pscustomobject][ordered]@{ relativePath=[System.IO.Path]::GetFileName($source); target=$source; targetExisted=$true; targetBeforeSha256=$expected; candidate=$candidate; candidateSha256=$candidateSha256; backup=$backup; state='PLANNED'; writeAttempted=$false }
    $touched = [System.Collections.Generic.List[object]]::new()
    try {
        Copy-Item -LiteralPath $candidate -Destination $staging
        if ((Get-AuthoringFileSha256 -Path $staging) -ne $candidateSha256) { throw '[AUTHORING_STAGING_MISMATCH] Candidate staging copy changed.' }
        if ((Get-AuthoringFileSha256 -Path $candidate) -ne $candidateSha256) { throw '[AUTHORING_CANDIDATE_DRIFT] Candidate changed after staging.' }
        Invoke-AuthoringGuardedWrite -Record $record -Stage $staging -Touched $touched -TestFaultInjector $TestFaultInjector -Index 0 -Context $null
        Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterWriteBeforeVerify' -Record $record -Index 0 -Context $null
        $publishedSha256 = Get-AuthoringFileSha256 -Path $source
        if ($publishedSha256 -ne $candidateSha256) { throw '[AUTHORING_PUBLISH_VERIFY_FAILED] Published workbook hash differs from candidate.' }
        $record.state = 'VERIFIED'
        Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterAllWritesBeforeAudit' -Record $record -Index 0 -Context $null
        $record = [ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_PUBLISHED'; taskOrigin=$TaskOrigin; mode=$Mode; source=$source; candidate=$candidate; sourceBeforeSha256=$expected; sourceAfterSha256=$publishedSha256; backup=$backup; contentDifferenceCount=$diff.differenceCount; structureDifferenceCount=$structureDiff.differenceCount; structureDifferences=$structureDiff.differences; baselineContentSha256=$diff.baselineContentSha256; candidateContentSha256=$diff.candidateContentSha256; baselineStructureSha256=$structureDiff.baselineStructureSha256; candidateStructureSha256=$structureDiff.candidateStructureSha256 }
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record $record
        return [pscustomobject]$record
    }
    catch {
        $primary = $_.Exception
        Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'BeforeRollback' -Record $record -Index 0 -Context $null
        $rollbackResults = @(Invoke-AuthoringTransactionRollback -Records $touched.ToArray() -TestFaultInjector $TestFaultInjector -Context $null)
        $auditError = $null
        try { Add-AuthoringAuditRecord -AuditPath $AuditPath -Record ([ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_PUBLISH_FAILED'; taskOrigin=$TaskOrigin; mode=$Mode; source=$source; candidate=$candidate; primaryError=$primary.Message; rollback=$rollbackResults }) }
        catch { $auditError = $_.Exception.Message }
        throw (New-AuthoringPublishFailureException -PrimaryException $primary -RollbackResults $rollbackResults -AuditError $auditError)
    }
    finally { if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Force } }
}

function Restore-AuthoringWorkbookBackup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TargetWorkbook,
        [Parameter(Mandatory)][string]$BackupPath,
        [Parameter(Mandatory)][string]$ExpectedCurrentSha256,
        [Parameter(Mandatory)][string]$RollbackBackupPath,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$TaskOrigin,
        [Parameter(Mandatory)][string]$AuditPath
    )

    $target = [System.IO.Path]::GetFullPath($TargetWorkbook)
    $backup = [System.IO.Path]::GetFullPath($BackupPath)
    $rollbackBackup = [System.IO.Path]::GetFullPath($RollbackBackupPath)
    $observed = Get-AuthoringFileSha256 -Path $target
    if ($observed -ne $ExpectedCurrentSha256.ToUpperInvariant()) {
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record ([ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_ROLLBACK_REFUSED'; reason='EXTERNAL_DRIFT'; taskOrigin=$TaskOrigin; target=$target; expectedCurrentSha256=$ExpectedCurrentSha256; observedCurrentSha256=$observed })
        throw '[AUTHORING_EXTERNAL_DRIFT] Workbook changed before rollback.'
    }
    if (-not (Test-Path -LiteralPath $backup -PathType Leaf)) { throw "Workbook backup does not exist: $backup" }
    if (Test-Path -LiteralPath $rollbackBackup) { throw "[AUTHORING_BACKUP_EXISTS] Refusing to overwrite rollback backup: $rollbackBackup" }
    $backupSha256 = Get-AuthoringFileSha256 -Path $backup
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $rollbackBackup)) | Out-Null
    $stage = "$target.rollback-$([guid]::NewGuid().ToString('N')).tmp"
    try {
        Copy-Item -LiteralPath $backup -Destination $stage
        if ((Get-AuthoringFileSha256 -Path $stage) -ne $backupSha256) { throw '[AUTHORING_ROLLBACK_STAGE_MISMATCH] Backup staging bytes changed.' }
        if ((Get-AuthoringFileSha256 -Path $target) -ne $observed) { throw '[AUTHORING_EXTERNAL_DRIFT] Workbook changed during rollback.' }
        if ((Get-AuthoringFileSha256 -Path $backup) -ne $backupSha256) { throw '[AUTHORING_ROLLBACK_BACKUP_DRIFT] Workbook backup changed during rollback.' }
        [System.IO.File]::Replace($stage, $target, $rollbackBackup, $true)
        $record = [ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='WORKBOOK_ROLLED_BACK'; taskOrigin=$TaskOrigin; target=$target; restoredFrom=$backup; replacedSha256=$observed; restoredSha256=(Get-AuthoringFileSha256 -Path $target); rollbackBackup=$rollbackBackup }
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record $record
        return [pscustomobject]$record
    }
    finally { if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Force } }
}

function New-AuthoringDerivedPublishPlan {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TargetRoot,
        [Parameter(Mandatory)][string[]]$RelativePaths,
        [Parameter(Mandatory)][string]$BackupDirectory
    )

    $targetRootFull = [System.IO.Path]::GetFullPath($TargetRoot).TrimEnd('\', '/')
    $backupRootFull = [System.IO.Path]::GetFullPath($BackupDirectory).TrimEnd('\', '/')
    $records = [System.Collections.Generic.List[object]]::new()
    foreach ($relativePathRaw in @($RelativePaths | Sort-Object -Unique)) {
        $relativePath = [string]$relativePathRaw
        if ([System.IO.Path]::IsPathRooted($relativePath) -or $relativePath.Replace('\','/').Split('/') -contains '..') { throw "[AUTHORING_UNSAFE_PATH] Derived path must remain relative: $relativePath" }
        $target = [System.IO.Path]::GetFullPath((Join-Path $targetRootFull $relativePath))
        $backup = [System.IO.Path]::GetFullPath((Join-Path $backupRootFull ($relativePath + '.backup')))
        if (-not (Test-AuthoringPathWithinRoot -Path $target -Root $targetRootFull)) { throw "[AUTHORING_UNSAFE_PATH] Target escaped root: $relativePath" }
        if (-not (Test-AuthoringPathWithinRoot -Path $backup -Root $backupRootFull)) { throw "[AUTHORING_UNSAFE_PATH] Backup escaped root: $relativePath" }
        if (Test-Path -LiteralPath $backup) { throw "[AUTHORING_BACKUP_EXISTS] Refusing to overwrite backup: $backup" }
        $targetState = Get-AuthoringOptionalFileState -Path $target
        [void]$records.Add([pscustomobject][ordered]@{ relativePath=$relativePath.Replace('\','/'); target=$target; targetExisted=$targetState.exists; targetBeforeSha256=$targetState.sha256; backup=$backup; backupExisted=$false })
    }
    return [pscustomobject][ordered]@{ schemaVersion=1; createdAt=[DateTimeOffset]::Now.ToString('o'); targetRoot=$targetRootFull; backupRoot=$backupRootFull; records=$records.ToArray() }
}

function Publish-AuthoringDerivedFileSet {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$CandidateRoot,
        [Parameter(Mandatory)][string]$TargetRoot,
        [Parameter(Mandatory)][string[]]$RelativePaths,
        [Parameter(Mandatory)][string]$BackupDirectory,
        [Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$TaskOrigin,
        [Parameter(Mandatory)][string]$AuditPath,
        [string]$ProvenancePath,
        $PublishPlan,
        [scriptblock]$TestFaultInjector,
        [string]$TestFixtureRoot
    )

    $candidateRootFull = [System.IO.Path]::GetFullPath($CandidateRoot).TrimEnd('\', '/')
    $targetRootFull = [System.IO.Path]::GetFullPath($TargetRoot).TrimEnd('\', '/')
    $backupRootFull = [System.IO.Path]::GetFullPath($BackupDirectory).TrimEnd('\', '/')
    Assert-AuthoringFaultInjectionScope -TestFaultInjector $TestFaultInjector -TestFixtureRoot $TestFixtureRoot -Paths @($candidateRootFull, $targetRootFull, $backupRootFull, $AuditPath)
    if ($null -eq $PublishPlan) { $PublishPlan = New-AuthoringDerivedPublishPlan -TargetRoot $targetRootFull -RelativePaths $RelativePaths -BackupDirectory $backupRootFull }
    if ([System.IO.Path]::GetFullPath([string]$PublishPlan.targetRoot).TrimEnd('\','/') -ne $targetRootFull -or [System.IO.Path]::GetFullPath([string]$PublishPlan.backupRoot).TrimEnd('\','/') -ne $backupRootFull) {
        throw '[AUTHORING_PLAN_MISMATCH] Publish plan roots do not match the requested roots.'
    }
    $planByPath = @{}
    foreach ($planRecord in @($PublishPlan.records)) { $planByPath[[string]$planRecord.relativePath] = $planRecord }
    $records = [System.Collections.Generic.List[object]]::new()
    foreach ($relativePathRaw in @($RelativePaths | Sort-Object -Unique)) {
        $relativePath = ([string]$relativePathRaw).Replace('\','/')
        if (-not $planByPath.ContainsKey($relativePath)) { throw "[AUTHORING_PLAN_MISMATCH] Derived path is absent from the frozen plan: $relativePath" }
        $frozen = $planByPath[$relativePath]
        $candidate = [System.IO.Path]::GetFullPath((Join-Path $candidateRootFull $relativePath))
        if (-not (Test-AuthoringPathWithinRoot -Path $candidate -Root $candidateRootFull)) { throw "[AUTHORING_UNSAFE_PATH] Candidate escaped root: $relativePath" }
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { throw "Derived candidate does not exist: $candidate" }
        [void]$records.Add([pscustomobject][ordered]@{
            relativePath = $relativePath
            candidate = $candidate
            candidateSha256 = Get-AuthoringFileSha256 -Path $candidate
            target = [string]$frozen.target
            targetExisted = [bool]$frozen.targetExisted
            targetBeforeSha256 = if ([bool]$frozen.targetExisted) { [string]$frozen.targetBeforeSha256 } else { $null }
            backup = [string]$frozen.backup
            state = 'PLANNED'
            writeAttempted = $false
        })
    }

    foreach ($record in $records) {
        $targetState = Get-AuthoringOptionalFileState -Path $record.target
        if ($targetState.exists -ne [bool]$record.targetExisted -or $targetState.sha256 -ne $record.targetBeforeSha256) { throw "[AUTHORING_EXTERNAL_DRIFT] Derived target changed after plan: $($record.relativePath)" }
        if (Test-Path -LiteralPath $record.backup) { throw "[AUTHORING_BACKUP_EXISTS] Refusing to overwrite backup: $($record.backup)" }
    }
    $context = [pscustomobject]@{ records=$records.ToArray(); candidateRoot=$candidateRootFull; targetRoot=$targetRootFull; backupRoot=$backupRootFull }
    Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterCandidateFreeze' -Record $null -Index -1 -Context $context
    foreach ($record in $records) {
        if ((Get-AuthoringFileSha256 -Path $record.candidate) -ne $record.candidateSha256) { throw "[AUTHORING_CANDIDATE_DRIFT] Derived candidate changed after freeze: $($record.relativePath)" }
    }

    $touched = [System.Collections.Generic.List[object]]::new()
    try {
        for ($index = 0; $index -lt $records.Count; $index++) {
            $record = $records[$index]
            Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'BeforeWrite' -Record $record -Index $index -Context $context
            $targetState = Get-AuthoringOptionalFileState -Path $record.target
            if ($targetState.exists -ne [bool]$record.targetExisted -or $targetState.sha256 -ne $record.targetBeforeSha256) {
                throw "[AUTHORING_EXTERNAL_DRIFT] Derived target changed before publish: $($record.relativePath)"
            }
            if (Test-Path -LiteralPath $record.backup) { throw "[AUTHORING_BACKUP_EXISTS] Refusing to overwrite backup: $($record.backup)" }
            if ((Get-AuthoringFileSha256 -Path $record.candidate) -ne $record.candidateSha256) { throw "[AUTHORING_CANDIDATE_DRIFT] Derived candidate changed before publish: $($record.relativePath)" }
            [System.IO.Directory]::CreateDirectory((Split-Path -Parent $record.target)) | Out-Null
            [System.IO.Directory]::CreateDirectory((Split-Path -Parent $record.backup)) | Out-Null
            $stage = "$($record.target).authoring-$([guid]::NewGuid().ToString('N')).tmp"
            Copy-Item -LiteralPath $record.candidate -Destination $stage
            try {
                if ((Get-AuthoringFileSha256 -Path $stage) -ne $record.candidateSha256) { throw "[AUTHORING_STAGING_MISMATCH] Derived staging bytes changed: $($record.relativePath)" }
                Invoke-AuthoringGuardedWrite -Record $record -Stage $stage -Touched $touched -TestFaultInjector $TestFaultInjector -Index $index -Context $context
            }
            finally { if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Force } }
            Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterWriteBeforeVerify' -Record $record -Index $index -Context $context
            if ((Get-AuthoringFileSha256 -Path $record.target) -ne $record.candidateSha256) { throw "[AUTHORING_PUBLISH_VERIFY_FAILED] Derived target mismatch: $($record.relativePath)" }
            $record.state = 'VERIFIED'
        }
        Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'AfterAllWritesBeforeAudit' -Record $null -Index -1 -Context $context
        $audit = [ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='DERIVED_FILES_PUBLISHED'; taskOrigin=$TaskOrigin; candidateRoot=$candidateRootFull; targetRoot=$targetRootFull; provenancePath=$ProvenancePath; fileCount=$records.Count; files=$records.ToArray() }
        Add-AuthoringAuditRecord -AuditPath $AuditPath -Record $audit
        return [pscustomobject]$audit
    }
    catch {
        $primary = $_.Exception
        Invoke-AuthoringTestFault -TestFaultInjector $TestFaultInjector -Phase 'BeforeRollback' -Record $null -Index -1 -Context $context
        $rollbackResults = @(Invoke-AuthoringTransactionRollback -Records $touched.ToArray() -TestFaultInjector $TestFaultInjector -Context $context)
        $auditError = $null
        try { Add-AuthoringAuditRecord -AuditPath $AuditPath -Record ([ordered]@{ at=[DateTimeOffset]::Now.ToString('o'); action='DERIVED_FILES_PUBLISH_FAILED'; taskOrigin=$TaskOrigin; targetRoot=$targetRootFull; primaryError=$primary.Message; rollback=$rollbackResults; transaction=$records.ToArray() }) }
        catch { $auditError = $_.Exception.Message }
        throw (New-AuthoringPublishFailureException -PrimaryException $primary -RollbackResults $rollbackResults -AuditError $auditError)
    }
}
