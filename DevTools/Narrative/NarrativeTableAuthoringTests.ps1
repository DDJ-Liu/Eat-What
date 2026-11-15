# Fixture group called only from the existing Test-Narrative entry.
function Invoke-NarrativeTableAuthoringTests {
    param([string]$RepositoryRoot,[string]$OutputRoot)
    . (Join-Path $RepositoryRoot 'DevTools/DataTables/Authoring/Authoring.Common.ps1')
    [IO.Directory]::CreateDirectory($OutputRoot)|Out-Null
    $script:tableAuthoringChecks=0
    function Assert-TableAuthoring([bool]$Value,[string]$Message) { $script:tableAuthoringChecks++; if(-not $Value){throw $Message} }
    function Write-FixturePart($Archive,[string]$Name,[string]$Text) {
        if($Archive.Mode -eq [IO.Compression.ZipArchiveMode]::Update){$old=$Archive.GetEntry($Name);if($old){$old.Delete()}}
        $entry=$Archive.CreateEntry($Name);$writer=[IO.StreamWriter]::new($entry.Open(),[Text.UTF8Encoding]::new($false))
        try{$writer.Write($Text)}finally{$writer.Dispose()}
    }
    $newNames=@('Characters','Dialogues','DialogueLines','CGs','Scenes','SceneStates','SceneStatePlacements','AmbientPools')
    $dataRoot=Join-Path $OutputRoot 'DataTables'
    [IO.Directory]::CreateDirectory($dataRoot)|Out-Null
    Copy-Item -LiteralPath (Join-Path $RepositoryRoot 'DataTables/Schemas') -Destination $dataRoot -Recurse
    $schemas=@(Get-ChildItem (Join-Path $dataRoot 'Schemas') -Recurse -File -Filter '*.schema.json'|Sort-Object FullName|ForEach-Object{Get-Content $_.FullName -Raw|ConvertFrom-Json})
    foreach($schema in $schemas){
        $source=if($schema.tableName -in $newNames){Join-Path $RepositoryRoot ('DevTools/Narrative/Fixtures/TablesV25/'+$schema.csvPath)}else{Join-Path $RepositoryRoot ('DataTables/'+$schema.csvPath)}
        $target=Join-Path $dataRoot $schema.csvPath
        [IO.Directory]::CreateDirectory((Split-Path -Parent $target))|Out-Null
        Copy-Item -LiteralPath $source -Destination $target
    }
    $notePlan=(Invoke-AuthoringNode -Command 'header-comment-plan' -Arguments @{'data-root'=$dataRoot})|ConvertFrom-Json -Depth 30
    Assert-TableAuthoring ($notePlan.tableCount -eq 22 -and $notePlan.fieldCount -eq 224 -and @($notePlan.tables.comments.text|Select-Object -Unique).Count -eq 224) 'all 224 header explanations are distinct and table-aware'
    # A deliberately independent old workbook: 14 old headers, typed/manual
    # content and a hidden unknown sheet. No CSV rebuild of this workbook.
    $sourceBook=Join-Path $OutputRoot 'existing-authoring.xlsx'
    $archive=[IO.Compression.ZipFile]::Open($sourceBook,[IO.Compression.ZipArchiveMode]::Create)
    $ns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'
    $office='http://schemas.openxmlformats.org/officeDocument/2006/relationships'
    $sheets=[Text.StringBuilder]::new();$rels=[Text.StringBuilder]::new();$types=[Text.StringBuilder]::new();$index=0
    try{
        $oldSchemas=@($schemas|Where-Object tableName -NotIn $newNames)
        foreach($schema in $oldSchemas){
            $index++;[void]$sheets.Append('<sheet name="'+$schema.tableName+'" sheetId="'+$index+'" r:id="rId'+$index+'"/>')
            [void]$rels.Append('<Relationship Id="rId'+$index+'" Type="'+$office+'/worksheet" Target="worksheets/sheet'+$index+'.xml"/>')
            [void]$types.Append('<Override PartName="/xl/worksheets/sheet'+$index+'.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>')
            $cells=[Text.StringBuilder]::new();$column=0
            foreach($field in $schema.columns){$column++;[void]$cells.Append('<c r="'+(Get-AuthoringColumnAddress $column)+'1" t="inlineStr"><is><t>'+[Security.SecurityElement]::Escape([string]$field.csvColumn)+'</t></is></c>')}
            $row=if($index -eq 1){'<row r="2"><c r="A2" t="inlineStr"><is><t>manual-original</t></is></c><c r="B2" t="b"><v>0</v></c><c r="C2"><v>12.50</v></c><c r="D2"><f>1+2</f><v>3</v></c></row>'}else{''}
            Write-FixturePart $archive "xl/worksheets/sheet$index.xml" ('<worksheet xmlns="'+$ns+'"><dimension ref="A1:'+(Get-AuthoringColumnAddress ([math]::Max(4,$column)))+'2"/><sheetData><row r="1">'+$cells.ToString()+'</row>'+$row+'</sheetData></worksheet>')
        }
        $index++;[void]$sheets.Append('<sheet name="UserNotes" sheetId="'+$index+'" state="hidden" r:id="rId'+$index+'"/>')
        [void]$rels.Append('<Relationship Id="rId'+$index+'" Type="'+$office+'/worksheet" Target="worksheets/sheet'+$index+'.xml"/>')
        [void]$types.Append('<Override PartName="/xl/worksheets/sheet'+$index+'.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>')
        Write-FixturePart $archive "xl/worksheets/sheet$index.xml" ('<worksheet xmlns="'+$ns+'"><dimension ref="A1:D2"/><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>用户自建页</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>不能覆盖</t></is></c><c r="B2" t="b"><v>1</v></c><c r="C2"><v>-5.25</v></c><c r="D2"><f t="normal">SUM(1,2)</f><v>3</v></c></row></sheetData></worksheet>')
        Write-FixturePart $archive 'xl/workbook.xml' ('<workbook xmlns="'+$ns+'" xmlns:r="'+$office+'"><bookViews><workbookView/></bookViews><sheets>'+$sheets.ToString()+'</sheets></workbook>')
        Write-FixturePart $archive 'xl/_rels/workbook.xml.rels' ('<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'+$rels.ToString()+'<Relationship Id="styles" Type="'+$office+'/styles" Target="styles.xml"/></Relationships>')
        Write-FixturePart $archive '_rels/.rels' ('<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="root" Type="'+$office+'/officeDocument" Target="xl/workbook.xml"/></Relationships>')
        Write-FixturePart $archive 'xl/styles.xml' ([IO.File]::ReadAllText((Join-Path $RepositoryRoot 'DevTools/Narrative/Fixtures/TablesV25/AuthoringStyles.xml')))
        Write-FixturePart $archive '[Content_Types].xml' ('<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>'+$types.ToString()+'</Types>')
    }finally{$archive.Dispose()}
    $baseline=Get-AuthoringWorkbookContentSnapshot $sourceBook
    $baseline|ConvertTo-Json -Depth 25|Set-Content (Join-Path $OutputRoot 'before.json')
    Assert-TableAuthoring ($baseline.sheetCount -eq 15) 'seed has 14 old plus unknown sheet'
    $entry=Join-Path $RepositoryRoot 'DevTools/DataTables/Authoring/New-AuthoringWorkbook.ps1'
    & $entry -DataTablesRoot $dataRoot -OutputWorkbook $sourceBook -AddSchemaTables $newNames -TaskOrigin 'AI534-FIXTURE' -AuditPath (Join-Path $OutputRoot 'append.audit.jsonl') -ExpectedWorkbookSha256 $baseline.sourceSha256
    $after=Get-AuthoringWorkbookContentSnapshot $sourceBook
    $diff=Compare-AuthoringWorkbookContent $baseline $after
    Assert-TableAuthoring ($after.sheetCount -eq 23 -and $diff.differenceCount -eq 8 -and @($diff.differences|Where-Object kind -NE 'SHEET_ADDED').Count -eq 0) 'only eight header sheets added; old values/types/formulas/order/hidden unknown sheet preserved'
    Assert-TableAuthoring ((Compare-AuthoringWorkbookStructure $baseline $after).isEqual) 'old blank structure preserved'
    Assert-TableAuthoring (@($after.sheets|Where-Object name -In $newNames|ForEach-Object cells).Count -eq 105) '105 new headers and no data'
    & $entry -DataTablesRoot $dataRoot -OutputWorkbook $sourceBook -AddSchemaTables $newNames -TaskOrigin 'AI534-IDEMPOTENCE' -AuditPath (Join-Path $OutputRoot 'idempotent.audit.jsonl') -ExpectedWorkbookSha256 $after.sourceSha256
    Assert-TableAuthoring ((Get-AuthoringFileSha256 $sourceBook) -eq $after.sourceSha256) 'idempotent no write'
    $negative=Join-Path $OutputRoot 'old-content-changed.xlsx';Copy-Item $sourceBook $negative
    $archive=[IO.Compression.ZipFile]::Open($negative,[IO.Compression.ZipArchiveMode]::Update)
    try{$text=Read-AuthoringZipEntryText $archive 'xl/worksheets/sheet1.xml';Write-FixturePart $archive 'xl/worksheets/sheet1.xml' $text.Replace('manual-original','overwritten')}finally{$archive.Dispose()}
    $refused=$false
    try{Publish-AuthoringWorkbookChange -SourceWorkbook $sourceBook -CandidateWorkbook $negative -Mode BlankStructureOnly -AllowedNewHeaderSheets @([pscustomobject]@{name='NotActuallyNew';headers=@('x')}) -ExpectedSourceSha256 $after.sourceSha256 -BackupPath (Join-Path $OutputRoot 'must-not-backup.xlsx') -TaskOrigin 'AI534-EXPECTED-REJECTION' -AuditPath (Join-Path $OutputRoot 'refusal.audit.jsonl')|Out-Null}catch{if($_.Exception.Message -notlike '*AUTHORING_CONTENT_PROTECTED*'){throw};$refused=$true}
    Assert-TableAuthoring ($refused -and (Get-AuthoringFileSha256 $sourceBook) -eq $after.sourceSha256) 'old value overwrite rejected before write'
    $comments=Join-Path $OutputRoot 'with-comments.xlsx'
    $commentEntry=Join-Path $RepositoryRoot 'DevTools/DataTables/Authoring/Update-HeaderComments.ps1'
    $commentReceipt=& $commentEntry -InputWorkbook $sourceBook -CandidateWorkbook $comments -ExpectedInputSha256 $after.sourceSha256 -DataTablesRoot $dataRoot -InitializeSchemaTables @($schemas.tableName)
    $commentReceipt|Write-Output
    Assert-TableAuthoring ($commentReceipt.initializedThreads -eq 224) 'explicit initialization creates exactly 224 notes'
    Assert-TableAuthoring ((Compare-AuthoringWorkbookContent $after (Get-AuthoringWorkbookContentSnapshot $comments)).isEqual) '224 header notes preserve full typed content'
    $repeatComments=Join-Path $OutputRoot 'repeat-comments.xlsx'
    $repeatReceipt=& $commentEntry -InputWorkbook $comments -CandidateWorkbook $repeatComments -ExpectedInputSha256 (Get-AuthoringFileSha256 $comments) -DataTablesRoot $dataRoot
    Assert-TableAuthoring ($repeatReceipt.initializedThreads -eq 0 -and $repeatReceipt.updatedThreads -eq 0 -and $repeatReceipt.removedToolThreads -eq 0) 'default repeat preserves all existing notes without initialization'
    $userComments=Join-Path $OutputRoot 'user-comments.xlsx';Copy-Item $comments $userComments
    $archive=[IO.Compression.ZipFile]::Open($userComments,[IO.Compression.ZipArchiveMode]::Update)
    try{
        $part=@($archive.Entries|Where-Object FullName -Like 'xl/threadedcomments/*')[0].FullName
        [xml]$document=Read-AuthoringZipEntryText $archive $part
        $document.DocumentElement.FirstChild.FirstChild.InnerText='USER_EDITED_NOTE_MUST_SURVIVE'
        Write-FixturePart $archive $part $document.OuterXml
    }finally{$archive.Dispose()}
    $userHash=Get-AuthoringFileSha256 $userComments;$refused=$false
    try{& $commentEntry -InputWorkbook $userComments -CandidateWorkbook (Join-Path $OutputRoot 'must-refuse-user-note.xlsx') -ExpectedInputSha256 $userHash -DataTablesRoot $dataRoot -InitializeSchemaTables @($schemas.tableName)|Out-Null}catch{if($_.Exception.Message -notlike '*HEADER_COMMENT_NO_TOOL_THREAD*'){throw};$refused=$true}
    Assert-TableAuthoring ($refused -and (Get-AuthoringFileSha256 $userComments) -eq $userHash) 'explicit initialization cannot replace an edited existing note'
    $formatted=Join-Path $OutputRoot 'formatted.xlsx'
    & (Join-Path $RepositoryRoot 'DevTools/DataTables/Authoring/Format-AuthoringWorkbook.ps1') -InputWorkbook $comments -CandidateWorkbook $formatted -ExpectedInputSha256 (Get-AuthoringFileSha256 $comments) -DataTablesRoot $dataRoot
    Assert-TableAuthoring ((Compare-AuthoringWorkbookContent $after (Get-AuthoringWorkbookContentSnapshot $formatted)).isEqual) 'format preserves full typed content including unknown page'
    $sampleBook=Join-Path $OutputRoot 'sample-roundtrip.xlsx'
    & $entry -DataTablesRoot $dataRoot -OutputWorkbook $sampleBook
    $sampleBefore=Get-AuthoringWorkbookContentSnapshot $sampleBook
    $exportRoot=Join-Path $OutputRoot 'exported'
    Invoke-AuthoringNode -Command export -Arguments @{'data-root'=$dataRoot;input=$sampleBook;'output-root'=$exportRoot;'baseline-root'=$dataRoot;'expected-input-sha'=$sampleBefore.sourceSha256;'provenance-output'=(Join-Path $OutputRoot 'provenance.json')}|Write-Output
    foreach($schema in $schemas|Where-Object tableName -In $newNames){
        $expected=[EatWhat.DataTables.DataTableCsvParser]::Parse([IO.File]::ReadAllText((Join-Path $dataRoot $schema.csvPath)))
        $actual=[EatWhat.DataTables.DataTableCsvParser]::Parse([IO.File]::ReadAllText((Join-Path $exportRoot $schema.csvPath)))
        Assert-TableAuthoring ([Linq.Enumerable]::SequenceEqual([string[]]$expected.Table.Headers,[string[]]$actual.Table.Headers)) ($schema.tableName+' header export')
        Assert-TableAuthoring ($expected.Table.Rows.Count -eq $actual.Table.Rows.Count) ($schema.tableName+' row count')
        for($i=0;$i -lt $expected.Table.Rows.Count;$i++){Assert-TableAuthoring ([Linq.Enumerable]::SequenceEqual([string[]]$expected.Table.Rows[$i].Values,[string[]]$actual.Table.Rows[$i].Values)) ($schema.tableName+' all raw cells incl empty/0/false/comment')}
    }
    Assert-TableAuthoring ((Get-AuthoringFileSha256 $sampleBook) -eq $sampleBefore.sourceSha256) 'export does not write source workbook'
    # AI537_BEGIN: selected-table regression; the original 49 checks above remain unchanged.
    $scopeOriginalChecks=$script:tableAuthoringChecks
    $scopeSource=Join-Path $OutputRoot 'scope-source.xlsx';Copy-Item $comments $scopeSource
    $scopeOldParts=[Collections.Generic.List[string]]::new()
    $scopeSheetParts=@{}
    $scopeArchive=[IO.Compression.ZipFile]::Open($scopeSource,[IO.Compression.ZipArchiveMode]::Update)
    try{
        [xml]$scopeBook=Read-AuthoringZipEntryText $scopeArchive 'xl/workbook.xml'
        [xml]$scopeBookRels=Read-AuthoringZipEntryText $scopeArchive 'xl/_rels/workbook.xml.rels'
        [xml]$scopeTypes=Read-AuthoringZipEntryText $scopeArchive '[Content_Types].xml'
        foreach($scopeSheet in $scopeBook.SelectNodes('//*[local-name()="sheets"]/*[local-name()="sheet"]')){
            $scopeName=$scopeSheet.GetAttribute('name')
            if($scopeName -notin $schemas.tableName){continue}
            $scopeId=$scopeSheet.GetAttribute('id','http://schemas.openxmlformats.org/officeDocument/2006/relationships')
            $scopeRel=@($scopeBookRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Id') -ceq $scopeId})[0]
            $scopePart=[Uri]::new([Uri]'http://fixture/xl/workbook.xml',$scopeRel.GetAttribute('Target')).AbsolutePath.TrimStart('/')
            $scopeSheetParts[$scopeName]=$scopePart
            $scopeRelsPart=[IO.Path]::GetDirectoryName($scopePart).Replace('\','/')+'/_rels/'+[IO.Path]::GetFileName($scopePart)+'.rels'
            [xml]$scopeRels=Read-AuthoringZipEntryText $scopeArchive $scopeRelsPart
            $scopeCommentRels=@($scopeRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type').EndsWith('/comments',[StringComparison]::Ordinal) -or $_.GetAttribute('Type').EndsWith('/threadedComment',[StringComparison]::Ordinal)})
            $scopeCommentParts=@{}
            foreach($scopeRelation in $scopeCommentRels){
                $scopeKind=if($scopeRelation.GetAttribute('Type').EndsWith('/comments',[StringComparison]::Ordinal)){'legacy'}else{'threads'}
                $scopeCommentParts[$scopeKind]=[Uri]::new([Uri]('http://fixture/'+$scopePart),$scopeRelation.GetAttribute('Target')).AbsolutePath.TrimStart('/')
            }
            if($scopeName -in $newNames){
                foreach($scopeCommentPart in $scopeCommentParts.Values){
                    $scopeArchive.GetEntry($scopeCommentPart).Delete()
                    foreach($scopeOverride in @($scopeTypes.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('PartName') -ceq ('/'+$scopeCommentPart)})){[void]$scopeTypes.DocumentElement.RemoveChild($scopeOverride)}
                }
                foreach($scopeRelation in $scopeCommentRels){[void]$scopeRels.DocumentElement.RemoveChild($scopeRelation)}
                Write-FixturePart $scopeArchive $scopeRelsPart $scopeRels.OuterXml
            }else{
                [void]$scopeOldParts.Add($scopeRelsPart)
                foreach($scopeCommentPart in $scopeCommentParts.Values){[void]$scopeOldParts.Add($scopeCommentPart)}
                if($scopeName -eq 'Items'){
                    [xml]$scopeThreads=Read-AuthoringZipEntryText $scopeArchive $scopeCommentParts.threads
                    [xml]$scopeLegacy=Read-AuthoringZipEntryText $scopeArchive $scopeCommentParts.legacy
                    $scopePrimary=@($scopeThreads.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('ref') -eq 'M1' -and -not $_.HasAttribute('parentId')})[0]
                    $scopeOldText=[regex]::Replace($scopePrimary.FirstChild.InnerText,'(?m)^示例：.*$','示例：参考图1（旧说明保留）')
                    Assert-TableAuthoring ($scopeOldText -cne $scopePrimary.FirstChild.InnerText -and $scopeOldText.Contains('用途/填法：') -and $scopeOldText.Contains('消费点：') -and $scopeOldText.Contains('单位：') -and $scopeOldText.Contains('类型：')) 'fixture retains complete old explanation with an outdated sample'
                    $scopePrimary.FirstChild.InnerText=$scopeOldText
                    $scopeLegacyNode=$scopeLegacy.SelectSingleNode('//*[local-name()="comment" and @ref="M1"]/*[local-name()="text"]/*[local-name()="t"]')
                    $scopeLegacyNode.InnerText=$scopeOldText
                    $scopeReply=$scopeThreads.CreateElement('xltc','threadedComment',$scopeThreads.DocumentElement.NamespaceURI)
                    foreach($scopeAttribute in @('ref','personId','dT')){$scopeReply.SetAttribute($scopeAttribute,$scopePrimary.GetAttribute($scopeAttribute))}
                    $scopeReply.SetAttribute('id','{'+[guid]::NewGuid().ToString().ToUpperInvariant()+'}');$scopeReply.SetAttribute('parentId',$scopePrimary.GetAttribute('id'))
                    $scopeReplyText=$scopeThreads.CreateElement('xltc','text',$scopeThreads.DocumentElement.NamespaceURI);$scopeReplyText.InnerText='人工回复：保留历史备注'
                    [void]$scopeReply.AppendChild($scopeReplyText);[void]$scopeThreads.DocumentElement.AppendChild($scopeReply)
                    Write-FixturePart $scopeArchive $scopeCommentParts.threads $scopeThreads.OuterXml
                    Write-FixturePart $scopeArchive $scopeCommentParts.legacy $scopeLegacy.OuterXml
                }
            }
        }
        Write-FixturePart $scopeArchive '[Content_Types].xml' $scopeTypes.OuterXml
    }finally{$scopeArchive.Dispose()}
    [void]$scopeOldParts.Add('xl/persons/person.xml')
    $scopeBefore=Get-AuthoringWorkbookContentSnapshot $scopeSource
    Assert-TableAuthoring ((Compare-AuthoringWorkbookContent $after $scopeBefore).isEqual -and (Compare-AuthoringWorkbookStructure $after $scopeBefore).isEqual) 'scope fixture changes notes only; all original values/types/formulas/structure retained'
    $scopeOldTextByPart=@{};$scopeArchive=[IO.Compression.ZipFile]::OpenRead($scopeSource)
    try{foreach($scopePart in $scopeOldParts){$scopeOldTextByPart[$scopePart]=Read-AuthoringZipEntryText $scopeArchive $scopePart}}finally{$scopeArchive.Dispose()}
    $scopeCandidate=Join-Path $OutputRoot 'scope-new-eight.xlsx'
    $scopeReceipt=& $commentEntry -InputWorkbook $scopeSource -CandidateWorkbook $scopeCandidate -ExpectedInputSha256 $scopeBefore.sourceSha256 -DataTablesRoot $dataRoot -UpdateSchemaTables $newNames -InitializeSchemaTables $newNames
    Assert-TableAuthoring ($scopeReceipt.tableCount -eq 22 -and $scopeReceipt.fieldCount -eq 224 -and $scopeReceipt.updateTableCount -eq 8 -and $scopeReceipt.initializedThreads -eq 105 -and $scopeReceipt.updatedThreads -eq 0 -and $scopeReceipt.removedToolThreads -eq 0) 'selected eight initialize 105 notes under the full 22/224 plan'
    $scopeAfter=Get-AuthoringWorkbookContentSnapshot $scopeCandidate
    Assert-TableAuthoring ((Compare-AuthoringWorkbookContent $scopeBefore $scopeAfter).isEqual -and (Compare-AuthoringWorkbookStructure $scopeBefore $scopeAfter).isEqual) 'selected update preserves all workbook values/types/formulas/order/hidden structure'
    $scopePartsEqual=$true;$scopeTopCount=0;$scopeReplyCount=0;$scopeLegacyCount=0
    $scopeArchive=[IO.Compression.ZipFile]::OpenRead($scopeCandidate)
    try{
        foreach($scopePart in $scopeOldParts){if((Read-AuthoringZipEntryText $scopeArchive $scopePart) -cne $scopeOldTextByPart[$scopePart]){$scopePartsEqual=$false}}
        foreach($scopeEntry in @($scopeArchive.Entries|Where-Object FullName -Like 'xl/threadedcomments/*.xml')){
            [xml]$scopeDocument=Read-AuthoringZipEntryText $scopeArchive $scopeEntry.FullName
            $scopeTopCount+=@($scopeDocument.DocumentElement.ChildNodes|Where-Object{-not $_.HasAttribute('parentId')}).Count
            $scopeReplyCount+=@($scopeDocument.DocumentElement.ChildNodes|Where-Object{$_.HasAttribute('parentId')}).Count
        }
        foreach($scopeEntry in @($scopeArchive.Entries|Where-Object FullName -Like 'xl/comments*.xml')){[xml]$scopeDocument=Read-AuthoringZipEntryText $scopeArchive $scopeEntry.FullName;$scopeLegacyCount+=$scopeDocument.SelectNodes('//*[local-name()="comment"]').Count}
    }finally{$scopeArchive.Dispose()}
    Assert-TableAuthoring ($scopePartsEqual -and $scopeOldParts.Count -eq 43 -and $scopeReceipt.commentProtectionAudit.protectedNodes -gt 119) 'old 119 notes, reply, legacy authors/companions, sheet relationships and person part preserved verbatim; global audit retained'
    Assert-TableAuthoring ($scopeTopCount -eq 224 -and $scopeReplyCount -eq 1 -and $scopeLegacyCount -eq 224) '119 old plus 105 new explanations cover all 224 headers with the old reply intact'
    $scopeHuman=Join-Path $OutputRoot 'scope-human-new.xlsx';Copy-Item $scopeCandidate $scopeHuman
    $scopeArchive=[IO.Compression.ZipFile]::Open($scopeHuman,[IO.Compression.ZipArchiveMode]::Update)
    try{
        $scopeCharacterRelsPart=[IO.Path]::GetDirectoryName($scopeSheetParts.Characters).Replace('\','/')+'/_rels/'+[IO.Path]::GetFileName($scopeSheetParts.Characters)+'.rels'
        [xml]$scopeCharacterRels=Read-AuthoringZipEntryText $scopeArchive $scopeCharacterRelsPart
        $scopeThreadRel=@($scopeCharacterRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type').EndsWith('/threadedComment',[StringComparison]::Ordinal)})[0]
        $scopeHumanPart=[Uri]::new([Uri]('http://fixture/'+$scopeSheetParts.Characters),$scopeThreadRel.GetAttribute('Target')).AbsolutePath.TrimStart('/')
        [xml]$scopeHumanDocument=Read-AuthoringZipEntryText $scopeArchive $scopeHumanPart;$scopeHumanDocument.DocumentElement.FirstChild.FirstChild.InnerText='人工新页备注：禁止覆盖'
        Write-FixturePart $scopeArchive $scopeHumanPart $scopeHumanDocument.OuterXml
    }finally{$scopeArchive.Dispose()}
    $scopeBadHeader=Join-Path $OutputRoot 'scope-unselected-header.xlsx';Copy-Item $scopeSource $scopeBadHeader
    $scopeArchive=[IO.Compression.ZipFile]::Open($scopeBadHeader,[IO.Compression.ZipArchiveMode]::Update)
    try{
        [xml]$scopeHeaderDocument=Read-AuthoringZipEntryText $scopeArchive $scopeSheetParts.Items
        $scopeHeaderDocument.SelectSingleNode('//*[local-name()="c" and @r="M1"]/*[local-name()="is"]/*[local-name()="t"]').InnerText='WRONG_HEADER'
        Write-FixturePart $scopeArchive $scopeSheetParts.Items $scopeHeaderDocument.OuterXml
    }finally{$scopeArchive.Dispose()}
    $scopeCases=@(
        @{name='default-all';parameters=@{InitializeSchemaTables=$newNames};expected='*HEADER_COMMENT_NO_TOOL_THREAD*Items!M1*'},
        @{name='selected-old-unknown';parameters=@{UpdateSchemaTables=@('Items');InitializeSchemaTables=@('Items')};expected='*HEADER_COMMENT_NO_TOOL_THREAD*Items!M1*'},
        @{name='selected-new-human';source=$scopeHuman;parameters=@{UpdateSchemaTables=$newNames;InitializeSchemaTables=$newNames};expected='*HEADER_COMMENT_NO_TOOL_THREAD*Characters!A1*'},
        @{name='unknown-table';parameters=@{UpdateSchemaTables=@('NoSuchTable')};expected='*HEADER_COMMENT_UPDATE_SCOPE*'},
        @{name='duplicate-table';parameters=@{UpdateSchemaTables=@('Characters','Characters')};expected='*HEADER_COMMENT_UPDATE_SCOPE*'},
        @{name='case-sensitive';parameters=@{UpdateSchemaTables=@('characters')};expected='*HEADER_COMMENT_UPDATE_SCOPE*'},
        @{name='empty-explicit';parameters=@{UpdateSchemaTables=[string[]]@()};expected='*HEADER_COMMENT_UPDATE_SCOPE*'},
        @{name='initialize-outside';parameters=@{UpdateSchemaTables=$newNames;InitializeSchemaTables=@('Items')};expected='*HEADER_COMMENT_INITIALIZE_SCOPE*'},
        @{name='unselected-header';source=$scopeBadHeader;parameters=@{UpdateSchemaTables=$newNames;InitializeSchemaTables=$newNames};expected='*HEADER_COMMENT_HEADER_DRIFT*Items!M1*'},
        @{name='source-drift';expectedSha=('0'*64);parameters=@{UpdateSchemaTables=$newNames;InitializeSchemaTables=$newNames};expected='*HEADER_COMMENT_SOURCE_DRIFT*'}
    )
    foreach($scopeCase in $scopeCases){
        $scopeCaseSource=if($scopeCase.ContainsKey('source')){$scopeCase.source}else{$scopeSource}
        $scopeCaseHash=Get-AuthoringFileSha256 $scopeCaseSource
        $scopeCaseExpected=if($scopeCase.ContainsKey('expectedSha')){$scopeCase.expectedSha}else{$scopeCaseHash}
        $scopeRejected=Join-Path $OutputRoot ('scope-refused-'+$scopeCase.name+'.xlsx');$scopeRefused=$false
        $scopeArguments=$scopeCase.parameters
        try{& $commentEntry -InputWorkbook $scopeCaseSource -CandidateWorkbook $scopeRejected -ExpectedInputSha256 $scopeCaseExpected -DataTablesRoot $dataRoot @scopeArguments|Out-Null}catch{if($_.Exception.Message -notlike $scopeCase.expected){throw};$scopeRefused=$true}
        Assert-TableAuthoring ($scopeRefused -and -not (Test-Path -LiteralPath $scopeRejected) -and (Get-AuthoringFileSha256 $scopeCaseSource) -eq $scopeCaseHash) ('scope refusal without source write: '+$scopeCase.name)
    }
    Assert-TableAuthoring ((Get-AuthoringFileSha256 $scopeSource) -eq $scopeBefore.sourceSha256) 'scope source bytes remain unchanged after all positive and negative cases'
    [pscustomobject]@{passed=$true;originalChecks=$scopeOriginalChecks;additionalChecks=($script:tableAuthoringChecks-$scopeOriginalChecks);updatedTables=8;initializedNotes=105;oldNotesPreserved=119;fullHeaderCoverage=$scopeTopCount;oldReplies=$scopeReplyCount;oldLegacyAndRelationshipParts=$scopeOldParts.Count;globalCommentAudit=$scopeReceipt.commentProtectionAudit;negativeCases=@($scopeCases.name);sourceWorkbookUnchanged=$true}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $OutputRoot 'scope-result.json')
    # AI537_END
    # AI539_BEGIN: missing empty-sheet targets; original 49+17 checks remain unchanged.
    $emptyOriginalChecks=$script:tableAuthoringChecks
    $emptyData=Join-Path $OutputRoot 'empty-data';Copy-Item -LiteralPath $dataRoot -Destination $emptyData -Recurse
    foreach($emptySchema in $schemas|Where-Object tableName -In $newNames){[IO.File]::WriteAllText((Join-Path $emptyData $emptySchema.csvPath),([string]::Join(',',[string[]]$emptySchema.columns.csvColumn)+"`r`n"),[Text.UTF8Encoding]::new($false))}
    $emptyBook=Join-Path $OutputRoot 'eight-empty.xlsx'; & $entry -DataTablesRoot $emptyData -OutputWorkbook $emptyBook
    $emptyBefore=Get-AuthoringWorkbookContentSnapshot $emptyBook
    $emptyFresh=Join-Path $OutputRoot 'empty-fresh-node';$emptyProvenance=Join-Path $OutputRoot 'empty-fresh-provenance.json'
    $emptyReceipt=(Invoke-AuthoringNode -Command export -Arguments @{'data-root'=$emptyData;input=$emptyBook;'output-root'=$emptyFresh;'baseline-root'=$emptyData;'expected-input-sha'=$emptyBefore.sourceSha256;'provenance-output'=$emptyProvenance})|ConvertFrom-Json -Depth 30
    Assert-TableAuthoring ($emptyReceipt.written.Count -eq 22 -and @($emptyReceipt.headerOnlyCsv).Count -eq 8 -and @($emptyReceipt.skipped).Count -eq 0) 'fresh Node export materializes all 22 tables including eight empty sheets'
    $emptyHeaderColumns=0
    foreach($emptySchema in $schemas|Where-Object tableName -In $newNames){
        $emptyCsv=[EatWhat.DataTables.DataTableCsvParser]::Parse([IO.File]::ReadAllText((Join-Path $emptyFresh $emptySchema.csvPath)))
        $emptyHeaderColumns+=$emptyCsv.Table.Headers.Count
        Assert-TableAuthoring ($emptyCsv.IsValid -and $emptyCsv.Table.Rows.Count -eq 0 -and [Linq.Enumerable]::SequenceEqual([string[]]$emptyCsv.Table.Headers,[string[]]$emptySchema.columns.csvColumn)) ($emptySchema.tableName+' has exact header order and zero business rows')
    }
    Assert-TableAuthoring ($emptyHeaderColumns -eq 105) 'eight empty CSV headers total 105 columns'
    $emptyOrigin=Get-Content $emptyProvenance -Raw|ConvertFrom-Json -Depth 40
    Assert-TableAuthoring (@($emptyOrigin.headerOnlyCsv).Count -eq 8 -and @($emptyOrigin.headerOnlyCsv|Where-Object{$_.dataRowCount -ne 0 -or $_.source -ne 'workbook-schema-header'}).Count -eq 0 -and @($emptyOrigin.cells|Where-Object sheet -In $newNames).Count -eq 0) 'header-only provenance is separate and contains no fabricated business cells'
    $emptyWrapper=Join-Path $OutputRoot 'empty-fresh-wrapper';$emptyWrapperReports=Join-Path $OutputRoot 'empty-wrapper-reports'
    $emptyWrapperOutput=@(& (Join-Path $RepositoryRoot 'DevTools/DataTables/Authoring/Export-AuthoringToCsv.ps1') -InputWorkbook $emptyBook -OutputRoot $emptyWrapper -BaselineRoot $emptyData -ReportDirectory $emptyWrapperReports -ExpectedWorkbookSha256 $emptyBefore.sourceSha256)
    $emptyWrapperOutput|Set-Content (Join-Path $OutputRoot 'empty-wrapper.stdout.txt')
    $emptyWrapperReceipt=@($emptyWrapperOutput|Where-Object{$_ -is [string] -and $_ -match '^\{'}|Select-Object -Last 1)[0]|ConvertFrom-Json
    Assert-TableAuthoring ($emptyWrapperReceipt.isValid -eq $true -and $emptyWrapperReceipt.errorCount -eq 0 -and $emptyWrapperReceipt.productionPublished -eq $false -and @(Get-ChildItem $emptyWrapper -Recurse -File -Filter '*.csv').Count -eq 22) 'actual PS export wrapper validates fresh 22-table output without production publishing'
    $emptyReimport=Join-Path $OutputRoot 'empty-reimport.xlsx'; & $entry -DataTablesRoot $emptyWrapper -OutputWorkbook $emptyReimport
    Assert-TableAuthoring ((Compare-AuthoringWorkbookContent $emptyBefore (Get-AuthoringWorkbookContentSnapshot $emptyReimport)).isEqual) 'fresh wrapper output rebuilds a workbook with identical values/types/formulas/header-only sheets'
    $emptyExisting=Join-Path $OutputRoot 'empty-existing-targets'
    $emptyCharacter=@($schemas|Where-Object tableName -EQ 'Characters')[0];$emptyCg=@($schemas|Where-Object tableName -EQ 'CGs')[0]
    $emptyConfiguredPath=Join-Path $emptyExisting $emptyCharacter.csvPath;$emptyHeaderPath=Join-Path $emptyExisting $emptyCg.csvPath
    [IO.Directory]::CreateDirectory((Split-Path -Parent $emptyConfiguredPath))|Out-Null
    $emptyKeepCells=New-Object string[] $emptyCharacter.columns.Count;$emptyKeepCells[0]='chr_keep';$emptyKeepCells[$emptyKeepCells.Length-1]='EXISTING_CONFIG_MUST_SURVIVE'
    [IO.File]::WriteAllText($emptyConfiguredPath,([string]::Join(',',[string[]]$emptyCharacter.columns.csvColumn)+"`r`n"+[string]::Join(',',$emptyKeepCells)+"`r`n"),[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllBytes($emptyHeaderPath,([byte[]]@(239,187,191)+[Text.Encoding]::UTF8.GetBytes([string]::Join(',',[string[]]$emptyCg.columns.csvColumn)+"`n")))
    $emptyConfiguredHash=Get-AuthoringFileSha256 $emptyConfiguredPath;$emptyHeaderHash=Get-AuthoringFileSha256 $emptyHeaderPath
    $emptyExistingReceipt=(Invoke-AuthoringNode -Command export -Arguments @{'data-root'=$emptyData;input=$emptyBook;'output-root'=$emptyExisting;'baseline-root'=$emptyData;'expected-input-sha'=$emptyBefore.sourceSha256})|ConvertFrom-Json -Depth 30
    Assert-TableAuthoring ($emptyExistingReceipt.skipped -contains $emptyCharacter.csvPath -and $emptyExistingReceipt.skipped -contains $emptyCg.csvPath -and (Get-AuthoringFileSha256 $emptyConfiguredPath) -eq $emptyConfiguredHash -and (Get-AuthoringFileSha256 $emptyHeaderPath) -eq $emptyHeaderHash) 'existing configured CSV and BOM/LF header-only CSV remain byte-identical'
    $emptyAgain=(Invoke-AuthoringNode -Command export -Arguments @{'data-root'=$emptyData;input=$emptyBook;'output-root'=$emptyExisting;'baseline-root'=$emptyData;'expected-input-sha'=$emptyBefore.sourceSha256})|ConvertFrom-Json -Depth 30
    Assert-TableAuthoring (@($emptyAgain.headerOnlyCsv).Count -eq 0 -and @($emptyAgain.skipped).Count -eq 8 -and (Get-AuthoringFileSha256 $emptyConfiguredPath) -eq $emptyConfiguredHash -and (Get-AuthoringFileSha256 $emptyHeaderPath) -eq $emptyHeaderHash) 'repeat empty export skips every existing target without rewriting bytes'
    $emptyConfiguredBaseline=Join-Path $OutputRoot 'empty-configured-baseline';Copy-Item -LiteralPath $emptyData -Destination $emptyConfiguredBaseline -Recurse
    $emptyDialogue=@($schemas|Where-Object tableName -EQ 'Dialogues')[0]
    Copy-Item -LiteralPath (Join-Path $dataRoot $emptyDialogue.csvPath) -Destination (Join-Path $emptyConfiguredBaseline $emptyDialogue.csvPath)
    $emptyBaselineHash=Get-AuthoringFileSha256 (Join-Path $emptyConfiguredBaseline $emptyDialogue.csvPath)
    $emptyNoClear=Join-Path $OutputRoot 'empty-baseline-no-clear'
    $emptyNoClearReceipt=(Invoke-AuthoringNode -Command export -Arguments @{'data-root'=$emptyData;input=$emptyBook;'output-root'=$emptyNoClear;'baseline-root'=$emptyConfiguredBaseline;'expected-input-sha'=$emptyBefore.sourceSha256})|ConvertFrom-Json -Depth 30
    Assert-TableAuthoring (-not (Test-Path -LiteralPath (Join-Path $emptyNoClear $emptyDialogue.csvPath)) -and @($emptyNoClearReceipt.emptySheetSkips|Where-Object{$_.csvPath -eq $emptyDialogue.csvPath -and $_.reason -eq 'baseline-contains-data'}).Count -eq 1 -and (Get-AuthoringFileSha256 (Join-Path $emptyConfiguredBaseline $emptyDialogue.csvPath)) -eq $emptyBaselineHash) 'nonempty baseline is neither cleared nor copied into a missing target'
    $emptyMissingBaseline=Join-Path $OutputRoot 'empty-missing-baseline'
    foreach($emptySchema in $schemas|Where-Object tableName -NotIn $newNames){$emptyCopy=Join-Path $emptyMissingBaseline $emptySchema.csvPath;[IO.Directory]::CreateDirectory((Split-Path -Parent $emptyCopy))|Out-Null;Copy-Item -LiteralPath (Join-Path $emptyData $emptySchema.csvPath) -Destination $emptyCopy}
    $emptyMissingOutput=Join-Path $OutputRoot 'empty-baseline-missing-output'
    $emptyMissingReceipt=(Invoke-AuthoringNode -Command export -Arguments @{'data-root'=$emptyData;input=$emptyBook;'output-root'=$emptyMissingOutput;'baseline-root'=$emptyMissingBaseline;'expected-input-sha'=$emptyBefore.sourceSha256})|ConvertFrom-Json -Depth 30
    Assert-TableAuthoring (@($emptyMissingReceipt.headerOnlyCsv|Where-Object baseline -EQ 'missing').Count -eq 8 -and $emptyMissingReceipt.written.Count -eq 22) 'missing baselines permit safe missing header-only targets'
    $emptyInvalidCases=@(
        @{name='bad-header';bytes=[Text.Encoding]::UTF8.GetBytes("WRONG_HEADER`r`n");diagnostic='AUTHORING_EMPTY_BASELINE_HEADER'},
        @{name='bad-encoding';bytes=[byte[]]@(255);diagnostic='ERR_ENCODING_INVALID_ENCODED_DATA'}
    )
    foreach($emptyInvalid in $emptyInvalidCases){
        $emptyInvalidBaseline=Join-Path $OutputRoot ('empty-'+$emptyInvalid.name+'-baseline');Copy-Item -LiteralPath $emptyData -Destination $emptyInvalidBaseline -Recurse
        [IO.File]::WriteAllBytes((Join-Path $emptyInvalidBaseline $emptyCharacter.csvPath),$emptyInvalid.bytes)
        $emptyInvalidOutput=Join-Path $OutputRoot ('empty-'+$emptyInvalid.name+'-output');$emptyErrorPath=Join-Path $OutputRoot ('empty-'+$emptyInvalid.name+'.stderr.txt');$emptyRefused=$false
        try{Invoke-AuthoringNode -Command export -Arguments @{'data-root'=$emptyData;input=$emptyBook;'output-root'=$emptyInvalidOutput;'baseline-root'=$emptyInvalidBaseline;'expected-input-sha'=$emptyBefore.sourceSha256} 2> $emptyErrorPath|Out-Null}catch{$emptyRefused=$true}
        $emptyErrorText=if(Test-Path -LiteralPath $emptyErrorPath){[IO.File]::ReadAllText($emptyErrorPath)}else{''}
        Assert-TableAuthoring ($emptyRefused -and $emptyErrorText.Contains($emptyInvalid.diagnostic) -and -not (Test-Path -LiteralPath (Join-Path $emptyInvalidOutput $emptyCharacter.csvPath))) ('invalid baseline fails explicitly: '+$emptyInvalid.name)
    }
    Assert-TableAuthoring ((Get-AuthoringFileSha256 $emptyBook) -eq $emptyBefore.sourceSha256 -and (Compare-AuthoringWorkbookContent $emptyBefore (Get-AuthoringWorkbookContentSnapshot $emptyBook)).isEqual) 'all empty-sheet exports leave source workbook bytes and values/types/formulas unchanged'
    [pscustomobject]@{passed=$true;originalChecks=$emptyOriginalChecks;additionalChecks=($script:tableAuthoringChecks-$emptyOriginalChecks);freshNodeTables=$emptyReceipt.written.Count;headerOnlyTables=@($emptyReceipt.headerOnlyCsv).Count;headerColumns=$emptyHeaderColumns;wrapperValidated=$emptyWrapperReceipt.isValid;wrapperErrors=$emptyWrapperReceipt.errorCount;wrapperWarnings=$emptyWrapperReceipt.warningCount;rebuiltWorkbookEqual=$true;existingConfiguredAndHeaderBytesPreserved=$true;configuredBaselineNotCleared=$true;missingBaselineAllowed=$true;invalidBaselinesRefused=2;sourceWorkbookUnchanged=$true}|ConvertTo-Json -Depth 6|Set-Content (Join-Path $OutputRoot 'empty-export-result.json')
    # AI539_END
    # AI541_BEGIN: actual package relationships; original 86 checks remain unchanged.
    $pathOriginalChecks=$script:tableAuthoringChecks
    $pathSeed=Join-Path $OutputRoot 'path-seed.xlsx';Copy-Item -LiteralPath $comments -Destination $pathSeed
    $pathArchive=[IO.Compression.ZipFile]::Open($pathSeed,[IO.Compression.ZipArchiveMode]::Update)
    try{
        [xml]$pathBook=Read-AuthoringZipEntryText $pathArchive 'xl/workbook.xml'
        [xml]$pathBookRels=Read-AuthoringZipEntryText $pathArchive 'xl/_rels/workbook.xml.rels'
        $pathTable=$notePlan.tables[0];$pathField=$pathTable.comments[0]
        $pathSheet=@($pathBook.SelectNodes('//*[local-name()="sheets"]/*[local-name()="sheet"]')|Where-Object{$_.GetAttribute('name') -ceq $pathTable.tableName})[0]
        $pathRelId=$pathSheet.GetAttribute('id','http://schemas.openxmlformats.org/officeDocument/2006/relationships')
        $pathSheetRel=@($pathBookRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Id') -ceq $pathRelId})[0]
        $pathSheetPart=[Uri]::new([Uri]'http://fixture/xl/workbook.xml',$pathSheetRel.GetAttribute('Target')).AbsolutePath.TrimStart('/')
        $pathRelsPart='xl/worksheets/_rels/'+[IO.Path]::GetFileName($pathSheetPart)+'.rels'
        [xml]$pathRels=Read-AuthoringZipEntryText $pathArchive $pathRelsPart
        $pathParts=@{}
        foreach($pathKind in @('comments','threadedComment')){
            $pathRel=@($pathRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type').EndsWith('/'+$pathKind,[StringComparison]::Ordinal)})[0]
            $pathParts[$pathKind]=[Uri]::new([Uri]('http://fixture/'+$pathSheetPart),$pathRel.GetAttribute('Target')).AbsolutePath.TrimStart('/')
        }
        [xml]$pathThreads=Read-AuthoringZipEntryText $pathArchive $pathParts.threadedComment
        [xml]$pathLegacy=Read-AuthoringZipEntryText $pathArchive $pathParts.comments
        $pathPrimary=@($pathThreads.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('ref') -ceq $pathField.address -and -not $_.HasAttribute('parentId')})[0]
        $pathAuthors=@($pathLegacy.SelectNodes('//*[local-name()="authors"]/*[local-name()="author"]'))
        $pathCompanion=@($pathLegacy.SelectNodes('//*[local-name()="commentList"]/*')|Where-Object{$pathAuthors[[int]$_.GetAttribute('authorId')].InnerText -ceq ('tc='+$pathPrimary.GetAttribute('id'))})[0]
        $pathPrimary.FirstChild.InnerText=[string]$pathField.legacyToolTexts[0]
        $pathCompanion.SelectSingleNode('./*[local-name()="text"]').InnerText=[string]$pathField.legacyToolTexts[0]
        [xml]$pathPersons=Read-AuthoringZipEntryText $pathArchive 'xl/persons/person.xml'
        $pathPerson=$pathPersons.CreateElement('person',$pathPersons.DocumentElement.NamespaceURI);$pathPerson.SetAttribute('id','{BC5BC3F4-51D5-4D98-B932-B0AA54100001}');$pathPerson.SetAttribute('displayName','Path Fixture Human');[void]$pathPersons.DocumentElement.AppendChild($pathPerson)
        $pathReply=$pathThreads.CreateElement('xltc','threadedComment',$pathThreads.DocumentElement.NamespaceURI)
        $pathReply.SetAttribute('id','{BC5BC3F4-51D5-4D98-B932-B0AA54100002}');$pathReply.SetAttribute('personId',$pathPerson.GetAttribute('id'));$pathReply.SetAttribute('parentId',$pathPrimary.GetAttribute('id'));$pathReply.SetAttribute('ref',[string]$pathField.address)
        $pathBody=$pathThreads.CreateElement('xltc','text',$pathThreads.DocumentElement.NamespaceURI);$pathBody.InnerText='USER_PATH_REPLY_MUST_SURVIVE';[void]$pathReply.AppendChild($pathBody);[void]$pathThreads.DocumentElement.AppendChild($pathReply);$pathReplyXml=$pathReply.OuterXml
        Write-FixturePart $pathArchive $pathParts.threadedComment $pathThreads.OuterXml;Write-FixturePart $pathArchive $pathParts.comments $pathLegacy.OuterXml;Write-FixturePart $pathArchive 'xl/persons/person.xml' $pathPersons.OuterXml
    }finally{$pathArchive.Dispose()}
    $pathCases=[Collections.Generic.List[object]]::new()
    foreach($pathMode in @('absolute','relative')){
        $pathInput=Join-Path $OutputRoot ('path-'+$pathMode+'.xlsx');Copy-Item -LiteralPath $pathSeed -Destination $pathInput
        $pathArchive=[IO.Compression.ZipFile]::Open($pathInput,[IO.Compression.ZipArchiveMode]::Update)
        try{
            [xml]$pathBookRels=Read-AuthoringZipEntryText $pathArchive 'xl/_rels/workbook.xml.rels'
            foreach($pathRel in $pathBookRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type').EndsWith('/worksheet',[StringComparison]::Ordinal)}){
                $pathResolved=[Uri]::new([Uri]'http://fixture/xl/workbook.xml',$pathRel.GetAttribute('Target')).AbsolutePath
                $pathRel.SetAttribute('Target',$(if($pathMode -eq 'absolute'){$pathResolved}else{$pathResolved.Substring('/xl/'.Length).Replace('/','\')}))
            }
            Write-FixturePart $pathArchive 'xl/_rels/workbook.xml.rels' $pathBookRels.OuterXml
            foreach($pathSheetRelsPart in @($pathArchive.Entries|Where-Object FullName -Like 'xl/worksheets/_rels/*.rels'|ForEach-Object FullName)){
                [xml]$pathSheetRels=Read-AuthoringZipEntryText $pathArchive $pathSheetRelsPart
                $pathBase=[Uri]('http://fixture/xl/worksheets/'+[IO.Path]::GetFileNameWithoutExtension($pathSheetRelsPart))
                foreach($pathRel in $pathSheetRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type') -match '/(comments|threadedComment)$'}){
                    $pathResolved=[Uri]::new($pathBase,$pathRel.GetAttribute('Target'))
                    $pathRel.SetAttribute('Target',$(if($pathMode -eq 'absolute'){$pathResolved.AbsolutePath}else{$pathBase.MakeRelativeUri($pathResolved).ToString().Replace('../','.././').Replace('/','\')}))
                }
                Write-FixturePart $pathArchive $pathSheetRelsPart $pathSheetRels.OuterXml
            }
        }finally{$pathArchive.Dispose()}
        $pathBefore=Get-AuthoringWorkbookContentSnapshot $pathInput;$pathCandidate=Join-Path $OutputRoot ('path-'+$pathMode+'-updated.xlsx')
        $pathReceipt=& $commentEntry -InputWorkbook $pathInput -CandidateWorkbook $pathCandidate -ExpectedInputSha256 $pathBefore.sourceSha256 -DataTablesRoot $dataRoot
        $pathReceipt|ConvertTo-Json -Depth 8|Set-Content (Join-Path $OutputRoot ('path-'+$pathMode+'-receipt.json'))
        Assert-TableAuthoring ($pathReceipt.updatedThreads -eq 1 -and $pathReceipt.removedToolThreads -eq 0 -and $pathReceipt.commentProtectionAudit.protectedNodes -gt 0 -and $pathReceipt.commentProtectionAudit.declaredBefore -eq 2 -and $pathReceipt.commentProtectionAudit.declaredAfter -eq 2) ($pathMode+' actual update changes one exact known template and its companion; global audit protects all other remarks')
        $pathAfter=Get-AuthoringWorkbookContentSnapshot $pathCandidate;$pathArchive=[IO.Compression.ZipFile]::OpenRead($pathCandidate)
        try{[xml]$pathThreads=Read-AuthoringZipEntryText $pathArchive $pathParts.threadedComment;$pathSurvivingReply=@($pathThreads.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('id') -ceq $pathReply.GetAttribute('id')})}finally{$pathArchive.Dispose()}
        Assert-TableAuthoring ($pathReceipt.contentDifferenceCount -eq 0 -and $pathReceipt.structureDifferenceCount -eq 0 -and (Compare-AuthoringWorkbookContent $pathBefore $pathAfter).isEqual -and (Compare-AuthoringWorkbookStructure $pathBefore $pathAfter).isEqual -and (Get-AuthoringFileSha256 $pathInput) -eq $pathBefore.sourceSha256 -and $pathSurvivingReply.Count -eq 1 -and $pathSurvivingReply[0].OuterXml -ceq $pathReplyXml) ($pathMode+' package leaves source bytes, typed content, blank structure and exact human reply unchanged')
        [void]$pathCases.Add([pscustomobject]@{mode=$pathMode;sourceSha256=$pathBefore.sourceSha256;receipt=$pathReceipt;humanReplyPreserved=$true})
    }
    foreach($pathEscape in @('/../xl/comments.xml','../../../xl/comments.xml')){
        $pathIndex=if($pathEscape.StartsWith('/')){'absolute'}else{'relative'}
        $pathInvalid=Join-Path $OutputRoot ('path-escape-'+$pathIndex+'.xlsx');Copy-Item -LiteralPath $pathSeed -Destination $pathInvalid
        $pathArchive=[IO.Compression.ZipFile]::Open($pathInvalid,[IO.Compression.ZipArchiveMode]::Update)
        try{[xml]$pathRels=Read-AuthoringZipEntryText $pathArchive $pathRelsPart;$pathRel=@($pathRels.DocumentElement.ChildNodes|Where-Object{$_.GetAttribute('Type').EndsWith('/comments',[StringComparison]::Ordinal)})[0];$pathRel.SetAttribute('Target',$pathEscape);Write-FixturePart $pathArchive $pathRelsPart $pathRels.OuterXml}finally{$pathArchive.Dispose()}
        $pathInvalidSha=Get-AuthoringFileSha256 $pathInvalid;$pathDeniedCandidate=Join-Path $OutputRoot ('path-escape-'+$pathIndex+'-must-not-exist.xlsx');$pathDenied=$false
        try{& $commentEntry -InputWorkbook $pathInvalid -CandidateWorkbook $pathDeniedCandidate -ExpectedInputSha256 $pathInvalidSha -DataTablesRoot $dataRoot|Out-Null}catch{if($_.Exception.Message -notlike '*[[]HEADER_COMMENT_PATH_ESCAPE[]]*'){throw};$pathDenied=$true}
        Assert-TableAuthoring ($pathDenied -and -not (Test-Path -LiteralPath $pathDeniedCandidate) -and (Get-AuthoringFileSha256 $pathInvalid) -eq $pathInvalidSha) ($pathIndex+' root escape is rejected by actual tool without source write or leftover candidate')
    }
    [pscustomobject]@{passed=$true;originalChecks=$pathOriginalChecks;additionalChecks=($script:tableAuthoringChecks-$pathOriginalChecks);packages=$pathCases.ToArray();relativeParentDotAndBackslashCovered=$true;rootEscapesRejected=2}|ConvertTo-Json -Depth 12|Set-Content (Join-Path $OutputRoot 'package-path-result.json')
    # AI541_END
    $result=[pscustomobject]@{passed=$true;checks=$script:tableAuthoringChecks;oldSheets=14;unknownSheets=1;addedSheets=8;newHeaders=105;allHeaderNotes=224;sourceValuesTypesFormulasUnchanged=$true;sampleRoundTripTables=8;productionWorkbookWritten=$false}
    $result|ConvertTo-Json|Set-Content (Join-Path $OutputRoot 'result.json');$result|ConvertTo-Json -Compress
}
