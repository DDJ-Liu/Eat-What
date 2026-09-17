function Read-ArtDailyConfig([string]$Root) {
    $path=Join-Path $Root 'inputs/art/ART_DAILY_CONFIG.json'
    if (Test-Path -LiteralPath $path) {return Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json}
    return $null
}
function Get-ArtDailyControllerHandoff($Config, [string]$AuditId, [string]$ReportPath) {
    # Opt-in only. Report generation and red-file safety are independent of controller reception.
    if ($null -eq $Config -or $Config.controllerReceptionEnabled -ne $true) { return $null }
    return [pscustomobject]@{threadId=$Config.controllerThreadId
        message="ART 日报 $AuditId 已生成：$ReportPath；请读取 §6 和 triggers.json，按 .ai-workspace/CONTROL_CHAT.md 做去重拆解/红项拦截；本线未入队或部署。"}
}
function Test-ArtIgnored($Path) {return [IO.Path]::GetFileName($Path) -in @('Thumbs.db','desktop.ini','.DS_Store')}
function Initialize-ArtPngReader([string]$Root) {
    if ('ArtDailyPngReader' -as [type]) {return}
    Add-Type -AssemblyName System.Drawing
    $refs=@([Drawing.Bitmap].Assembly.Location,[Drawing.Rectangle].Assembly.Location) | Select-Object -Unique
    foreach($dependency in [Drawing.Bitmap].Assembly.GetReferencedAssemblies()) {
        if($dependency.Name -like 'System.Private.Windows.*') {$refs+=,[Reflection.Assembly]::Load($dependency).Location}
    }
    Add-Type -Path (Join-Path $Root 'art-daily-png.cs') -ReferencedAssemblies $refs
}
function Add-ArtPngMeasurements($Files,$Previous,[string]$Source,[string]$Root) {
    Initialize-ArtPngReader $Root
    $cache=@{}
    foreach($f in @($Previous.files)) {if ($null -ne $f -and $null -ne $f.png) {$cache[$f.sha256]=$f.png}}
    foreach($f in $Files) {
        if ([IO.Path]::GetExtension($f.path) -ine '.png') {continue}
        $png=$null;$problem=$null
        if($cache.ContainsKey($f.sha256)) {$png=$cache[$f.sha256]}
        else {
            try {$png=[ArtDailyPngReader]::Read((Join-Path $Source $f.path))}
            catch {$problem=$_.Exception.Message}
        }
        $f | Add-Member -NotePropertyName png -NotePropertyValue $png -Force
        $f | Add-Member -NotePropertyName pngError -NotePropertyValue $problem -Force
    }
}
function Get-ArtDailyDiff($Previous,$Current) {
    $old=@{};$new=@{}
    foreach($f in @($Previous)) {if($null -ne $f -and -not (Test-ArtIgnored $f.path)) {$old[$f.path]=$f}}
    foreach($f in @($Current)) {if(-not (Test-ArtIgnored $f.path)) {$new[$f.path]=$f}}
    $rows=New-Object Collections.ArrayList;$added=@();$removed=@()
    foreach($f in @($new.Values)) {
        if(-not $old.ContainsKey($f.path)) {$added+=,$f;continue}
        $b=$old[$f.path]
        $kind=if($b.sha256 -ne $f.sha256){'修改'}elseif($b.path -cne $f.path){'改名'}elseif(([DateTimeOffset]$b.lastWriteUtc).UtcTicks -ne ([DateTimeOffset]$f.lastWriteUtc).UtcTicks){'元数据'}else{$null}
        if($kind){[void]$rows.Add([pscustomobject]@{kind=$kind;path=$f.path;oldPath=$b.path;before=$b;after=$f;note='';basis='path/hash'})}
    }
    foreach($f in @($old.Values)) {if(-not $new.ContainsKey($f.path)) {$removed+=,$f}}
    $paired=@{}
    foreach($f in $removed) {
        $matches=@($added | Where-Object sha256 -eq $f.sha256)
        # Ambiguous identical duplicates are not arbitrarily paired.
        if($matches.Count -eq 1 -and @($removed | Where-Object sha256 -eq $f.sha256).Count -eq 1) {
            $n=$matches[0];$paired[$n.path]=$true;$paired[$f.path]=$true
            $kind=if([IO.Path]::GetDirectoryName($f.path) -ieq [IO.Path]::GetDirectoryName($n.path)){'改名'}else{'目录移动'}
            $note=if([IO.Path]::GetFileName($f.path) -cne [IO.Path]::GetFileName($n.path)){'同 SHA 异名；若同时跨目录则只计目录移动'}else{'同 SHA 跨目录'}
            [void]$rows.Add([pscustomobject]@{kind=$kind;path=$n.path;oldPath=$f.path;before=$f;after=$n;note=$note;basis='unique-sha-match'})
        }
    }
    foreach($f in $added) {if(-not $paired.ContainsKey($f.path)){[void]$rows.Add([pscustomobject]@{kind='新增';path=$f.path;oldPath='';before=$null;after=$f;note='';basis='path'})}}
    foreach($f in $removed) {if(-not $paired.ContainsKey($f.path)){[void]$rows.Add([pscustomobject]@{kind='删除';path=$f.path;oldPath=$f.path;before=$f;after=$null;note='';basis='path'})}}
    return @($rows | Sort-Object path,kind)
}
function Get-ArtDailyAnalysis($Snapshot,$Previous,$Diff,$Config,$PendingConfig) {
    $dims=New-Object Collections.ArrayList;$issues=New-Object Collections.ArrayList;$red=@{}
    # Red is not erased by an unchanged next-day diff. Human release is recorded by the controller.
    foreach($r in @($Previous.redFiles)) {if($null -ne $r){$red[$r.path]=$r}}
    $oldByPath=@{};foreach($f in @($Previous.files)){if($null -ne $f){$oldByPath[$f.path]=$f}}
    foreach($d in @($Diff | Where-Object {$_.kind -in @('新增','修改') -and [IO.Path]::GetExtension($_.path) -ieq '.png'})) {
        $f=$d.after;$p=$f.png;$old=$d.before.png;$reasons=@();$level='正常';$delta=$null
        if($f.pngError){$level='🔴';$reasons+='PNG解码失败'}
        elseif($null -ne $p) {
            if($p.transparent -or $p.black -or $p.width -lt 8 -or $p.height -lt 8){$level='🔴';$reasons+='纯透明/纯黑/尺寸小于8px'}
            if($null -ne $old -and $old.ratio -gt 0 -and $p.ratio -gt 0) {
                $delta=100*[math]::Abs($p.ratio/$old.ratio-1)
                if($delta -gt 5){$level='🔴';$reasons+='可见宽高比变化>5%'}
            } elseif($d.kind -eq '修改') {$reasons+='旧可见区无证据，不可比较（不使用旧画布比例）'}
            if([math]::Abs($p.occupancyWidth-0.83) -gt 0.10 -or [math]::Abs($p.occupancyHeight-0.83) -gt 0.10){if($level -eq '正常'){$level='🟡'};$reasons+='可见占比偏离83%超过10个百分点（E2诊断）'}
            if([math]::Max($p.width,$p.height) -gt 4096){if($level -eq '正常'){$level='🟡'};$reasons+='画布最大边>4096'}
        }
        $row=[pscustomobject]@{path=$f.path;canvas=$(if($p){"$($p.width)×$($p.height)"}else{'未知'});visible=$(if($p){"$($p.visibleWidth)×$($p.visibleHeight)@($($p.x),$($p.y))"}else{'未知'});occupancy=$(if($p){'{0:P1}/{1:P1}' -f $p.occupancyWidth,$p.occupancyHeight}else{'未知'});ratio=$p.ratio;previousRatio=$old.ratio;changePercent=$delta;level=$level;reasons=($reasons -join '；')}
        [void]$dims.Add($row)
        if($level -eq '🔴') {$red[$f.path]=[pscustomobject]@{path=$f.path;sha256=$f.sha256;sourceDailyId=$Snapshot.auditId;reason=$row.reasons}}
    }
    $valid=@($Snapshot.files | Where-Object {-not (Test-ArtIgnored $_.path)})
    foreach($f in $valid) {
        $name=[IO.Path]::GetFileName($f.path);$ext=[IO.Path]::GetExtension($name).ToLowerInvariant()
        $problems=@()
        if($name -match '\s'){$problems+='含空格'}
        if($name.Contains('+')){$problems+='含 +'}
        if($name -match '[^\x00-\x7F]'){$problems+='非 ASCII 文件名'}
        if($ext -eq '.png') {
            if($name -notmatch '^(ui_shicai_|ui_caipin_|ui_tag_|ui_gongju_|ui_caipu_|chuju_)'){$problems+='前缀不符'}
            if($name -match '^(ui_shicai_|ui_caipin_|ui_tag_|ui_gongju_)' -and $f.png -and ($f.png.width -ne 512 -or $f.png.height -ne 512)){$problems+='E3 图标画布非512×512'}
            if($f.path -match 'UIUX/菜谱/' -and ($name -match '^ui_caipin_|tuya[123]|446')){$problems+='目录错位：菜品/涂鸦待移出菜谱目录'}
        }
        foreach($problem in $problems){[void]$issues.Add([pscustomobject]@{path=$f.path;kind=$problem;detail='按 E1~E6/P1~P6 核对；仅报告';suggestion='请美术确认改名/目录/导出，日常不改源'})}
    }
    foreach($g in @($valid | Group-Object sha256 | Where-Object Count -gt 1)) {
        $all=(@($g.Group.path | Sort-Object) -join '；')
        foreach($f in $g.Group){[void]$issues.Add([pscustomobject]@{path=$f.path;kind='重复 SHA';detail=$all;suggestion='同内容不等于多余；请人工判断，不自动删除'})}
    }
    foreach($f in @($Diff | Where-Object {$_.kind -in @('新增','修改') -and [IO.Path]::GetExtension($_.path) -ieq '.psd'})) {
        [void]$issues.Add([pscustomobject]@{path=$f.path;kind='PSD P1~P6待人工';detail='文件变更已核实；尚未读取图层结构，不声称分层合规';suggestion='核对单元件组、光影、旧版、文字、画布与参考图交付'})
    }
    $pending=New-Object Collections.ArrayList
    foreach($item in $PendingConfig.items) {
        $hits=@();$missing=@()
        foreach($pattern in @($item.patterns)) {
            $found=@($valid | Where-Object {[IO.Path]::GetFileName($_.path) -ilike $pattern})
            if($found.Count){$hits+=@($found.path)}else{$missing+=,$pattern}
        }
        $hits=@($hits | Select-Object -Unique);$ready=($missing.Count -eq 0 -and @($item.patterns).Count -gt 0)
        if($item.minimumCount -and $hits.Count -lt $item.minimumCount){$ready=$false}
        $status=if($ready){'已出现；待规范/部署审查'}else{"缺项：$($missing -join '、')；命中$($hits.Count)件"}
        if($item.id -eq 'split') {
            $split=@($valid | Where-Object {$_.path -match '(Split|冰箱猫拆分)/' -and $_.path -match '\.png$'})
            $main=@($valid | Where-Object {[IO.Path]::GetFileName($_.path) -eq 'chuju_bingxiangmao_zhutu.png'})
            $hits=@((@($split.path)+@($main.path)) | Where-Object {$_});$ready=$false
            $status="拆分$($split.Count)/13；zhutu$($main.Count)/1；E6逐件尺度证据未齐，不解锁"
            if(@($item.scaleTargets).Count -eq 14 -and @($item.scaleTargets.path | Select-Object -Unique).Count -eq 14 -and $split.Count -eq 13 -and $main.Count -eq 1 -and $item.scaleEvidence) {
                $ready=$true
                foreach($target in $item.scaleTargets) {
                    $f=@($valid | Where-Object path -eq $target.path) | Select-Object -First 1
                    if(-not $f -or -not $f.png -or $f.path -notin $hits -or $target.preExportSha256 -notmatch '^[0-9A-Fa-f]{64}$' -or $target.width -le 0 -or $target.height -le 0 -or [math]::Abs($f.png.width/$target.width-1) -gt .05 -or [math]::Abs($f.png.height/$target.height-1) -gt .05 -or $f.sha256 -eq $target.preExportSha256){$ready=$false}
                }
                $status=if($ready){'14件重导且逐件E6尺度±5%通过'}else{'已出现，但重导/逐件E6尺度仍未通过'}
            }
        }
        if($item.requiresManualNaming){$ready=$false;$status='待人工核对源命名恢复/迁移映射；不能仅凭旧文件消失视为完成'}
        $prior=@($Previous.pendingItems | Where-Object id -eq $item.id) | Select-Object -First 1
        $first=$prior.firstAppearedAt
        if(-not $first -and $hits.Count){$first=([DateTimeOffset]$Snapshot.capturedAt).ToOffset([timespan]::FromHours(8)).ToString('yyyy-MM-dd')}
        [void]$pending.Add([pscustomobject]@{id=$item.id;name=$item.name;expected=$item.expected;status=$status;firstAppearedAt=$first;ready=$ready;paths=@($hits);trigger=$item.trigger;card=$item.card})
    }
    $triggers=@()
    foreach($item in @($pending | Where-Object trigger)) {
        $blocked=@($item.paths | Where-Object {$_ -and $red.ContainsKey($_)})
        $triggers += [pscustomobject]@{key=$item.id;condition=$item.expected;hit=$item.ready;deployAllowed=($item.ready -and $blocked.Count -eq 0);blockedFiles=$blocked;paths=$item.paths;target=$item.trigger;card=$item.card}
    }
    $triggers += [pscustomobject]@{key='red';condition='任何 🔴（含前日报未处理红项）';hit=($red.Count -gt 0);deployAllowed=$false;blockedFiles=@($red.Keys);paths=@($red.Keys);target='PENDING_USER 提醒，不入队';card=$null}
    return [pscustomobject]@{dims=@($dims);issues=@($issues);pending=@($pending);triggers=@($triggers);redFiles=@($red.Values)}
}
function Format-ArtCanvas($File) {if($null -ne $File.png){return "$($File.png.width)×$($File.png.height)"};return '未知/无同期尺寸证据'}
function Write-ArtDailyCsv($Path,$Rows,[string[]]$Columns) {
    # CSV is an evidence export, not an executable spreadsheet; escape formula-leading source text.
    $safe=@(foreach($r in @($Rows)) {$v=[ordered]@{};foreach($c in $Columns){$x=$r.$c;if($x -is [string] -and $x -match '^[=+@\-\t\r]'){$x="'"+$x};$v[$c]=$x};[pscustomobject]$v})
    if($safe.Count){$csv=($safe | ConvertTo-Csv -NoTypeInformation) -join "`r`n"}else{$csv=($Columns | ForEach-Object {'"'+$_+'"'}) -join ','}
    [IO.File]::WriteAllText($Path,$csv+"`r`n",(New-Object Text.UTF8Encoding($true)))
}
function New-ArtDailyReport($Snapshot,$Previous,$Diff,$Analysis) {
    $l=New-Object Collections.Generic.List[string]
    function Line($t='') {$l.Add([string]$t)}
    function Cell($x) {Convert-ArtReportCell $x}
    $actual=([DateTimeOffset]$Snapshot.capturedAt).ToOffset([timespan]::FromHours(8))
    Line "# $($Snapshot.auditId) · 美术资产每日审查";Line
    Line "- 审查时间：计划 $(@($Snapshot.scheduledDates) -join ', ') 00:00 +08:00；实际 $($actual.ToString('yyyy-MM-dd HH:mm:ss zzz'))"
    Line ('- 延后原因：'+$(if(@($Snapshot.deferReasons).Count){$Snapshot.deferReasons -join '；'}else{'无已记录业务延后；如为手动首报/离线补审，以实际时间为准'}))
    Line ('- 审查根：'+(Cell $Snapshot.sourcePath));Line ('- 基线：'+(Cell $Previous.auditId)+'；'+(Cell $Snapshot.baselinePath))
    $files=@($Snapshot.files);$ignored=@($files | Where-Object {Test-ArtIgnored $_.path}).Count
    $png=@($files | Where-Object path -Match '(?i)\.png$').Count;$psd=@($files | Where-Object path -Match '(?i)\.psd$').Count
    Line "- 文件总数：$($files.Count)（PNG $png / PSD $psd / 其它 $($files.Count-$png-$psd-$ignored) / 忽略 $ignored：Thumbs.db、desktop.ini、.DS_Store）"
    Line ('- 变更总数：'+((@('新增','修改','删除','改名','目录移动','元数据') | ForEach-Object {$kind=$_;$kind+' '+@($Diff | Where-Object kind -eq $kind).Count}) -join ' / '))
    Line;Line '## 1. 变更清单（文件级，按目录）';Line
    Line '| 类型 | 相对路径 | 旧 SHA-8 | 新 SHA-8 | 旧尺寸 | 新尺寸 | 备注 |';Line '|---|---|---|---|---|---|---|'
    if(-not @($Diff).Count){Line '| 无变更 | — | — | — | — | — | 已完成全量哈希比较 |'}
    foreach($d in $Diff){$oldSha=if($d.before){$d.before.sha256.Substring(0,8)}else{'—'};$newSha=if($d.after){$d.after.sha256.Substring(0,8)}else{'—'};$path=if($d.kind -in @('改名','目录移动')){$d.oldPath+' → '+$d.path}else{$d.path};Line "| $($d.kind) | $(Cell $path) | $oldSha | $newSha | $(Format-ArtCanvas $d.before) | $(Format-ArtCanvas $d.after) | $(Cell $d.note) |"}
    Line;Line '## 2. 尺寸与可见区回归（仅本次新增/修改的 PNG）';Line
    Line '| 相对路径 | 画布 | alpha 可见区 | 可见区占比 | 宽高比（可见区） | 上一版宽高比 | 变化% | 判定 |';Line '|---|---|---|---|---|---|---|---|'
    foreach($d in $Analysis.dims){
        $oldText=if($null -eq $d.previousRatio){'未知/无同期可见区'}else{$d.previousRatio}
        $changeText=if($null -eq $d.changePercent){'不可比较'}else{'{0:F2}' -f $d.changePercent}
        Line "| $(Cell $d.path) | $($d.canvas) | $($d.visible) | $($d.occupancy) | $($d.ratio) | $oldText | $changeText | $($d.level) $(Cell $d.reasons) |"
    }
    if(-not $Analysis.dims.Count){Line '| 本次无新增/修改 PNG | — | — | — | — | — | — | 无 |'}
    Line '算法：alpha>0最小外接矩形；变化%=abs(新/旧-1)×100；无历史可见区写不可比较。83%为批次诊断参照而非强制边距；PSD分层/内嵌旋转/描边仍须人工。'
    Line;Line '## 3. 命名与目录规范';Line
    Line '| 相对路径 | 问题类型 | 说明 | 建议 |';Line '|---|---|---|---|'
    foreach($i in $Analysis.issues){Line "| $(Cell $i.path) | $(Cell $i.kind) | $(Cell $i.detail) | $(Cell $i.suggestion) |"}
    if(-not $Analysis.issues.Count){Line '| 未发现文件级问题 | — | 不代表视觉/PSD分层已验收 | — |'}
    Line;Line '## 4. 待产清单状态';Line
    Line '| 待产项 | 期望文件名 | 今日状态 | 首次出现日期 |';Line '|---|---|---|---|'
    foreach($i in $Analysis.pending){Line "| $(Cell $i.name) | $(Cell $i.expected) | $(Cell $i.status) | $(Cell $i.firstAppearedAt) |"}
    Line '首次出现日期为审查系统首次观察日期，不伪造真实创作/上传时间；条目由控制台维护，本车道仅填状态。'
    Line;Line '## 5. 同步区动作';Line
    Line '- 仅报告；本次未镜像。未修改源目录、同步区或 Assets。显式同步任务才可镜像。'
    Line '- manifest 路径：本报告同目录 manifest.json。'
    Line;Line '## 6. 挂起任务触发命中（供用户阅读；明确要求后由控制台手动安排，本车道不入队）';Line
    Line '不回传控制台，不因命中自动入队/部署。';Line
    Line '| 触发条件 | 命中 | 对应挂起任务 |';Line '|---|---|---|'
    foreach($t in $Analysis.triggers){$hit=if($t.hit){'是'}else{'否/证据未齐'};$suffix=if($t.hit -and -not $t.deployAllowed){'；禁止部署/待用户'}else{''};Line "| $(Cell $t.condition) | $hit$suffix | $(Cell $t.target) |"}
    Line;Line '## 7. 六要素';Line
    Line '- 做了什么：只读文件差异、PNG可见区与规范审查；未执行挂起业务任务。'
    Line '- 产物路径：本报告、manifest.json、diff.csv、dims.csv、changes.json、triggers.json。'
    Line '- 实际验证：逐文件SHA-256；双次完整目录扫描一致，PNG测量前后文件哈希不变。'
    Line ('- 待人工项：本次红项及§3/PSD项；红文件证据记录 '+@($Analysis.redFiles).Count+'。包括前日报保留证据；具体批准处置以控制台精确文件/SHA账本为准，报告完成不等于消费许可。')
    Line '- 文档影响：无。';Line '- 工具登记影响：无。'
    return ($l -join "`r`n")+"`r`n"
}
