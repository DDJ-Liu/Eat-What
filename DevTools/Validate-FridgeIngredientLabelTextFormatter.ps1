[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$formatterPath = Join-Path $projectRoot 'Assets/Scripts/Cooking/Prepare/RecipeBook/Fridge/FridgeIngredientLabelTextFormatter.cs'
$fitterPath = Join-Path $projectRoot 'Assets/Scripts/Cooking/Prepare/RecipeBook/Fridge/FridgeIngredientLabelTextFitter.cs'

if (-not (Test-Path -LiteralPath $formatterPath) -or -not (Test-Path -LiteralPath $fitterPath)) {
    throw 'Fridge ingredient label formatter or fitter source file is missing.'
}

Add-Type -Path $formatterPath

function Assert-Equal {
    param(
        [string]$Name,
        [object]$Expected,
        [object]$Actual
    )

    if ($Expected -ne $Actual) {
        throw "${Name} failed. Expected '$Expected', got '$Actual'."
    }
}

function Assert-True {
    param(
        [string]$Name,
        [bool]$Condition
    )

    if (-not $Condition) {
        throw "${Name} failed."
    }
}

$format = [FridgeIngredientLabelTextFormatter]

function New-UnicodeText {
    param([int[]]$CodePoints)

    return -join ($CodePoints | ForEach-Object { [char]$_ })
}

$fan = New-UnicodeText @(0x756A)
$tomato = New-UnicodeText @(0x756A, 0x8304)
$stir = New-UnicodeText @(0x7092)
$rice = New-UnicodeText @(0x996D)
$fiveChinese = New-UnicodeText @(0x756A, 0x8304, 0x7092, 0x9E21, 0x86CB)
$sixChinese = New-UnicodeText @(0x7EA2, 0x70E7, 0x8089, 0x76D6, 0x6D47, 0x996D)
$sevenChinese = New-UnicodeText @(0x8D85, 0x957F, 0x5496, 0x55B1, 0x9E21, 0x8089, 0x996D)
$firstHalfOfSix = New-UnicodeText @(0x7EA2, 0x70E7, 0x8089)
$secondHalfOfSix = New-UnicodeText @(0x76D6, 0x6D47, 0x996D)
$firstHalfOfSeven = New-UnicodeText @(0x8D85, 0x957F, 0x5496, 0x55B1)
$secondHalfOfSeven = New-UnicodeText @(0x9E21, 0x8089, 0x996D)

Assert-Equal 'empty text' '' $format::Format('', 5)
Assert-Equal 'one Chinese character' $fan $format::Format($fan, 5)
Assert-Equal 'five Chinese characters remain one line' $fiveChinese $format::Format($fiveChinese, 5)
Assert-Equal 'six Chinese characters balance into two lines' "$firstHalfOfSix`n$secondHalfOfSix" $format::Format($sixChinese, 5)
Assert-Equal 'seven Chinese characters balance into two lines' "$firstHalfOfSeven`n$secondHalfOfSeven" $format::Format($sevenChinese, 5)
Assert-Equal 'mixed text uses visible characters' "A1$fan`n$($tomato.Substring(1, 1))$stir$rice" $format::Format("A1$tomato$stir$rice", 5)
Assert-Equal 'rich-text tags do not count as visible characters' "<b>$fiveChinese</b>" $format::Format("<b>$fiveChinese</b>", 5)
Assert-Equal 'rich-text long text remains balanced' "<color=red>$firstHalfOfSeven`n$secondHalfOfSeven</color>" $format::Format("<color=red>$sevenChinese</color>", 5)
Assert-Equal 'first explicit newline is preserved and later newlines are removed' "$fan`n$tomato$($fiveChinese.Substring(2, 2))" $format::Format("$fan`n$tomato`n$($fiveChinese.Substring(2, 2))", 5)

$formattedOnce = $format::Format($sixChinese, 5)
Assert-Equal 'repeated formatting does not add a newline' $formattedOnce $format::Format($formattedOnce, 5)
Assert-Equal 'visible count ignores rich-text tags and newlines' 6 $format::CountVisibleCharacters("<b>$($firstHalfOfSix.Substring(0, 2))</b>`n$($firstHalfOfSix.Substring(2, 1))$secondHalfOfSix")
Assert-True 'all automatic long labels use at most two lines' (($format::Format($sevenChinese, 5).Split("`n").Count) -le 2)

$fitterSource = Get-Content -LiteralPath $fitterPath -Raw
Assert-True 'serialized font bounds exist' ($fitterSource -match '\[SerializeField\]\s+private float minFontSize' -and $fitterSource -match '\[SerializeField\]\s+private float maxFontSize')
Assert-True 'font bounds are normalized' ($fitterSource -match 'maxFontSize = Mathf\.Max\(minFontSize, maxFontSize\)')
Assert-True 'TMP text changes are event driven' ($fitterSource -match 'TMPro_EventManager\.TEXT_CHANGED_EVENT\.Add\(OnTextChanged\)')
Assert-True 'safe area uses preferred values' ($fitterSource -match 'GetPreferredValues\(formattedText\)')
Assert-True 'two-line contract disables automatic wrapping' ($fitterSource -match 'targetText\.enableWordWrapping = false')
Assert-True 'minimum-size overflow uses ellipsis' ($fitterSource -match 'TextOverflowModes\.Ellipsis')
Assert-True 'dimension changes do not require Update polling' ($fitterSource -match 'OnRectTransformDimensionsChange' -and $fitterSource -notmatch 'void Update\s*\(')

Write-Output 'PASS: Fridge ingredient label formatter behavior and fitter static contract validated.'
