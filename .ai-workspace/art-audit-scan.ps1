# Read-only file inventory. No image transforms, sync, source writes, or Unity calls.
function Get-ArtInventory([string]$SourcePath) {
    $root=Get-Item -LiteralPath $SourcePath -Force -ErrorAction Stop
    if (-not $root.PSIsContainer) { throw 'Audit source must be a directory.' }
    if ($root.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Reparse-point audit roots require explicit review.' }
    $prefix=$root.FullName.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar
    $stack=New-Object 'System.Collections.Generic.Stack[string]'
    $stack.Push($root.FullName)
    $records=New-Object System.Collections.ArrayList
    while ($stack.Count -gt 0) {
        foreach ($item in @(Get-ChildItem -LiteralPath ($stack.Pop()) -Force -ErrorAction Stop)) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Unscanned link/junction: $($item.FullName)" }
            if ($item.PSIsContainer) { $stack.Push($item.FullName); continue }
            $beforeLength=$item.Length; $beforeTime=$item.LastWriteTimeUtc.ToString('o')
            $stream=[IO.File]::Open($item.FullName,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
            $sha=[Security.Cryptography.SHA256]::Create()
            try { $hash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
            finally { $sha.Dispose();$stream.Dispose() }
            $item.Refresh()
            if (-not $item.Exists -or $item.Length -ne $beforeLength -or $item.LastWriteTimeUtc.ToString('o') -ne $beforeTime) { throw "File changed while scanning: $($item.FullName)" }
            [void]$records.Add([pscustomobject]@{path=$item.FullName.Substring($prefix.Length).Replace('\','/');bytes=$beforeLength;lastWriteUtc=$beforeTime;sha256=$hash})
        }
    }
    return @($records | Sort-Object -Property path -CaseSensitive)
}
function Get-ArtInventoryDigest($Files) {
    $json=ConvertTo-Json -InputObject @($Files) -Depth 6 -Compress
    $sha=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($json))).Replace('-','') }
    finally { $sha.Dispose() }
}
function Compare-ArtInventory($Previous, $Current, [bool]$HasBaseline) {
    $old=@{}; $new=@{}
    foreach ($file in @($Previous)) { if ($null -ne $file) {$old[$file.path]=$file} }
    foreach ($file in @($Current)) { $new[$file.path]=$file }
    $added=@();$removed=@();$modified=@();$metadata=@();$unchanged=0
    if ($HasBaseline) {
        foreach ($file in @($Current)) {
            if (-not $old.ContainsKey($file.path)) { $added += $file;continue }
            $before=$old[$file.path]
            if ($before.sha256 -ne $file.sha256) { $modified += [pscustomobject]@{path=$file.path;before=$before;after=$file} }
            elseif (([DateTimeOffset]$before.lastWriteUtc).UtcTicks -ne ([DateTimeOffset]$file.lastWriteUtc).UtcTicks -or $before.path -cne $file.path) { $metadata += [pscustomobject]@{path=$file.path;before=$before;after=$file} }
            else { $unchanged++ }
        }
        foreach ($file in @($Previous)) { if (-not $new.ContainsKey($file.path)) {$removed += $file} }
    }
    $moves=@()
    foreach ($file in $removed) {
        $matches=@($added | Where-Object sha256 -eq $file.sha256)
        if ($matches.Count -eq 1 -and @($removed | Where-Object sha256 -eq $file.sha256).Count -eq 1) {
            $moves += [pscustomobject]@{from=$file.path;to=$matches[0].path;sha256=$file.sha256;certainty='candidate-only'}
        }
    }
    return [pscustomobject]@{baseline=(-not $HasBaseline);added=@($added);removed=@($removed);modified=@($modified);metadataOnly=@($metadata);renameCandidates=@($moves);unchanged=$unchanged}
}
function Convert-ArtReportCell($Value) {
    return ([string]$Value).Replace('&','&amp;').Replace('<','&lt;').Replace('>','&gt;').Replace('|','&#124;').Replace('`','&#96;').Replace("`r",' ').Replace("`n",' ')
}
function New-ArtAuditReport($Snapshot, $Previous, $Diff) {
    $lines=New-Object System.Collections.Generic.List[string]
    $lines.Add('# 美术资产日常差异审查 · '+$Snapshot.auditId)
    $lines.Add('')
    $lines.Add('- 只读源：'+(Convert-ArtReportCell $Snapshot.sourcePath))
    $lines.Add('- 本次实际采样：'+$Snapshot.startedAt+' 至 '+$Snapshot.capturedAt+'（UTC；工作日调度按 Asia/Shanghai）')
    $lines.Add('- 计划日期：'+(@($Snapshot.scheduledDates) -join ', ')+'；延迟执行时仅反映实际扫描时点，不能还原缺失的历史午夜状态。')
    $lines.Add('- 上次成功采样：'+$(if ($null -eq $Previous) {'无；本轮建立基线，不把现存文件误报为新增。'} else {([DateTimeOffset]$Previous.capturedAt).ToUniversalTime().ToString('o')}))
    $lines.Add('- 文件总数：'+@($Snapshot.files).Count+'；两次完整 SHA-256 扫描一致。此报告不评价视觉设计质量，不执行同步/删除/导入。')
    $lines.Add('- 新增 '+@($Diff.added).Count+' / 内容修改 '+@($Diff.modified).Count+' / 删除 '+@($Diff.removed).Count+' / 仅时间或大小写变化 '+@($Diff.metadataOnly).Count+' / 未变化 '+$Diff.unchanged)
    foreach ($entry in @(@('新增','added'),@('删除','removed'))) {
        $lines.Add('');$lines.Add('## '+$entry[0]);$lines.Add('');$lines.Add('| 相对路径 | 字节数 | 修改时间 UTC | SHA-256 |');$lines.Add('|---|---:|---|---|')
        foreach ($f in @($Diff.($entry[1]))) { $lines.Add('| '+(Convert-ArtReportCell $f.path)+' | '+$f.bytes+' | '+([DateTimeOffset]$f.lastWriteUtc).ToUniversalTime().ToString('o')+' | '+$f.sha256+' |') }
    }
    foreach ($entry in @(@('内容修改','modified'),@('仅元数据/大小写变化','metadataOnly'))) {
        $lines.Add('');$lines.Add('## '+$entry[0]);$lines.Add('');$lines.Add('| 相对路径 | 字节数 前→后 | 修改时间 UTC 前→后 | SHA-256 前→后 |');$lines.Add('|---|---|---|---|')
        foreach ($f in @($Diff.($entry[1]))) { $lines.Add('| '+(Convert-ArtReportCell $f.path)+' | '+$f.before.bytes+' → '+$f.after.bytes+' | '+([DateTimeOffset]$f.before.lastWriteUtc).ToUniversalTime().ToString('o')+' → '+([DateTimeOffset]$f.after.lastWriteUtc).ToUniversalTime().ToString('o')+' | '+$f.before.sha256+' → '+$f.after.sha256+' |') }
    }
    $lines.Add('');$lines.Add('## 疑似移动/重命名（仅唯一同内容匹配，不作为已确认重命名，仍计入增删）');$lines.Add('')
    foreach ($f in @($Diff.renameCandidates)) {$lines.Add('- '+(Convert-ArtReportCell $f.from)+' → '+(Convert-ArtReportCell $f.to))}
    $lines.Add('');$lines.Add('完整文件清单与机器差异见同目录 snapshot.json / changes.json。失败或不完整扫描不会推进比较基线。')
    return ($lines -join "`r`n")+"`r`n"
}
