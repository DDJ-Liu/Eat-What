[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('Status','Install','Uninstall')][string]$Action,
    [ValidateRange(5,10)][int]$IntervalMinutes = 5,
    [string]$TaskName = 'EatWhat-WATCH-PRE5b',
    [string]$DeliveryScript
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'PS7 or later is required.' }
$watch = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'watch.ps1'))
$pwsh = Join-Path $PSHOME 'pwsh.exe'
if (-not (Test-Path -LiteralPath $pwsh -PathType Leaf)) { throw 'pwsh.exe not found.' }
$marker = 'EatWhat PRE-5b no-model WATCH managed by Manage-WatchHost.ps1'
$existing = @(Get-ScheduledTask -ErrorAction Stop | Where-Object TaskName -eq $TaskName | Select-Object -First 1)[0]
if ($existing -and [string]$existing.Description -cne $marker) {
    throw "Task name belongs to another owner; refusing to modify: $TaskName"
}
if ($Action -eq 'Status') {
    [pscustomobject]@{action='WATCH_HOST_STATUS';taskName=$TaskName;installed=($null -ne $existing);
        state=$(if($existing){[string]$existing.State}else{$null});intervalMinutes=$IntervalMinutes} |
        ConvertTo-Json -Compress
    return
}
if ($Action -eq 'Uninstall') {
    if ($existing) { Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false }
    [pscustomobject]@{action='WATCH_HOST_REMOVED';taskName=$TaskName;wasInstalled=($null -ne $existing)} |
        ConvertTo-Json -Compress
    return
}
if ($existing) { throw 'WATCH host already installed; inspect status, then explicitly uninstall before changing it.' }
$args = "-NoProfile -WindowStyle Hidden -File `"$watch`" -IntervalMinutes $IntervalMinutes"
if ($DeliveryScript) {
    $delivery = [IO.Path]::GetFullPath($DeliveryScript)
    if (-not (Test-Path -LiteralPath $delivery -PathType Leaf)) { throw 'Delivery adapter is missing.' }
    $args += " -DeliveryScript `"$delivery`""
}
$actionSpec = New-ScheduledTaskAction -Execute $pwsh -Argument $args -WorkingDirectory (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes) -RepetitionDuration (New-TimeSpan -Days 3650)
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Minutes 4) -MultipleInstances IgnoreNew
$principal = New-ScheduledTaskPrincipal -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $TaskName -Description $marker -Action $actionSpec -Trigger $trigger -Settings $settings -Principal $principal | Out-Null
[pscustomobject]@{action='WATCH_HOST_INSTALLED';taskName=$TaskName;intervalMinutes=$IntervalMinutes;
    caveat='Installation does not prove any wakeup. Confirm Task Scheduler LastRunTime and WATCH cursor/adapter receipt separately.'} |
    ConvertTo-Json -Compress
