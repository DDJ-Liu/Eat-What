[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$InputWorkbook,
    [Parameter(Mandatory)][string]$CandidateWorkbook,
    [Parameter(Mandatory)][string]$ExpectedInputSha256,
    [string]$DataTablesRoot
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'Authoring.Common.ps1')
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not $DataTablesRoot) { $DataTablesRoot=Join-Path (Get-AuthoringRepositoryRoot) 'DataTables' }
$source=[IO.Path]::GetFullPath($InputWorkbook)
$candidate=[IO.Path]::GetFullPath($CandidateWorkbook)
$expected=$ExpectedInputSha256.ToUpperInvariant()
if ($source -eq $candidate -or (Test-Path -LiteralPath $candidate)) { throw '[AUTHORING_FORMAT_TARGET_EXISTS] Candidate must be new and distinct.' }
if ((Get-AuthoringFileSha256 $source) -ne $expected) { throw '[AUTHORING_EXTERNAL_DRIFT] Source changed before formatting.' }
$baseline=Get-AuthoringWorkbookContentSnapshot $source
$schemas=@(Get-ChildItem (Join-Path $DataTablesRoot 'Schemas') -Recurse -File -Filter '*.schema.json' | ForEach-Object {
    $schema=Get-Content $_.FullName -Raw | ConvertFrom-Json
    $schema | Add-Member -NotePropertyName schemaPath -NotePropertyValue $_.FullName
    $schema
})
if($schemas.Count -ne 14){throw "[AUTHORING_SCHEMA_COUNT] Expected 14, found $($schemas.Count)."}
$schemaByName=@{}
foreach($schema in $schemas){$schemaByName[[string]$schema.tableName]=$schema}
$main='http://schemas.openxmlformats.org/spreadsheetml/2006/main'
function New-Element([xml]$Document,[string]$Name){$Document.CreateElement($Name,$main)}
function Read-Xml($Archive,[string]$Name){[xml](Read-AuthoringZipEntryText -Archive $Archive -Name $Name)}
function Write-Xml($Archive,[string]$Name,[xml]$Document){
    $old=$Archive.GetEntry($Name); if(-not $old){throw "[AUTHORING_FORMAT_ENTRY] $Name"}
    $old.Delete();$entry=$Archive.CreateEntry($Name,[IO.Compression.CompressionLevel]::Optimal)
    $writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($false))
    try{$Document.Save($writer)}finally{$writer.Dispose()}
}
function Column-Name([int]$Index){
    $result=''
    for($n=$Index+1;$n -gt 0;$n=[Math]::Floor(($n-1)/26)){$result=[char](65+(($n-1)%26))+$result}
    $result
}
function Reference-Target($Reference){
    $relative=([string]$Reference.targetSchema).Replace('\','/').Replace('Schemas/','')
    $target=@($schemas | Where-Object { $_.schemaPath.Replace('\','/').EndsWith('/Schemas/'+$relative,[StringComparison]::OrdinalIgnoreCase) } | Select-Object -First 1)
    if($target.Count -ne 1){throw "[AUTHORING_REFERENCE_SCHEMA] $relative"}
    for($i=0;$i -lt $target[0].columns.Count;$i++){
        if($target[0].columns[$i].name -eq $Reference.targetField -or $target[0].columns[$i].csvColumn -eq $Reference.targetField){
            return [pscustomobject]@{sheet=$target[0].tableName;column=(Column-Name $i)}
        }
    }
    throw "[AUTHORING_REFERENCE_FIELD] $relative/$($Reference.targetField)"
}
[IO.Directory]::CreateDirectory((Split-Path -Parent $candidate))|Out-Null
[IO.File]::Copy($source,$candidate,$false)
$restoredStyles=0;$ruleCount=0
try{
    $zip=[IO.Compression.ZipFile]::Open($candidate,[IO.Compression.ZipArchiveMode]::Update)
    try{
        $book=Read-Xml $zip 'xl/workbook.xml'
        $rels=Read-Xml $zip 'xl/_rels/workbook.xml.rels'
        $styles=Read-Xml $zip 'xl/styles.xml'
        $sns=[Xml.XmlNamespaceManager]::new($styles.NameTable);$sns.AddNamespace('s',$main)
        $xfs=$styles.SelectSingleNode('/s:styleSheet/s:cellXfs',$sns)
        if(-not $xfs){throw '[AUTHORING_STYLE_BASELINE] Cell styles missing.'}
        $bodyIndex=-1;$normalHeaderIndex=-1;$requiredHeaderIndex=-1
        for($j=0;$j -lt $xfs.ChildNodes.Count;$j++){
            $item=$xfs.ChildNodes[$j]
            if($item.fontId -eq '1' -and $item.fillId -eq '0' -and $item.borderId -eq '1' -and $bodyIndex -lt 0){$bodyIndex=$j}
            if($item.fillId -eq '2' -and $normalHeaderIndex -lt 0){$normalHeaderIndex=$j}
            if($item.fillId -eq '3' -and $requiredHeaderIndex -lt 0){$requiredHeaderIndex=$j}
        }
        if($bodyIndex -lt 0 -or $normalHeaderIndex -lt 0 -or $requiredHeaderIndex -lt 0){throw '[AUTHORING_STYLE_BASELINE] Approved body/header styles not found.'}
        $styleMap=@{}
        foreach($format in @('text','int','decimal')){
            $xf=$xfs.ChildNodes[$bodyIndex].CloneNode($true)
            if($format -eq 'text'){$xf.SetAttribute('numFmtId','49')}
            elseif($format -eq 'int'){$xf.SetAttribute('numFmtId','1')}
            else{
                $numFmts=$styles.SelectSingleNode('/s:styleSheet/s:numFmts',$sns)
                if(-not $numFmts){$numFmts=New-Element $styles 'numFmts';[void]$styles.DocumentElement.InsertBefore($numFmts,$styles.DocumentElement.FirstChild)}
                $maxId=163;foreach($node in @($numFmts.ChildNodes)){$maxId=[Math]::Max($maxId,[int]$node.numFmtId)}
                $newFmt=New-Element $styles 'numFmt';$newFmt.SetAttribute('numFmtId',[string]($maxId+1));$newFmt.SetAttribute('formatCode','0.########')
                [void]$numFmts.AppendChild($newFmt);$numFmts.SetAttribute('count',[string]$numFmts.ChildNodes.Count)
                $xf.SetAttribute('numFmtId',[string]($maxId+1))
            }
            $xf.SetAttribute('applyNumberFormat','1');$styleMap[$format]=$xfs.ChildNodes.Count;[void]$xfs.AppendChild($xf)
        }
        $xfs.SetAttribute('count',[string]$xfs.ChildNodes.Count)
        Write-Xml $zip 'xl/styles.xml' $styles
        $rns=[Xml.XmlNamespaceManager]::new($rels.NameTable);$rns.AddNamespace('r','http://schemas.openxmlformats.org/package/2006/relationships')
        $targetById=@{};foreach($rel in @($rels.SelectNodes('/r:Relationships/r:Relationship',$rns))){$targetById[[string]$rel.Id]=[string]$rel.Target}
        $bns=[Xml.XmlNamespaceManager]::new($book.NameTable);$bns.AddNamespace('s',$main)
        foreach($sheetNode in @($book.SelectNodes('/s:workbook/s:sheets/s:sheet',$bns))){
            $name=[string]$sheetNode.name;if(-not $schemaByName.ContainsKey($name)){continue}
            $schema=$schemaByName[$name]
            $rid=$sheetNode.GetAttribute('id','http://schemas.openxmlformats.org/officeDocument/2006/relationships')
            $target=$targetById[$rid].Replace('\','/')
            $entry=if($target.StartsWith('/')){$target.TrimStart('/')}elseif($target.StartsWith('xl/')){$target}else{'xl/'+$target}
            $xml=Read-Xml $zip $entry
            $ns=[Xml.XmlNamespaceManager]::new($xml.NameTable);$ns.AddNamespace('s',$main)
            foreach($cell in @($xml.SelectNodes('/s:worksheet/s:sheetData/s:row/s:c',$ns))){
                if([string]$cell.r -notmatch '^([A-Z]+)([0-9]+)$'){continue}
                $letters=$Matches[1];$row=[int]$Matches[2];$columnIndex=0
                foreach($letter in $letters.ToCharArray()){$columnIndex=$columnIndex*26+([int][char]$letter-64)}
                $columnIndex--;if($columnIndex -ge $schema.columns.Count){continue}
                $current=if($cell.HasAttribute('s')){[int]$cell.s}else{0}
                if($current -ne 0){continue}
                if($row -eq 1){$newStyle=if($schema.columns[$columnIndex].required){$requiredHeaderIndex}else{$normalHeaderIndex}}
                else{
                    $type=[string]$schema.columns[$columnIndex].type
                    $format=if($type -eq 'int'){'int'}elseif($type -in @('float','double','decimal')){'decimal'}else{'text'}
                    $newStyle=$styleMap[$format]
                }
                $cell.SetAttribute('s',[string]$newStyle);$restoredStyles++
            }
            $view=$xml.SelectSingleNode('/s:worksheet/s:sheetViews/s:sheetView',$ns)
            if($view -and -not $view.SelectSingleNode('s:pane',$ns)){
                $pane=New-Element $xml 'pane'
                $pane.SetAttribute('ySplit','1');$pane.SetAttribute('topLeftCell','A2');$pane.SetAttribute('activePane','bottomLeft');$pane.SetAttribute('state','frozen')
                [void]$view.PrependChild($pane)
            }
            $cols=$xml.SelectSingleNode('/s:worksheet/s:cols',$ns)
            if(-not $cols){$cols=New-Element $xml 'cols';[void]$xml.DocumentElement.InsertBefore($cols,$xml.SelectSingleNode('/s:worksheet/s:sheetData',$ns))}
            for($i=1;$i -le $schema.columns.Count;$i++){
                if(@($cols.ChildNodes | Where-Object { [int]$_.min -le $i -and [int]$_.max -ge $i }).Count -eq 0){
                    $col=New-Element $xml 'col';$col.SetAttribute('min',[string]$i);$col.SetAttribute('max',[string]$i);$col.SetAttribute('width','18');$col.SetAttribute('customWidth','1');[void]$cols.AppendChild($col)
                }
            }
            $dvRoot=$xml.SelectSingleNode('/s:worksheet/s:dataValidations',$ns)
            if(-not $dvRoot){
                $dvRoot=New-Element $xml 'dataValidations'
                $page=$xml.SelectSingleNode('/s:worksheet/s:pageMargins',$ns)
                if($page){[void]$xml.DocumentElement.InsertBefore($dvRoot,$page)}else{[void]$xml.DocumentElement.AppendChild($dvRoot)}
            }
            $references=[Collections.Generic.List[object]]::new()
            foreach($reference in @($schema.references)){[void]$references.Add($reference)}
            if($name -ne 'Localization'){
                foreach($column in $schema.columns){
                    if($column.type -eq 'loc_key' -and @($references | Where-Object field -EQ $column.name).Count -eq 0){
                        [void]$references.Add([pscustomobject]@{field=$column.name;targetSchema='Localization/Localization.schema.json';targetField='loc_key';isList=$false})
                    }
                }
            }
            foreach($reference in $references){
                $index=-1
                for($i=0;$i -lt $schema.columns.Count;$i++){if($schema.columns[$i].name -eq $reference.field -or $schema.columns[$i].csvColumn -eq $reference.field){$index=$i;break}}
                if($index -lt 0){continue}
                $column=Column-Name $index
                $destination=Reference-Target $reference
                foreach($cf in @($xml.SelectNodes('//*[local-name()="conditionalFormatting"]'))){
                    $formulas=@($cf.SelectNodes('.//*[local-name()="formula" or local-name()="f"]'))
                    $legacy=@($formulas | Where-Object {
                        [regex]::IsMatch($_.InnerText, ('^AND\(' + [regex]::Escape($column) + '[0-9]+<>'),[Text.RegularExpressions.RegexOptions]::IgnoreCase) -and
                        $_.InnerText -match 'COUNTIF|TEXTSPLIT' -and
                        ($cf.NamespaceURI -eq 'http://schemas.microsoft.com/office/spreadsheetml/2009/9/main' -or
                         ($cf.GetAttribute('sqref') -eq "$($column)2:$($column)5000" -and $cf.OuterXml -match 'dxfId="0"'))
                    })
                    if($legacy.Count -gt 0){[void]$cf.ParentNode.RemoveChild($cf)}
                }
                $dollar=[char]36
                $targetRange="'$($destination.sheet.Replace("'","''"))'!$dollar$($destination.column)$dollar"+'2:'+"$dollar$($destination.column)$dollar"+'5000'
                $isList=([bool]$reference.isList -or $schema.columns[$index].type -eq 'list<ref>')
                if($isList){
                    $separator=if($reference.separator){[string]$reference.separator}elseif($schema.columns[$index].separator){[string]$schema.columns[$index].separator}else{';'}
                    $separator=$separator.Replace('"','""')
                    $formula='AND('+$column+'2<>"",IFERROR(SUMPRODUCT(--(COUNTIF('+$targetRange+',TRIM(_xlfn.TEXTSPLIT('+$column+'2,"'+$separator+'")))=0))>0,TRUE))'
                }else{$formula='AND('+$column+'2<>"",COUNTIF('+$targetRange+','+$column+'2)=0)'}
                $cf=New-Element $xml 'conditionalFormatting';$cf.SetAttribute('sqref',"$($column)2:$($column)5000")
                $rule=New-Element $xml 'cfRule';$rule.SetAttribute('type','expression');$rule.SetAttribute('dxfId','0');$rule.SetAttribute('priority',[string](100+$ruleCount))
                $f=New-Element $xml 'formula';$f.InnerText=$formula;[void]$rule.AppendChild($f);[void]$cf.AppendChild($rule)
                [void]$xml.DocumentElement.InsertBefore($cf,$dvRoot);$ruleCount++
                if(-not $isList){
                    foreach($oldDv in @($dvRoot.SelectNodes('s:dataValidation',$ns))){
                        if([string]$oldDv.sqref -eq "$($column)2:$($column)5000"){[void]$dvRoot.RemoveChild($oldDv)}
                    }
                    $dv=New-Element $xml 'dataValidation';$dv.SetAttribute('type','list');$dv.SetAttribute('allowBlank','1');$dv.SetAttribute('sqref',"$($column)2:$($column)5000")
                    $f1=New-Element $xml 'formula1';$f1.InnerText='INDIRECT("'+$targetRange+'")';[void]$dv.AppendChild($f1);[void]$dvRoot.AppendChild($dv)
                }
            }
            $dvRoot.SetAttribute('count',[string]$dvRoot.ChildNodes.Count)
            Write-Xml $zip $entry $xml
        }
    }finally{$zip.Dispose()}
    if((Get-AuthoringFileSha256 $source) -ne $expected){throw '[AUTHORING_EXTERNAL_DRIFT] Source changed during formatting.'}
    $after=Get-AuthoringWorkbookContentSnapshot $candidate
    $content=Compare-AuthoringWorkbookContent $baseline $after
    $structure=Compare-AuthoringWorkbookStructure $baseline $after
    if(-not $content.isEqual -or -not $structure.isEqual){throw "[AUTHORING_FORMAT_CONTENT_DRIFT] content=$($content.differenceCount), structure=$($structure.differenceCount)"}
    [pscustomobject]@{action='FORMAT_CANDIDATE';source=$source;candidate=$candidate;sourceSha256=$expected;candidateSha256=(Get-AuthoringFileSha256 $candidate);sheetCount=$baseline.sheetCount;repairedStyleZero=$restoredStyles;referenceRules=$ruleCount;contentDifferenceCount=0;structureDifferenceCount=0}
}catch{
    if(Test-Path -LiteralPath $candidate){Remove-Item -LiteralPath $candidate -Force}
    throw
}
