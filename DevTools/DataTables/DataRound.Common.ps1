$ErrorActionPreference = 'Stop'

function Get-DataTablesCsvSummaryHash {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$DataTablesRoot)

    $root = [System.IO.Path]::GetFullPath($DataTablesRoot)
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "DataTables root does not exist: $root"
    }

    $rows = @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.csv' | ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
        "$relative|$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
    } | Sort-Object)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [Convert]::ToHexString($sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes(($rows -join "`n"))))
    }
    finally {
        $sha256.Dispose()
    }
}

function Save-DataRoundManifest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)]$Manifest
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    [System.IO.Directory]::CreateDirectory((Split-Path -Parent $fullPath)) | Out-Null
    $temporaryPath = "$fullPath.tmp.$([Guid]::NewGuid().ToString('N'))"
    $utf8NoBom = [System.Text.UTF8Encoding]::new($false)
    try {
        [System.IO.File]::WriteAllText($temporaryPath, (($Manifest | ConvertTo-Json -Depth 12) + [Environment]::NewLine), $utf8NoBom)
        [System.IO.File]::Move($temporaryPath, $fullPath, $true)
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
    }
    return $fullPath
}

function Write-DataRoundExportEvent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidatePattern('^USER-U1-R[0-9]+$')][string]$RoundId,
        [Parameter(Mandatory)][string]$DataTablesRoot,
        [Parameter(Mandatory)][string]$EventDirectory,
        [string]$UserDependency = 'USER-U1',
        [string]$DataTaskModuleId
    )

    if ([string]::IsNullOrWhiteSpace($DataTaskModuleId)) {
        $roundNumber = [regex]::Match($RoundId, '[0-9]+$').Value
        $DataTaskModuleId = "DATA-R$roundNumber"
    }
    if ($DataTaskModuleId -notmatch '^DATA-R[0-9]+$') {
        throw 'DataTaskModuleId must match DATA-Rn.'
    }

    $manifest = [ordered]@{
        schemaVersion = 1
        roundId = $RoundId
        userDependency = $UserDependency
        dataTaskModuleId = $DataTaskModuleId
        status = 'EXPORTED'
        csvSummarySha256 = Get-DataTablesCsvSummaryHash -DataTablesRoot $DataTablesRoot
        exportedAt = [DateTimeOffset]::Now.ToString('o')
        doneAt = $null
        userDecision = $null
        source = 'DevTools/DataTables/Authoring/Export-AuthoringToCsv.ps1'
    }
    $path = Join-Path ([System.IO.Path]::GetFullPath($EventDirectory)) "$RoundId.json"
    [void](Save-DataRoundManifest -Path $path -Manifest $manifest)
    return [pscustomobject]@{ path=$path; manifest=[pscustomobject]$manifest }
}
