param([Parameter(Mandatory)][string]$InputPath,[Parameter(Mandatory)][string]$ConsumerPath)
$ErrorActionPreference='Stop'
$token=$null;$errorToken=$null;$ast=[Management.Automation.Language.Parser]::ParseFile($ConsumerPath,[ref]$token,[ref]$errorToken)
if($errorToken.Count){throw 'Actual consumer parse failed'}
foreach($name in @('LLStreetTimestamp','LLStreetSameInstant','LLStreetIdentityField','LLAssert','LLHash','LLCanonical','LLJson','LLInteger','LLTrue','LLFalse','LLNumbers','LLValidateCapturePending')){
 $selected=@($ast.FindAll({param($n)$n-is[Management.Automation.Language.FunctionDefinitionAst]-and$n.Name-ceq$name},$true))
 if($selected.Count-ne1){throw 'Unique actual production function required'};Invoke-Expression $selected[0].Extent.Text
}
. "$PSScriptRoot/StreetProbeManagedState.ps1"
$inputValue=LLJson ([IO.File]::ReadAllBytes($InputPath));$accepted=$false;$message=$null
try{
 if($inputValue.remaining){Street-AssertAdmission $inputValue.notice $inputValue.schedule $inputValue.remaining ([DateTimeOffset]::Parse($inputValue.now))}
 if($inputValue.role){Street-AssertRolePayload $inputValue.role $inputValue.report $inputValue.identity $inputValue.request $inputValue.notice $inputValue.noticeSha $inputValue.receipt $inputValue.captureReceipt}
 $accepted=$true
}catch{$message=$_.Exception.Message}
[pscustomobject]@{fixtureOnly=$true;accepted=$accepted;error=$message;nativeEvidenceAccepted=$false}|ConvertTo-Json -Depth 8 -Compress
