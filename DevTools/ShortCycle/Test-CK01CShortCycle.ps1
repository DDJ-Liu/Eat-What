[CmdletBinding()]
param([string]$UnityEditorPath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$builderPath = Join-Path $repoRoot 'Assets\Editor\ShortCycle\ShortCycleSceneBuilder.cs'
$inputAdapterPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleMouseActionBinding.cs'
$layerStackPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleInteractionLayerStack.cs'
$presenterPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleScenePresenter.cs'
$stateMachinePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleStateMachine.cs'
$trayStateMachinePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleTrayStateMachine.cs'
$probePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleAutomatedProbe.cs'
$commandBridgePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleCommandBridge.cs'
$actionRegistryPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleActionRegistry.cs'
$actionRouterPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleActionRouter.cs'
$cameraPanPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleCameraPanController.cs'
$overflowGatePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleOverflowVisibilityGate.cs'
$manualDriverPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleManualVerificationDriver.cs'
$localizedTextPath = Join-Path $repoRoot 'Assets\Scripts\Tools\Localization\LocalizedText.cs'
$sessionManagerPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleSessionManager.cs'
$directorPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Runtime\ShortCyclePresentationDirector.cs'
$travelerPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Presentation\ShortCycleTravelingElement.cs'
$trayControllerPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleTrayStateController.cs'
$fridgeBinderPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Content\ShortCycleFridgeBinder.cs'
$fridgeScrollAdapterPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Interaction\ShortCycleScrollAdapter.cs'
$fridgeScrollTraceProbePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Debug\ShortCycleFridgeScrollTraceProbe.cs'
$mouseScrollableObjectPath = Join-Path $repoRoot 'Assets\Scripts\MouseInteractive\Scroll\MouseScrollableObject.cs'
$fridgeScrollLabDriverPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Debug\FridgeScrollLabDriver.cs'
$fridgeScrollLabBuilderPath = Join-Path $repoRoot 'Assets\Editor\ShortCycle\FridgeScrollLabBuilder.cs'
$fridgeScrollLabAuditPath = Join-Path $repoRoot 'Assets\Editor\ShortCycle\FridgeScrollLabAudit.cs'
$mouseManagerPath = Join-Path $repoRoot 'Assets\Scripts\MouseInteractive\MouseManager.cs'
$clueBinderPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Content\ShortCycleClueBoardBinder.cs'
$stateGatePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Interaction\ShortCycleStateGatedInteraction.cs'
$gridPieceBasePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleGridPieceBase.cs'
$v22LayoutPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleV22LayoutContracts.cs'
$trayAnchorLayoutPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleTrayAnchorLayout.cs'
$subCardPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleSubCard.cs'
$migrationV2Path = Join-Path $repoRoot 'DevTools\ShortCycle\Migrations\ShortCycleSceneMigrationV2.cs'
$gridPiecePaths = @(
    (Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\FridgeSlot.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\TraySlot.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\CatalogCard.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\IngredientCard.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\SlotBlock.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Sticker.cs')
)
$runtimeManagersPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\ShortCycleRuntimeManagers.cs'
$catalogPath = Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01GeneratedDataCatalog.cs'
$buttonPath = Join-Path $repoRoot 'Assets\Scripts\MouseInteractive\Button\Button_MouseInteract.cs'
$textFitterPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\Prepare\RecipeBook\Fridge\FridgeIngredientLabelTextFitter.cs'
$fridgeCatBlinkSchedulerPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\Prepare\RecipeBook\Fridge\FridgeCatBlinkScheduler.cs'
$fridgeCatRigPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Presentation\FridgeCatRig.cs'
$fridgeCatTierPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Presentation\FridgeCatTier.cs'
$fridgeCatEyePath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Presentation\FridgeCatEye.cs'
$presentationSetPath = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle\Presentation\ShortCyclePresentationSet.cs'
$scrollAreaPath = Join-Path $repoRoot 'Assets\Scripts\MouseInteractive\ScrollArea\ScrollArea_Controller.cs'
$unityStubsPath = Join-Path $PSScriptRoot 'UnityStubs.cs'
$outlinePath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutline2D.cs'
$outlineGroupPath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutlineGroup2D.cs'
$outlineDefaultsPath = Join-Path $repoRoot 'Assets\Scripts\Rendering\SpriteOutlineDefaults.cs'
$outlineEditorPath = Join-Path $repoRoot 'Assets\Editor\Rendering\SpriteOutline2DEditor.cs'
$outlineGroupEditorPath = Join-Path $repoRoot 'Assets\Editor\Rendering\SpriteOutlineGroup2DEditor.cs'
$cameraFrameRuntimePath = Join-Path $repoRoot 'Assets\Scripts\Tools\Debug\CameraFrameGizmo.cs'
$cameraFrameDrawerPath = Join-Path $repoRoot 'Assets\Editor\ShortCycle\CameraFrameGizmo.cs'
$cameraFrameMenuPath = Join-Path $repoRoot 'Assets\Editor\ShortCycle\CameraFrameGizmoMenu.cs'
$projectSettingsPath = Join-Path $repoRoot 'ProjectSettings\ProjectSettings.asset'
$scenePath = Join-Path $repoRoot 'Assets\Scenes\Cooking\ShortCycle_P0P1.unity'
$globalInputManagerPath = Join-Path $repoRoot 'Assets\Scripts\Controller\InputManager.cs'
$inputActionsPath = Join-Path $repoRoot 'Assets\Scripts\Controller\PlayerControl.inputactions'
$inputActionsMetaPath = $inputActionsPath + '.meta'

function Assert-SourceContains {
    param([string]$Source, [string]$Pattern, [string]$Message)
    if ($Source -notmatch $Pattern) { throw $Message }
}

function Assert-SourceNotContains {
    param([string]$Source, [string]$Pattern, [string]$Message)
    if ($Source -match $Pattern) { throw $Message }
}

$builderSource = Get-Content -Raw -LiteralPath $builderPath
$inputAdapterSource = Get-Content -Raw -LiteralPath $inputAdapterPath
$layerStackSource = Get-Content -Raw -LiteralPath $layerStackPath
$presenterSource = Get-Content -Raw -LiteralPath $presenterPath
$stateMachineSource = Get-Content -Raw -LiteralPath $stateMachinePath
$trayStateMachineSource = Get-Content -Raw -LiteralPath $trayStateMachinePath
$probeSource = Get-Content -Raw -LiteralPath $probePath
$commandBridgeSource = Get-Content -Raw -LiteralPath $commandBridgePath
$actionRegistrySource = Get-Content -Raw -LiteralPath $actionRegistryPath
$actionRouterSource = Get-Content -Raw -LiteralPath $actionRouterPath
$cameraPanSource = Get-Content -Raw -LiteralPath $cameraPanPath
$overflowGateSource = Get-Content -Raw -LiteralPath $overflowGatePath
$manualDriverSource = Get-Content -Raw -LiteralPath $manualDriverPath
$localizedTextSource = Get-Content -Raw -LiteralPath $localizedTextPath
$sessionManagerSource = Get-Content -Raw -LiteralPath $sessionManagerPath
$directorSource = Get-Content -Raw -LiteralPath $directorPath
$travelerSource = Get-Content -Raw -LiteralPath $travelerPath
$trayControllerSource = Get-Content -Raw -LiteralPath $trayControllerPath
$fridgeBinderSource = Get-Content -Raw -LiteralPath $fridgeBinderPath
$fridgeScrollAdapterSource = Get-Content -Raw -LiteralPath $fridgeScrollAdapterPath
$fridgeScrollTraceProbeSource = Get-Content -Raw -LiteralPath $fridgeScrollTraceProbePath
$mouseScrollableObjectSource = Get-Content -Raw -LiteralPath $mouseScrollableObjectPath
$fridgeScrollLabDriverSource = Get-Content -Raw -LiteralPath $fridgeScrollLabDriverPath
$fridgeScrollLabBuilderSource = Get-Content -Raw -LiteralPath $fridgeScrollLabBuilderPath
$fridgeScrollLabAuditSource = Get-Content -Raw -LiteralPath $fridgeScrollLabAuditPath
$mouseManagerSource = Get-Content -Raw -LiteralPath $mouseManagerPath
$clueBinderSource = Get-Content -Raw -LiteralPath $clueBinderPath
$stateGateSource = Get-Content -Raw -LiteralPath $stateGatePath
$gridPieceSource = ((@($gridPieceBasePath) + $gridPiecePaths) | ForEach-Object { Get-Content -Raw -LiteralPath $_ }) -join "`n"
$v22LayoutSource = (Get-Content -Raw -LiteralPath $v22LayoutPath) + "`n" + (Get-Content -Raw -LiteralPath $trayAnchorLayoutPath)
$subCardSource = Get-Content -Raw -LiteralPath $subCardPath
$migrationV2Source = Get-Content -Raw -LiteralPath $migrationV2Path
$runtimeManagersSource = Get-Content -Raw -LiteralPath $runtimeManagersPath
$catalogSource = Get-Content -Raw -LiteralPath $catalogPath
$buttonSource = Get-Content -Raw -LiteralPath $buttonPath
$textFitterSource = Get-Content -Raw -LiteralPath $textFitterPath
$fridgeCatBlinkSchedulerSource = Get-Content -Raw -LiteralPath $fridgeCatBlinkSchedulerPath
$fridgeCatRigSource = Get-Content -Raw -LiteralPath $fridgeCatRigPath
$fridgeCatTierSource = Get-Content -Raw -LiteralPath $fridgeCatTierPath
$fridgeCatEyeSource = Get-Content -Raw -LiteralPath $fridgeCatEyePath
$presentationSetSource = Get-Content -Raw -LiteralPath $presentationSetPath
$scrollAreaSource = Get-Content -Raw -LiteralPath $scrollAreaPath
$unityStubsSource = Get-Content -Raw -LiteralPath $unityStubsPath
$outlineSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $outlinePath
$outlineGroupSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $outlineGroupPath
$outlineDefaultsSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $outlineDefaultsPath
$outlineEditorSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $outlineEditorPath
$outlineGroupEditorSource = Get-Content -Raw -Encoding UTF8 -LiteralPath $outlineGroupEditorPath
$cameraFrameRuntimeSource = Get-Content -Raw -LiteralPath $cameraFrameRuntimePath
$cameraFrameDrawerSource = Get-Content -Raw -LiteralPath $cameraFrameDrawerPath
$cameraFrameMenuSource = Get-Content -Raw -LiteralPath $cameraFrameMenuPath
$projectSettingsSource = Get-Content -Raw -LiteralPath $projectSettingsPath
$sceneSource = Get-Content -Raw -LiteralPath $scenePath
$globalInputManagerSource = Get-Content -Raw -LiteralPath $globalInputManagerPath
$inputActionsSource = Get-Content -Raw -LiteralPath $inputActionsPath
$inputActionsMetaSource = Get-Content -Raw -LiteralPath $inputActionsMetaPath
$shortCycleRoot = Join-Path $repoRoot 'Assets\Scripts\Cooking\ShortCycle'
$shortCycleCompilePaths = @(Get-ChildItem -LiteralPath $shortCycleRoot -Filter '*.cs' -File -Recurse | Select-Object -ExpandProperty FullName)
$transitionCompilePaths = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets\Scripts\Tools\Transition') -Filter '*.cs' -File | Select-Object -ExpandProperty FullName)
$shortCycleProductionSource = ($shortCycleCompilePaths |
    ForEach-Object { Get-Content -Raw -LiteralPath $_ }) -join "`n"
$shortCycleCoreSource = @(Get-ChildItem -LiteralPath (Join-Path $shortCycleRoot 'Core') -Filter '*.cs' -File -Recurse |
    ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName }) -join "`n"

Assert-SourceContains $fridgeScrollTraceProbeSource '\[CK01-F-SCROLL-TRACE\]' `
    'The real-wheel diagnostic must use the single CK01-F trace prefix.'
Assert-SourceContains $fridgeScrollTraceProbeSource '\[SerializeField\]\s+private\s+bool\s+traceEnabled\s*=\s*false' `
    'The fridge scroll trace must remain manually enabled and default off.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)Mouse\.current.*?Application\.isFocused|Application\.isFocused.*?Mouse\.current' `
    'The trace must observe both the real Input System mouse and application focus.'
Assert-SourceContains $fridgeScrollTraceProbeSource 'Physics2D\.OverlapPointAll' `
    'The trace must read-only recompute the framework hit candidates.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)firstMaskHits.*?currentScrollable.*?candidates' `
    'The trace snapshot must separate first-mask candidates from the authoritative framework target.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)GetPersistentEventCount.*?adapter.*?dispatch' `
    'The trace must expose the event-to-adapter boundary and dispatch result.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)Captures.*?AI-000075' `
    'The trace must export bounded evidence under the task capture directory.'
Assert-SourceNotContains $fridgeScrollTraceProbeSource '(?i)GameObject\.Find|FindObjectOfType|FindFirstObjectByType|FindAnyObjectByType' `
    'The trace must use explicit Inspector references and never search the scene.'
Assert-SourceNotContains $fridgeScrollTraceProbeSource 'scrollStepEvent\s*\?*\.Invoke|DispatchAction\s*\(' `
    'The passive trace must never inject scroll events or add a parallel business action route.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)ShortCycleScrollTraceMode.*?captureSessionId.*?lastNonzeroRawFrame' `
    'The lab trace must distinguish real/simulated sessions and preserve the last nonzero raw sample.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)managerFrameDelta.*?targetFrameDelta.*?receiveFrameDelta.*?dispatchFrameDelta' `
    'The lab trace must expose current-frame deltas across the manager, target, adapter, and dispatch chain.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)contentState.*?targetState.*?transitionState.*?stateKey' `
    'The bounded trace must retain content/target/transition changes, including transition completion.'
Assert-SourceContains $fridgeScrollLabDriverSource 'scrollAdapter\.Dispatch\s*\(' `
    'The explicitly labelled simulated path must reuse ShortCycleScrollAdapter.Dispatch.'
Assert-SourceNotContains $fridgeScrollLabDriverSource '(?i)GameObject\.Find|Transform\.Find|FindObjectOfType|FindFirstObjectByType|FindAnyObjectByType' `
    'The lab driver must use explicit serialized references only.'
Assert-SourceNotContains $fridgeScrollLabDriverSource 'scrollStepEvent\s*\?*\.Invoke|MouseManager\.RaiseScrollStep|InputManager\.OnKey|Keyboard\.current' `
    'The lab driver must not synthesize framework events or add global input.'
if ([regex]::Matches($fridgeScrollLabDriverSource, 'scrollAdapter\.Dispatch\s*\(').Count -ne 1) {
    throw 'The lab driver must expose exactly one explicit Adapter.Dispatch call site.'
}
Assert-SourceContains $fridgeScrollLabDriverSource '(?s)private\s+IEnumerator\s+Start\s*\([^)]*\)\s*\{.*?InitializeLabSession\s*\(\s*\).*?BeginRealInputSession\s*\(\s*\).*?\}' `
    'Start may initialize Phase1 and real-input tracing, but never the simulated adapter route.'
Assert-SourceContains $fridgeScrollLabDriverSource '(?s)TryShowRecipeCatalog\s*\(\s*\).*?TrySelectRecipe\s*\(\s*recipeId\s*,\s*false\s*\).*?TryStartCooking\s*\(\s*\)' `
    'The lab must enter Phase1 through the public Cover-to-Catalog selection sequence.'
Assert-SourceContains $fridgeScrollLabBuilderSource 'Assets/Scenes/ToolTests/FridgeScrollLab\.unity' `
    'The builder must target only the isolated FridgeScrollLab scene.'
Assert-SourceContains $fridgeScrollLabBuilderSource 'Assets/Prefabs/Cooking/ShortCycle/FridgeCat/FridgeCat_Rig\.prefab' `
    'The builder must instantiate the approved FridgeCat rig prefab source.'
Assert-SourceContains $fridgeScrollLabBuilderSource '(?s)Renderer.*bounds.*?BoxCollider2D|MeasureBounds.*?Collider' `
    'The builder must derive the ScrollRegion collider from measured renderer bounds.'
Assert-SourceContains $fridgeScrollLabBuilderSource '(?s)FridgeSlot\[30\].*?SlotBlock\[3\]' `
    'The builder must reserve 30 fridge slots and three clue blocks for the tomato/egg fixture.'
Assert-SourceContains $fridgeScrollLabBuilderSource 'TextMeshPro' `
    'The lab must use world-space TMP3D diagnostics.'
Assert-SourceNotContains $fridgeScrollLabBuilderSource '(?i)GameObject\.Find|Transform\.Find|using\s+UnityEngine\.UI\s*;|AddComponent\s*<\s*Canvas\s*>' `
    'The lab builder must avoid scene search and Canvas/UGUI interaction.'
Assert-SourceNotContains $fridgeScrollLabBuilderSource 'ShortCycle_P0P1|EditorBuildSettings\.scenes\s*=' `
    'The lab builder must not copy the whole production scene or alter Build Settings.'
Assert-SourceContains $fridgeScrollLabAuditSource '(?s)Renderer.*bounds.*?collider\.bounds.*?OverlapPointAll' `
    'The independent audit must report actual renderer/collider geometry and point hit paths.'
Assert-SourceContains $fridgeScrollLabAuditSource 'GetMonoBehavioursWithMissingScriptCount' `
    'The independent audit must count missing scripts without mutating the scene.'
Assert-SourceContains $fridgeScrollLabAuditSource 'GameView' `
    'The independent audit must report Editor GameView focus.'
Assert-SourceNotContains $fridgeScrollLabAuditSource '(?i)SaveScene|SetDirty|ApplyModifiedProperties|AddComponent|Instantiate|CreateAsset|DeleteAsset' `
    'The independent audit must remain read-only.'
Assert-SourceContains $mouseManagerSource '(?s)public\s+float\s+ScrollThreshold.*?public\s+float\s+AccumulatedScroll.*?public\s+float\s+LastScrollTime.*?public\s+float\s+ScrollCooldown' `
    'MouseManager must expose its existing wheel accumulator as read-only diagnostics.'
Assert-SourceContains $fridgeScrollAdapterSource '(?s)ReceivedStepCount.*?LastAttemptResult.*?IsSubscribed' `
    'The existing adapter must expose passive receive and subscription diagnostics.'
Assert-SourceContains $fridgeScrollAdapterSource '(?s)invertPhysicalScrollDirection\s*=\s*false.*?MapPhysicalStep.*?LastMappedDelta' `
    'Physical direction mapping must be explicit, default preserving, and observable.'
Assert-SourceContains $fridgeScrollAdapterSource '(?s)public\s+ShortCycleActionResult\s+Dispatch.*?delta\s*=\s*delta\s*<\s*0f\s*\?\s*-1f\s*:\s*1f' `
    'The public adapter business contract must remain +1 next and -1 previous.'
Assert-SourceContains $mouseManagerSource '(?s)LastScrollableCandidate.*?LastScrollableBlocker.*?LastScrollResolutionReason.*?ScrollDispatchSequence' `
    'MouseManager must expose the authoritative target/blocker and dispatch correlation diagnostics.'
Assert-SourceContains $mouseManagerSource '(?s)FindBlockingPressable\s*\(.*?MouseScrollableObject\s+scrollable.*?scrollable\.CanScrollThroughPressable\s*\(\s*pressable\s*\).*?continue\s*;.*?return\s+pressable\s*;' `
    'Pressable wheel pass-through must be delegated to the explicit Scrollable policy.'
Assert-SourceContains $mouseScrollableObjectSource '(?s)allowScrollThroughPressables\s*=\s*false.*?pressablePassThroughScope\s*=\s*null' `
    'Pressable wheel pass-through must preserve the old blocking behavior until both explicit fields are configured.'
Assert-SourceContains $mouseScrollableObjectSource '(?s)CanScrollThroughPressable.*?IsInScope\s*\(\s*transform\s*,\s*pressablePassThroughScope\s*\).*?IsInScope\s*\(\s*pressable\.transform\s*,\s*pressablePassThroughScope\s*\)' `
    'Pressable pass-through must require both the scroll target and blocker to share the configured scope.'
Assert-SourceContains $mouseManagerSource '(?s)mouseInteractiveLayers\s*==\s*null\s*\|\|\s*mouseInteractiveLayers\.Count\s*==\s*0.*?return\s*;.*?mouseInteractiveLayers\.Peek\s*\(' `
    'MouseManager must guard the empty interaction-layer stack before Peek.'
Assert-SourceContains $mouseManagerSource 'HasUnsafeEmptyLayerUpdatePath(?s).*?get\s*\{\s*return\s+false\s*;' `
    'MouseManager diagnostics must report that the pre-Count Peek path has been removed.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)resolveFrame.*?candidate.*?currentScrollable.*?blocker.*?reason' `
    'The trace must correlate the authoritative target and blocker decision in one snapshot.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)dispatchSequence.*?dispatchFrame.*?managerRaw.*?managerStep.*?managerWorld' `
    'The trace must correlate raw input, pointer, emitted step, frame, and sequence.'
Assert-SourceContains $fridgeScrollTraceProbeSource '(?s)fridgeFocused.*?inputLocked' `
    'The trace must distinguish the expected focus/input-lock window from a persistent failure.'
Assert-SourceContains $scrollAreaSource '(?s)NormalizedContentPosition.*?LastTargetPosition.*?IsProgrammaticTransitionActive' `
    'ScrollArea must expose passive current/target/transition diagnostics.'

if ([regex]::Matches($travelerSource, '\[SerializeField').Count -ne 4) {
    throw 'SnapTo must not add, remove, or rename the Traveler serialized field contract.'
}
Assert-SourceContains $travelerSource '(?s)public\s+bool\s+SnapTo\s*\(\s*ShortCycleSpaceId\s+space\s*\).*?ResolveAnchor\s*\(\s*space\s*\).*?Cancel\s*\(\s*\).*?movingTransform\.position\s*=\s*anchor\.position' `
    'Traveler SnapTo must reuse its existing anchor, cancel active movement, and set the existing moving Transform.'
Assert-SourceContains $directorSource '(?s)!hadDeparture.*?traveler\.SnapTo\s*\(\s*space\s*\)' `
    'Initial entry without a departure must snap each Traveler.'
Assert-SourceContains $directorSource '(?s)space\s*==\s*ShortCycleSpaceId\.Phase0.*?view\s*==\s*ShortCyclePhase0View\.ClueBoardShell.*?traveler\.SnapTo\s*\(\s*space\s*\)' `
    'Opening the Phase0 clue board must correct Traveler placement through SnapTo.'

Assert-SourceNotContains $builderSource 'UnityEngine\.UI\.Button|typeof\s*\(\s*Button\s*\)|\.onClick\b' `
    'ShortCycle builder must not carry an interactive UnityEngine.UI.Button path.'
Assert-SourceContains $builderSource 'typeof\s*\(\s*Button_MouseInteract\s*\)' `
    'ShortCycle builder must create Button_MouseInteract.'
Assert-SourceContains $builderSource 'typeof\s*\(\s*ColorImageButton_Visual\s*\)' `
    'ShortCycle builder must consume an existing Button_MouseInteract visual component.'
Assert-SourceContains $builderSource 'AddComponent<MouseInteractionLayer>\s*\(\s*\)' `
    'ShortCycle builder must create the project MouseInteractionLayer.'
Assert-SourceContains $builderSource '\.selectEvent\b' `
    'ShortCycle button actions must bind through Button_MouseInteract.selectEvent.'
Assert-SourceContains $layerStackSource '\bMouseInteractionLayer\b' `
    'ShortCycle modal ordering must be typed to the project MouseInteractionLayer.'
Assert-SourceContains $layerStackSource '\.OnPushLayer\s*\(\s*\)' `
    'ShortCycle modal ordering must push through the project layer stack.'
Assert-SourceContains $layerStackSource '\.OnRemoveLayer\s*\(\s*\)' `
    'ShortCycle modal ordering must remove through the project layer stack.'
Assert-SourceNotContains ($layerStackSource + $presenterSource) '\bShortCycleMouseInteractionLayer\b' `
    'Active ShortCycle layer wiring must not depend on the C-T2 compatibility alias.'
Assert-SourceContains $inputAdapterSource 'DispatchFromDropEvent\s*\(\s*MouseDraggableObject' `
    'MouseDraggableObject/DropZone event payload example must remain reserved.'
Assert-SourceContains $inputAdapterSource 'DispatchFromDropHitEvent\s*\(\s*DropZone' `
    'Drop hit event payload example must remain reserved.'
Assert-SourceContains $inputAdapterSource 'DispatchFromScrollEvent\s*\(\s*float' `
    'MouseScrollableObject event example must remain reserved.'
Assert-SourceContains $inputAdapterSource 'DispatchFromScrollPositionEvent\s*\(\s*Vector2' `
    'ScrollArea_Controller event example must remain reserved.'
Assert-SourceContains ($runtimeManagersSource + $presenterSource) '\[SerializeField\]\s+private\s+CK01GeneratedDataCatalog\s+dataCatalog' `
    'ShortCycle scene-facing data consumers must serialize only the generated catalog entry.'
$rowAssetTypes = 'CK01RecipeData|CK01RecipeSlotData|CK01RecipeStepData|CK01ItemData|CK01InitialInventoryData|CK01TagData|CK01ToolData|CK01CookingGameConfigData|CK01FridgeCapacityLevelData|CK01ProcessActionData|CK01ProcessingRecordData|CK01RecipeVariantData|KitchenAreaData|LocTableAsset'
Assert-SourceNotContains ($runtimeManagersSource + $presenterSource) "(?s)\[SerializeField\]\s+private\s+(?:List<\s*)?(?:$rowAssetTypes)" `
    'ShortCycle production code must not serialize generated row assets or row lists.'
Assert-SourceContains $builderSource 'LoadRequired<CK01GeneratedDataCatalog>\s*\(\s*CK01ImportRegistry\.DataCatalogAssetPath\s*\)' `
    'ShortCycle builder must bind the unique generated catalog asset.'
Assert-SourceNotContains $builderSource 'Generated/DataTables/(?:Cooking/(?:Items|Recipes|RecipeSteps)|Localization/Localization)/' `
    'ShortCycle builder must not load generated row assets by hard-coded path.'
Assert-SourceNotContains ($catalogSource + $runtimeManagersSource + $presenterSource) 'Resources\s*\.\s*Load' `
    'Generated data resolution must not introduce a raw Resources path.'
Assert-SourceContains $stateMachineSource 'class\s+ShortCyclePhase0State\s*:\s*ShortCycleSessionStateBase' `
    'Phase0 must be an explicit session state class.'
Assert-SourceContains $stateMachineSource 'class\s+ShortCyclePhase1State\s*:\s*ShortCycleSessionStateBase' `
    'Phase1 must be an explicit session state class.'
Assert-SourceContains $stateMachineSource 'class\s+ShortCyclePhase2HandoffState\s*:\s*ShortCycleSessionStateBase' `
    'Phase2 handoff must be an explicit session state class.'
foreach ($stateName in @('Cover','Browse','Catalog','ClueBoard')) {
    Assert-SourceContains $stateMachineSource ("class\s+ShortCyclePhase0{0}State\s*:\s*ShortCyclePhase0StateBase" -f $stateName) `
        "Phase0 $stateName must be an explicit sub-state class."
}
Assert-SourceNotContains $stateMachineSource 'class\s+ShortCyclePhase0StepDetailState' `
    'StepDetail must be an orthogonal modal rather than a base Phase0 sub-state.'
Assert-SourceContains $stateMachineSource 'TryOpenModal\(ShortCycleModalId\.StepDetail\)' `
    'Opening StepDetail must call the modal host without hiding Browse.'
Assert-SourceContains $stateMachineSource 'TryCloseCatalog\s*\(' `
    'Catalog close must be owned by the core state machine.'
Assert-SourceContains $stateMachineSource 'ShortCycleTransitionResult\.Failure\("no_recipe"\)' `
    'No-selection Catalog close must expose the stable no_recipe reason.'
Assert-SourceContains $sessionManagerSource 'useDefaultRecipeForDebug\s*=\s*false' `
    'Production startup must not opt into the default recipe hint.'
Assert-SourceContains $sessionManagerSource 'ClueBoardBind skipped\(no recipe\)' `
    'No-selection startup must preserve the five-step log with an explicit skipped step.'
Assert-SourceContains $stateGateSource 'requireSelectedRecipe' `
    'The common state gate must expose the reviewed selected-recipe condition.'
Assert-SourceContains $stateGateSource 'Context\.HasSelectedRecipe' `
    'The selected-recipe gate must read the runtime Context rather than duplicate state.'
foreach ($trayStateName in @('Collapsed','Highlight','Detail')) {
    Assert-SourceContains $trayStateMachineSource ("class\s+ShortCycleTray{0}State\s*:\s*ShortCycleTrayStateBase" -f $trayStateName) `
        "Tray $trayStateName must be an explicit sub-state class."
}
Assert-SourceNotContains $stateMachineSource '\bswitch\s*\(' `
    'Session presentation state must not be implemented as enum plus switch.'
Assert-SourceNotContains $presenterSource 'RunAutomatedProbe|AutomatedProbeSummary|runAutomatedProbe' `
    'ShortCycleScenePresenter must not contain automated probe implementation or state.'
Assert-SourceContains $probeSource 'class\s+ShortCycleAutomatedProbe' `
    'Automated probe must be split into its own component.'
Assert-SourceContains $probeSource 'enabled\s*=\s*false' `
    'Automated probe must default to disabled.'
Assert-SourceNotContains $inputAdapterSource 'ShortCycleKeyboardBinding|reservedKeyboardBindings|input_path' `
    'ShortCycle must not maintain a parallel keyboard binding structure.'
Assert-SourceContains $buttonSource 'KeyboardListener\s+linkedKeyboardListener' `
    'Keyboard binding must continue through Button_MouseInteract.linkedKeyboardListener.'
Assert-SourceContains $commandBridgeSource '\[Command\(' `
    'ShortCycle actions must be exposed through the project Command attribute.'
Assert-SourceContains $commandBridgeSource 'actionRouter\.DispatchAction' `
    'Command bridge must be one-way into the existing action router.'
Assert-SourceNotContains $commandBridgeSource '\bCommandManager\b' `
    'The one-way action bridge must not modify or directly depend on CommandManager.'
foreach ($pieceName in @('FridgeSlot','TraySlot','CatalogCard','IngredientCard','SlotBlock','Sticker')) {
    Assert-SourceContains $gridPieceSource ("class\s+{0}\s*:\s*ShortCycleGridPieceBase" -f $pieceName) `
        "$pieceName must consume the common grid-piece prefab contract."
}
Assert-SourceContains $gridPieceSource '\bTextMeshPro\b' `
    'World-space grid-piece text must use TextMeshPro 3D.'
Assert-SourceContains $gridPieceSource '\bSortingGroup\b' `
    'Composite grid-piece roots must expose SortingGroup wiring.'
Assert-SourceContains $gridPieceSource 'IShortCycleStateGatedInteraction' `
    'Grid-piece interaction must be gated by the explicit FSM contract.'
Assert-SourceNotContains $gridPieceSource 'UnityEngine\.UI|TextMeshProUGUI|Canvas|RectTransform' `
    'World-space grid pieces must have zero UGUI dependencies.'
Assert-SourceContains $textFitterSource 'TMP_Text\s+targetText' `
    'FridgeIngredientLabelTextFitter must remain compatible with TextMeshPro 3D through TMP_Text.'
Assert-SourceNotContains $shortCycleProductionSource '\bstatic\s+[A-Za-z0-9_<>.,]+\s+Instance\b' `
    'ShortCycle runtime must not introduce a static Instance singleton.'
Assert-SourceNotContains $shortCycleProductionSource '\.Find\s*\(|FindObjectOfType' `
    'ShortCycle production code must use explicit references instead of name/global lookups.'
Assert-SourceContains $directorSource 'interactionLayerStack\.Push\s*\(' `
    'PresentationDirector EnterSpace/PushModal must push explicit project interaction layers.'
Assert-SourceContains $directorSource 'interactionLayerStack\.Pop\s*\(' `
    'PresentationDirector LeaveSpace/PopModal must pop explicit project interaction layers.'
Assert-SourceContains $sessionManagerSource 'fridgeBinder\.Bind\s*\(' `
    'SessionManager BeginSession must bind fridge content before entering the FSM.'
Assert-SourceContains $sessionManagerSource 'clueBoardBinder\.Bind\s*\(' `
    'SessionManager BeginSession must bind clue-board content before entering the FSM.'
Assert-SourceContains $sessionManagerSource 'LocalizedText\.RefreshAll\s*\(' `
    'SessionManager BeginSession must refresh localization after both Binder calls.'
Assert-SourceNotContains $trayControllerSource 'ShortCycleTrayPresentation|trayPresentation|\.Apply\s*\(' `
    'TrayStateController must not retain a direct TrayPresentation fallback path.'
Assert-SourceNotContains $shortCycleCoreSource 'using\s+UnityEngine' `
    'ShortCycle Core must remain independent from UnityEngine.'
Assert-SourceNotContains $fridgeBinderSource 'Instantiate\s*\(|new\s+GameObject\s*\(' `
    'Fridge Binder must reuse serialized slots instead of allocating scene instances.'
Assert-SourceNotContains $clueBinderSource 'Instantiate\s*\(|new\s+GameObject\s*\(' `
    'Clue-board Binder must reuse serialized SlotBlocks instead of allocating scene instances.'
Assert-SourceNotContains $clueBinderSource '\.SetActive\s*\(' `
    'Clue-board Binder must not own presentation activation.'
Assert-SourceContains $clueBinderSource 'emptyBlocks\[index\]\.ClearSlot\s*\(\s*\)' `
    'An empty recipe must clear existing clue blocks through their content API.'
Assert-SourceNotContains ($fridgeBinderSource + $clueBinderSource) 'ShortCyclePresentation(?:Director|Set)|ShortCycleSpace' `
    'Content Binders must not depend on Presentation types.'
# TMP 3D's existing rectTransform.pivot property is not a new UI container dependency.
Assert-SourceNotContains $shortCycleProductionSource 'UnityEngine\.UI|TextMeshProUGUI|(?-i:\bRectTransform\b)|CanvasRenderer|GetComponent\s*<\s*Canvas\s*>' `
    'ShortCycle production code must remain world-space Sprite/TMP without UGUI or Canvas dependencies.'
Assert-SourceNotContains $presenterSource 'ApplyAI41PhaseVisualGate|\bTextMesh\b' `
    'The presenter must not retain AI41 parallel gating or legacy TextMesh status labels.'
Assert-SourceContains $actionRegistrySource 'public\s+string\s+recipe_id\b' `
    'select_recipe must carry recipe_id on ShortCycleActionRequest.'
foreach ($actionToken in @('OpenCover','CloseCover','OpenCatalog','CloseCatalog','SelectRecipe','OpenClueBoard','CloseClueBoard','OpenStepDetail','CloseStepDetail','ToggleFavorite','StartCooking','Return','AdvanceToPhase2','Escape')) {
    Assert-SourceContains $actionRouterSource ("Register\(ShortCycleActionNames\.{0}," -f $actionToken) `
        "ActionRouter must register $actionToken exactly through the named registry."
}
Assert-SourceContains $actionRouterSource 'Register\(ShortCycleActionNames\.FridgeScroll,\s*HandleFridgeScroll\)' `
    'fridge.scroll must use its real handler in the unique ActionRouter.'
if ([regex]::Matches($actionRouterSource, 'registry\.Register\([^,]+,\s*HandleNotImplemented\)').Count -ne 8) {
    throw 'Exactly the eight deferred actions other than fridge.scroll must remain NotImplemented.'
}
Assert-SourceContains $actionRouterSource '(?s)HandleFridgeScroll\s*\(\s*ShortCycleActionRequest\s+request\s*\).*?CurrentPhase\s*!=\s*ShortCyclePhase\.Phase1.*?coordinator\.ScrollFridgeTier\s*\(\s*request\.delta\s*<\s*0f\s*\?\s*-1\s*:\s*1\s*\)' `
    'The fridge.scroll handler must gate Phase1 and normalize into the coordinator proxy.'
Assert-SourceContains $sessionManagerSource '(?s)ShortCycleActionResult\s+ScrollFridgeTier\s*\(\s*int\s+sign\s*\).*?fridgeBinder\.ScrollTier\s*\(\s*sign\s*\)' `
    'SessionManager must expose only a thin proxy to its existing serialized FridgeBinder.'
Assert-SourceContains $cameraPanSource 'event\s+Action<ShortCycleCameraSlot,\s*ShortCycleCameraSlot>\s+PanStarted' `
    'Camera pan must publish PanStarted(fromSlot,toSlot).'
Assert-SourceContains $cameraPanSource 'event\s+Action<ShortCycleCameraSlot>\s+PanCompleted' `
    'Camera pan must publish PanCompleted(toSlot).'
Assert-SourceContains $cameraPanSource 'curve\s*!=\s*null\s*&&\s*curve\.length\s*>\s*0' `
    'Camera pan must reject both null and zero-key curves.'
Assert-SourceContains $cameraPanSource 'AnimationCurve\.Linear\s*\(\s*0f\s*,\s*0f\s*,\s*1f\s*,\s*1f\s*\)' `
    'An unconfigured camera pan must normalize to an explicit linear 0-to-1 curve.'
Assert-SourceContains $cameraPanSource 'HasUsableCurve\s*\(\s*curve\s*\)\s*\?\s*curve\.Evaluate\s*\(\s*progress\s*\)\s*:\s*progress' `
    'Camera pan must preserve valid authored curves while using linear progress for missing curves.'
Assert-SourceContains $cameraPanSource '!float\.IsNaN\s*\(\s*duration\s*\)\s*&&\s*!float\.IsInfinity\s*\(\s*duration\s*\)' `
    'Camera pan must reject non-finite durations instead of hanging.'
Assert-SourceContains $cameraPanSource '(?s)if\s*\(\s*activePan\s*!=\s*null\s*\)\s*\{\s*StopActivePan\s*\(\s*\)\s*;\s*\}.*?slot\s*==\s*CurrentSlot\s*&&\s*IsAtMarker' `
    'Camera pan must cancel an active request before considering same-slot synchronous completion.'
Assert-SourceContains $cameraPanSource 'Vector3\.Distance\s*\(\s*position\s*,\s*markerPosition\s*\)\s*<=\s*0\.0001f' `
    'Same-slot synchronous completion must require the camera to be physically at the requested marker.'
Assert-SourceNotContains $overflowGateSource 'SetActive|\.Find\s*\(|FindObjectOfType|CurrentPhase' `
    'Overflow visibility must depend only on explicit camera slots and Renderer/alpha controls.'
Assert-SourceContains $overflowGateSource 'Renderer\[\]\s+gatedRenderers' `
    'Overflow visibility must expose an explicit Renderer array.'
Assert-SourceNotContains $manualDriverSource 'AutomatedProbe|\bstatic\s+ShortCycleManualVerificationDriver\b' `
    'The manual driver must have no automated-probe dependency or static singleton.'
Assert-SourceContains $manualDriverSource '\bTextMeshPro\s+statusBoard' `
    'The manual driver status board must use world-space TextMeshPro 3D.'
Assert-SourceContains $projectSettingsSource '(?m)^\s*activeInputHandler:\s*2\s*$' `
    'The editor-external keyboard contract expects Active Input Handling = Both.'
Assert-SourceContains $manualDriverSource 'InputManager\.OnKeyPressed\s*\+=\s*HandleKeyPressed' `
    'The manual driver must read keys from the shared InputManager event.'
Assert-SourceNotContains $manualDriverSource 'Input\.GetKeyDown|Keyboard\.current' `
    'The manual driver must not introduce a second keyboard backend.'
Assert-SourceContains $globalInputManagerSource 'FindActionMap\s*\(\s*"Game"\s*(?:,|\))' `
    'InputManager must resolve the configured Game action map.'
Assert-SourceContains $globalInputManagerSource 'FindAction\s*\(\s*"KeyboardAnyKey"\s*(?:,|\))' `
    'InputManager must resolve the configured KeyboardAnyKey action.'
Assert-SourceContains $globalInputManagerSource '(?s)Keyboard\.current.*?keyboard\.allKeys' `
    'InputManager must translate Input System keyboard controls into shared key events.'
Assert-SourceContains $manualDriverSource '(?s)private\s+void\s+OnEnable\s*\(\s*\).*?OnKeyPressed\s*-=\s*HandleKeyPressed.*?OnKeyPressed\s*\+=\s*HandleKeyPressed' `
    'The manual driver subscription must be idempotent.'
Assert-SourceContains $manualDriverSource '(?s)private\s+void\s+OnDestroy\s*\(\s*\).*?OnKeyPressed\s*-=\s*HandleKeyPressed' `
    'The manual driver must release its keyboard listener when destroyed.'
Assert-SourceContains $manualDriverSource '(?s)private\s+bool\s+HandleKeyPressed.*?Dispatch\s*\(\s*actionName\s*\)' `
    'Mapped keys must enter the named-action dispatch path.'
Assert-SourceContains $manualDriverSource 'actionRouter\.DispatchAction\s*\(\s*request\s*\)' `
    'Business keys must terminate at ShortCycleActionRouter.'
Assert-SourceNotContains $manualDriverSource 'TryShowRecipe|TryStartCooking|TryReturnToPhase0|TryAdvanceToPhase2|TryEscape' `
    'The manual driver must not bypass the router into the state machine or presenter.'
$debugGroupMatch = [regex]::Match($sceneSource, '(?s)&1813977030\s*\r?\nGameObject:(?:(?!--- !u!).)*?m_Name:\s*DebugAndReferences\s*\r?\n(?:(?!--- !u!).)*?m_IsActive:\s*([01])')
if (-not $debugGroupMatch.Success) { throw 'DebugAndReferences and its explicit active state must remain present.' }
if ($debugGroupMatch.Groups[1].Value -ne '0') {
    Write-Warning 'DebugAndReferences is currently active in the user-owned scene; CODE tests preserve it and defer default-inactive restoration to the authorized ENGINE_MCP task.'
}
Assert-SourceContains $sceneSource '(?s)&1773755299\s*\r?\nGameObject:(?:(?!--- !u!).)*?m_Name:\s*Probe_ManualDriver\s*\r?\n(?:(?!--- !u!).)*?m_IsActive:\s*1.*?&1773755301\s*\r?\nMonoBehaviour:(?:(?!--- !u!).)*?m_Enabled:\s*1' `
    'The manual probe must be ready when its inactive DebugAndReferences parent is enabled (one-step activation).'
Assert-SourceContains $sceneSource '(?s)&721214570\s*\r?\nGameObject:.*?m_Name:\s*Managers.*?m_IsActive:\s*1' `
    'The Managers root must be active.'
Assert-SourceContains $sceneSource '(?s)&1912014245\s*\r?\nGameObject:.*?m_Name:\s*InputManager.*?m_IsActive:\s*1.*?&1912014247\s*\r?\nMonoBehaviour:.*?m_Enabled:\s*1' `
    'The scene InputManager object and component must be enabled.'
Assert-SourceContains $sceneSource '(?s)&1873999177\s*\r?\nGameObject:.*?m_Name:\s*MouseManager.*?m_IsActive:\s*1.*?&1873999179\s*\r?\nMonoBehaviour:.*?m_Enabled:\s*1' `
    'The scene MouseManager object and component must be enabled.'
Assert-SourceContains $sceneSource '(?s)m_Name:\s*Escape_Button_TUNE.*?m_IsActive:\s*1.*?guid:\s*b39a23f314318b043bc3fe1ef73eab40.*?m_Enabled:\s*1' `
    'The scene must contain an enabled KeyboardListener.'
$inputActionsGuid = [regex]::Match($inputActionsMetaSource, '(?m)^guid:\s*([0-9a-f]+)\s*$').Groups[1].Value
if ([string]::IsNullOrWhiteSpace($inputActionsGuid)) { throw 'Could not resolve the PlayerControl input-actions GUID.' }
Assert-SourceContains $sceneSource ("m_Actions:.*guid:\s*{0}" -f $inputActionsGuid) `
    'PlayerInput must reference the PlayerControl input-actions asset.'
Assert-SourceContains $sceneSource '(?m)^\s*m_DefaultActionMap:\s*Game\s*$' `
    'PlayerInput must select the Game action map by default.'
Assert-SourceContains $inputActionsSource '"name"\s*:\s*"KeyboardAnyKey"' `
    'PlayerControl must contain the KeyboardAnyKey action.'
Assert-SourceContains $inputActionsSource '"path"\s*:\s*"<Keyboard>/anyKey"' `
    'KeyboardAnyKey must bind to the Input System keyboard anyKey control.'
$removeFallbackMethod = [regex]::Match($localizedTextSource, '(?s)public\s+static\s+IList<string>\s+RemoveFallbackTokens.*?\n\s*private\s+static\s+bool\s+IsFallbackToken').Value
Assert-SourceContains $removeFallbackMethod 'tokens\.Add\(label\.text\)' `
    'RemoveFallbackTokens must record unresolved fallback tokens.'
Assert-SourceNotContains $removeFallbackMethod 'label\.text\s*=' `
    'RemoveFallbackTokens must never clear or rewrite unresolved label text.'
Assert-SourceContains $v22LayoutSource 'TrayAnchorCount\s*=\s*20' `
    'The v2.2 layout contract must expose 20 tray anchors.'
Assert-SourceContains $v22LayoutSource 'SubstituteAnchorCount\s*=\s*8' `
    'The v2.2 layout contract must expose eight substitute anchors.'
Assert-SourceContains $outlineSource 'overrideGroup\s*=\s*false' `
    'Outline members must default to local parameters.'
Assert-SourceContains $outlineSource 'OutlineMergeOverride\s+mergeOutline\s*=\s*OutlineMergeOverride\.FollowGroup' `
    'Outline merge must expose its independent tri-state.'
Assert-SourceContains $outlineSource 'OutlineMergeOverride\s+mergeShadow\s*=\s*OutlineMergeOverride\.FollowGroup' `
    'Shadow merge must expose its independent tri-state.'
Assert-SourceContains $outlineGroupSource 'CreateMissingMembers\s*\(' `
    'Group member creation must be an explicit API.'
Assert-SourceContains $outlineGroupSource 'SynchronizeFollowingMembers\s*\(' `
    'Group parameter synchronization must be an explicit API.'
Assert-SourceNotContains $outlineGroupSource 'OnTransformChildrenChanged\s*\(|private\s+void\s+OnEnable\s*\(' `
    'Group lifecycle must not create or rewrite members.'
Assert-SourceContains $outlineDefaultsSource 'Resources\.Load<SpriteOutlineDefaults>\(ResourcePath\)' `
    'Defaults must use the documented one-shot resource contract.'
$mergedShadowLabel = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('5ZCI5bm25Lit77ya5L2/55So57uE6Zi05b2x5Y+C5pWw'))
Assert-SourceContains $outlineEditorSource ([regex]::Escape($mergedShadowLabel)) `
    'The member inspector must use the reviewed merged-shadow explanation.'
Assert-SourceContains $outlineGroupEditorSource 'Undo\.RegisterFullObjectHierarchyUndo' `
    'Explicit group editor writes must be undoable.'
$formalOutlineSource = $outlineSource + $outlineGroupSource + $outlineDefaultsSource + $outlineEditorSource + $outlineGroupEditorSource
Assert-SourceNotContains -Source $formalOutlineSource -Pattern '[.]Find' -Message 'Formal v2 controls must not use name lookup.'
Assert-SourceNotContains -Source $formalOutlineSource -Pattern 'RendererFeature' -Message 'Formal v2 controls must not add a RendererFeature path.'
Assert-SourceNotContains -Source $formalOutlineSource -Pattern 'Stencil' -Message 'Formal v2 controls must not claim Stencil semantics.'
Assert-SourceNotContains ($v22LayoutSource + $subCardSource) 'GridLayout|LayoutTools|UnityEngine\.UI|TextMeshProUGUI|RectTransform' `
    'Manual tray/substitute contracts must not depend on layout tools or UGUI.'
Assert-SourceContains $subCardSource '(?s)slot\.sub_01.*slot\.sub_02.*slot\.sub_03.*slot\.sub_04.*slot\.sub_05.*slot\.sub_06.*slot\.sub_07.*slot\.sub_08' `
    'T2 substitute consumption must be explicit and ordered from sub_01 through sub_08.'
Assert-SourceContains $migrationV2Source 'Upgrade ShortCycle V2\.2 Contracts' `
    'Migration v2 must expose a dedicated idempotent v2.2 upgrade entry.'
Assert-SourceNotContains $migrationV2Source 'TraySlot_R.*GridLayout' `
    'The v2.2 migration path must not preserve grid-generated tray anchors.'
Assert-SourceContains $migrationV2Source 'if\s*\(\s*changed\s*\)\s*PrefabUtility\.SaveAsPrefabAsset' `
    'The v2.2 prefab upgrade must save only when its serialized contract changed.'
Assert-SourceContains $migrationV2Source 'SetObjectsIfChanged' `
    'The v2.2 builder must compare serialized arrays before writing them.'
Assert-SourceContains $fridgeCatTierSource 'ShelfColumnCount\s*=\s*5' `
    'FridgeCatRig tiers must expose exactly five shelf columns.'
Assert-SourceContains $fridgeCatRigSource 'SetTierCount\s*\(' `
    'FridgeCatRig must expose idempotent tier activation.'
Assert-SourceContains $fridgeCatRigSource 'Instantiate\s*\(\s*extensionTierPrefab' `
    'FridgeCatRig must pool shenti extension tier instances.'
Assert-SourceContains $fridgeCatRigSource 'GetShelfAnchor\s*\(' `
    'FridgeCatRig must expose deterministic row/column anchor lookup.'
Assert-SourceContains $fridgeCatRigSource 'ScrollToTier\s*\(' `
    'FridgeCatRig must expose tier-based programmatic scrolling.'
Assert-SourceNotContains ($fridgeCatRigSource + $fridgeCatTierSource + $fridgeCatEyeSource) '\.Find\s*\(|FindObjectOfType' `
    'Fridge-cat production components must use explicit references only.'
Assert-SourceContains $fridgeCatEyeSource 'SpriteRenderer\s+eyeBase' `
    'FridgeCatEye must explicitly expose its base layer.'
foreach ($eyeLayer in @('pupil','highlight','glow')) {
    Assert-SourceContains $fridgeCatEyeSource ("SpriteRenderer\s+{0}" -f $eyeLayer) `
        "FridgeCatEye must explicitly expose its $eyeLayer layer."
}
Assert-SourceContains $fridgeCatEyeSource 'TransitionController\s+blinkTransition' `
    'FridgeCatEye Blink must consume the registered Transition system.'
Assert-SourceContains $fridgeCatEyeSource 'blinkTransition\.GoTo\s*\(' `
    'FridgeCatEye Blink must run Transition presets instead of Animator.'
Assert-SourceNotContains ($fridgeCatEyeSource + $fridgeCatBlinkSchedulerSource) '\bAnimator\b|SetTrigger\s*\(' `
    'The composite fridge-cat blink path must not restore Animator dependencies.'
Assert-SourceContains $fridgeCatBlinkSchedulerSource '(?s)leftEye\.Blink\s*\(\s*\).*?rightEye\.Blink\s*\(\s*\)' `
    'One scheduler request must invoke both independent eye rigs synchronously.'
Assert-SourceContains $fridgeBinderSource 'SetTierCount\s*\(' `
    'Fridge Binder must size the visual rig before data binding.'
Assert-SourceContains $fridgeBinderSource 'GetRequiredTierCount\s*\(\s*capacity\s*\)' `
    'Fridge Binder tier count must derive from configured capacity rather than inventory row count.'
Assert-SourceNotContains $fridgeBinderSource 'GetRequiredTierCount\s*\(\s*records\.Count\s*\)' `
    'Fridge Binder must not treat the current inventory record count as capacity.'
Assert-SourceContains $fridgeBinderSource 'inventory\.GetQuantity\s*\(\s*record\.ItemId\s*\)\s*>\s*0' `
    'Fridge Binder must exclude zero-quantity records from occupied slots.'
Assert-SourceContains $fridgeBinderSource '(?s)public\s+int\s+ApplyFilterView\s*\(\s*int\s+visibleCount\s*\).*?GetFilterTierCount\s*\(\s*visibleCount\s*\)' `
    'Fridge Binder must expose the reviewed filter-view tier contract without a production route.'
if ([regex]::Matches($shortCycleProductionSource, '\.ApplyFilterView\s*\(').Count -ne 0) {
    throw 'ApplyFilterView must have zero production callers until the later filter route is implemented.'
}
Assert-SourceContains $fridgeBinderSource 'boundCapacity\s*=\s*capacity' `
    'Successful binding must expose the resolved capacity for audit.'
Assert-SourceContains $fridgeBinderSource 'GetShelfAnchor\s*\(' `
    'Fridge Binder must resolve each slot through the row/column rig contract.'
Assert-SourceContains $fridgeBinderSource 'SetParent\s*\(\s*shelfAnchors\[index\]\s*,\s*false\s*\)' `
    'Fridge Binder must parent configured pieces under explicit shelf anchors.'
Assert-SourceContains $fridgeBinderSource '(?s)ShortCycleActionResult\s+ScrollTier\s*\(\s*int\s+sign\s*\).*?fridgeCatRig\.ScrollToTier\s*\(\s*targetTier\s*\).*?currentTier\s*=\s*targetTier' `
    'FridgeBinder scroll must use the typed rig API and update state only after an accepted move.'
Assert-SourceContains $fridgeScrollAdapterSource 'MouseScrollableObject\s+scrollableObject' `
    'The fridge wheel adapter must consume MouseInteract MouseScrollableObject.'
Assert-SourceContains $fridgeScrollAdapterSource '(?s)OnEnable\s*\(\s*\).*?scrollStepEvent\.RemoveListener\s*\(\s*HandleScrollStep\s*\).*?scrollStepEvent\.AddListener\s*\(\s*HandleScrollStep\s*\)' `
    'The fridge wheel adapter must own one idempotent MouseInteract event subscription.'
Assert-SourceContains $fridgeScrollAdapterSource 'CurrentPhase\s*!=\s*ShortCyclePhase\.Phase1' `
    'The fridge wheel adapter must gate dispatch to Phase1.'
Assert-SourceContains $fridgeScrollAdapterSource '(?s)ActionName\s*=\s*ShortCycleActionNames\.FridgeScroll.*?delta\s*=\s*delta\s*<\s*0f\s*\?\s*-1f\s*:\s*1f' `
    'The fridge wheel adapter must normalize non-zero wheel input to a named +/-1 action request.'
Assert-SourceContains $scrollAreaSource 'ScrollStepMode\s+stepMode\s*=\s*ScrollStepMode\.Discrete' `
    'ScrollArea must expose the reviewed Discrete step mode.'
Assert-SourceContains $scrollAreaSource 'float\s+stepSize\s*=\s*0\.5f' `
    'ScrollArea must expose a fixed normalized discrete step size.'
Assert-SourceContains $scrollAreaSource 'scrollPositionTransition\.PlayTo\s*\(' `
    'Programmatic ScrollTo must consume TK-MOT-07 Position PlayTo.'
Assert-SourceContains $presentationSetSource '(?s)OnDisable\s*\(\s*\).*?RestoreTransientState\s*\(\s*false\s*\).*?OnDestroy\s*\(\s*\).*?RestoreTransientState\s*\(\s*false\s*\)' `
    'PresentationSet lifecycle exits must restore transient state without rewriting container activation.'
Assert-SourceContains $presentationSetSource '(?s)public\s+void\s+RestoreTransientState\s*\(\s*\).*?RestoreTransientState\s*\(\s*true\s*\)' `
    'Explicit PresentationSet cleanup must preserve the public container-state restoration contract.'
Assert-SourceContains $scrollAreaSource '(?s)CanAnimateProgrammaticScroll\s*\(\s*\).*?isActiveAndEnabled.*?gameObject\.activeInHierarchy.*?scrollPositionTransition\.isActiveAndEnabled.*?scrollPositionTransition\.gameObject\.activeInHierarchy' `
    'ScrollArea must guard both controller and Transition hierarchy activity before PlayTo.'
Assert-SourceContains $scrollAreaSource '(?s)OnDisable\s*\(\s*\).*?CompletePendingProgrammaticScroll\s*\(\s*\).*?CompleteProgrammaticScroll.*?CompleteScrollImmediately' `
    'ScrollArea must deterministically finish programmatic targets across lifecycle exit and inactive fallback.'
Assert-SourceContains $scrollAreaSource '(?s)!_initialized\s*&&\s*viewportDragLimit\s*!=\s*null.*?_pendingInitializationNormalizedPosition\s*=\s*clampedNormalizedPosition.*?_hasPendingInitializationScroll\s*=\s*true.*?return' `
    'ScrollArea must preserve normalized intent instead of resolving a target against pre-initialize bounds.'
Assert-SourceContains $scrollAreaSource '(?s)contentDragContainer\.UpdateBounds\s*\(\s*\).*?_initialized\s*=\s*true.*?_hasPendingInitializationScroll\s*=\s*false.*?ApplyScrollTarget\s*\(\s*pendingNormalizedPosition\s*,\s*false\s*\)' `
    'ScrollArea must consume a deferred target once after final runtime bounds are initialized.'
Assert-SourceContains $unityStubsSource '(?s)ApplyUpdatedBoundsOnUpdate.*?UpdateBoundsCount\+\+.*?LimitMinY\s*=\s*UpdatedLimitMinY.*?MoveToTargetCount\+\+' `
    'Editor-external stubs must model initialization-time bound changes and observable target application.'
Assert-SourceContains $unityStubsSource '(?s)StartCoroutine.*?!isActiveAndEnabled.*?!gameObject\.activeInHierarchy.*?InvalidOperationException' `
    'Editor-external stubs must reject the inactive coroutine path seen in Unity.'
Assert-SourceContains $unityStubsSource '(?s)ThrowOnSetActive.*?SetActiveCallCount.*?SetActive\s*\(.*?ThrowOnSetActive.*?InvalidOperationException' `
    'Editor-external stubs must expose lifecycle SetActive re-entry attempts.'
foreach ($legacyScrollMethod in @('ScrollTo','ScrollVerticalTo','ScrollHorizontalTo')) {
    Assert-SourceContains $scrollAreaSource ("public\s+void\s+{0}\s*\(" -f $legacyScrollMethod) `
        "ScrollArea must preserve the legacy $legacyScrollMethod API."
}

if ($cameraFrameRuntimePath -match '[\\/]Editor[\\/]') {
    throw 'CameraFrameGizmo runtime component must not live below an Editor folder.'
}
$cameraFrameAncestor = Split-Path -Parent $cameraFrameRuntimePath
$cameraFrameAsmdefs = @()
while ($cameraFrameAncestor -and $cameraFrameAncestor.StartsWith((Join-Path $repoRoot 'Assets'), [System.StringComparison]::OrdinalIgnoreCase)) {
    $cameraFrameAsmdefs += @(Get-ChildItem -LiteralPath $cameraFrameAncestor -Filter '*.asmdef' -File -ErrorAction SilentlyContinue)
    $parent = Split-Path -Parent $cameraFrameAncestor
    if ($parent -eq $cameraFrameAncestor) { break }
    $cameraFrameAncestor = $parent
}
if ($cameraFrameAsmdefs.Count -ne 0) {
    throw 'CameraFrameGizmo runtime ownership unexpectedly changed from Assembly-CSharp via an ancestor asmdef.'
}
Assert-SourceNotContains $cameraFrameRuntimeSource 'using\s+UnityEditor|CameraFrameGizmoMenu|EatWhat\.Cooking' `
    'The attachable CameraFrameGizmo runtime component must not depend on editor/menu/business code.'
Assert-SourceContains $cameraFrameRuntimeSource 'class\s+CameraFrameGizmo\s*:\s*MonoBehaviour' `
    'CameraFrameGizmo must remain an attachable MonoBehaviour in the runtime assembly.'
foreach ($fieldContract in @('Camera\s+referenceCamera','Transform\s+phase1Marker','Transform\s+phase0Marker','Transform\s+rightMarker')) {
    Assert-SourceContains $cameraFrameRuntimeSource ("public\s+{0}\s*;" -f $fieldContract) `
        "CameraFrameGizmo must preserve public field contract $fieldContract."
}
Assert-SourceContains $cameraFrameRuntimeSource 'static\s+Vector2\s+FrameSize\s*\(' `
    'CameraFrameGizmo must preserve FrameSize.'
Assert-SourceContains $cameraFrameRuntimeSource 'Transform\s+GetMarker\s*\(' `
    'CameraFrameGizmo must preserve GetMarker.'
if ([regex]::Matches(($cameraFrameRuntimeSource + "`n" + $cameraFrameDrawerSource), 'class\s+CameraFrameGizmo\s*:\s*MonoBehaviour').Count -ne 1) {
    throw 'Exactly one CameraFrameGizmo MonoBehaviour declaration must remain across runtime and editor sources.'
}
Assert-SourceContains $cameraFrameDrawerSource 'static\s+class\s+CameraFrameGizmoDrawer' `
    'Editor drawing must live in the static CameraFrameGizmoDrawer.'
Assert-SourceNotContains $cameraFrameDrawerSource 'class\s+CameraFrameGizmo\s*:\s*MonoBehaviour|OnDrawGizmos\s*\(' `
    'The Editor source must not redeclare the component or use an instance OnDrawGizmos callback.'
Assert-SourceContains $cameraFrameDrawerSource '\[DrawGizmo\s*\(\s*GizmoType\.Selected\s*\|\s*GizmoType\.NonSelected\s*\)\]' `
    'Camera-frame rendering must be registered with DrawGizmo for selected and non-selected objects.'
Assert-SourceContains $cameraFrameMenuSource 'LookAt\s*\(\s*marker\.position\s*,\s*Quaternion\.identity\s*,\s*7\.2f\s*,\s*true\s*,\s*true\s*\)' `
    'Camera framing must retain the reviewed five-parameter SceneView.LookAt call.'
Assert-SourceNotContains $cameraFrameMenuSource 'LookAtDirect\s*\(' `
    'Camera framing must not regress to the unavailable LookAtDirect API.'

$projectVersion = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ProjectSettings/ProjectVersion.txt')
$unityVersion = [regex]::Match($projectVersion, 'm_EditorVersion:\s*([^\r\n]+)').Groups[1].Value.Trim()
if ([string]::IsNullOrWhiteSpace($unityVersion)) { throw 'Could not resolve project Unity version.' }
if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $UnityEditorPath = @("C:/Program Files/Unity/Hub/Editor/$unityVersion/Editor/Unity.exe", "C:/Program Files/Unity $unityVersion/Editor/Unity.exe") | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $UnityEditorPath -or -not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw 'Pass -UnityEditorPath for the project Editor.' }
if (-not (Get-Item -LiteralPath $UnityEditorPath).VersionInfo.ProductVersion.StartsWith($unityVersion, [StringComparison]::Ordinal)) { throw 'Editor version does not match ProjectVersion.txt.' }
$unityRoot = Split-Path -Parent (Split-Path -Parent $UnityEditorPath)
$unityEngineFacadePath = Join-Path $unityRoot 'Editor\Data\Managed\UnityEngine\UnityEngine.dll'
$unityCoreModulePath = Join-Path $unityRoot 'Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll'
$unityImguiModulePath = Join-Path $unityRoot 'Editor\Data\Managed\UnityEngine\UnityEngine.IMGUIModule.dll'
$unityEditorAssemblyPath = Join-Path $unityRoot 'Editor\Data\Managed\UnityEditor.dll'
$unityMonoRoot = Join-Path $unityRoot 'Editor\Data\MonoBleedingEdge'
$unityMonoPath = Join-Path $unityMonoRoot 'bin\mono.exe'
$unityCscPath = Join-Path $unityMonoRoot 'lib\mono\4.5\csc.exe'
$unityFacadePath = Join-Path $unityMonoRoot 'lib\mono\4.5\Facades'
$unityMscorlibPath = Join-Path $unityRoot 'Editor\Data\NetStandard\compat\2.1.0\shims\netfx\mscorlib.dll'
$unityNetStandardPath = Join-Path $unityRoot 'Editor\Data\NetStandard\ref\2.1.0\netstandard.dll'
foreach ($requiredUnityPath in @($unityEngineFacadePath, $unityCoreModulePath, $unityImguiModulePath,
        $unityEditorAssemblyPath, $unityMonoPath, $unityCscPath, $unityMscorlibPath,
        $unityNetStandardPath)) {
    if (-not (Test-Path -LiteralPath $requiredUnityPath -PathType Leaf)) {
        throw "Required Unity $unityVersion verification dependency is missing: $requiredUnityPath"
    }
}
$cameraFrameCompileRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('EatWhat-CameraFrameGizmo-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $cameraFrameCompileRoot | Out-Null
try {
    $cameraFrameRuntimeDll = Join-Path $cameraFrameCompileRoot 'CameraFrameGizmo.Runtime.dll'
    $previousMonoPath = $env:MONO_PATH
    $env:MONO_PATH = $unityFacadePath
    $compileOutput = & $unityMonoPath $unityCscPath /nologo /nostdlib+ /target:library `
        ("/out:{0}" -f $cameraFrameRuntimeDll) ("/reference:{0}" -f $unityMscorlibPath) `
        ("/reference:{0}" -f $unityNetStandardPath) ("/reference:{0}" -f $unityCoreModulePath) `
        $cameraFrameRuntimePath 2>&1
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $cameraFrameRuntimeDll -PathType Leaf)) {
        throw "CameraFrameGizmo runtime source did not compile independently against the real UnityEngine DLL: $compileOutput"
    }
    $cameraFrameEditorDll = Join-Path $cameraFrameCompileRoot 'CameraFrameGizmo.Editor.dll'
    $compileOutput = & $unityMonoPath $unityCscPath /nologo /nostdlib+ /target:library `
        ("/out:{0}" -f $cameraFrameEditorDll) ("/reference:{0}" -f $unityMscorlibPath) `
        ("/reference:{0}" -f $unityNetStandardPath) ("/reference:{0}" -f $unityEngineFacadePath) `
        ("/reference:{0}" -f $unityCoreModulePath) ("/reference:{0}" -f $unityImguiModulePath) `
        ("/reference:{0}" -f $unityEditorAssemblyPath) ("/reference:{0}" -f $cameraFrameRuntimeDll) `
        $cameraFrameDrawerPath $cameraFrameMenuPath 2>&1
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $cameraFrameEditorDll -PathType Leaf)) {
        throw "CameraFrameGizmo drawer/menu did not compile against the real UnityEditor DLL: $compileOutput"
    }
}
finally {
    $env:MONO_PATH = $previousMonoPath
    if (Test-Path -LiteralPath $cameraFrameCompileRoot) {
        Remove-Item -LiteralPath $cameraFrameCompileRoot -Recurse -Force
    }
}

$sourcePaths = @(
    $unityStubsPath,
    $outlinePath,
    $outlineGroupPath,
    $outlineDefaultsPath,
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CookingEnums.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01DataModels.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01RecipeSlotData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01RecipeStepData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01ItemData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01TagData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01ToolData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01CookingGameConfigData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01FridgeCapacityLevelData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01ProcessActionData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01ProcessingRecordData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01RecipeVariantData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\CK01InitialInventoryData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\CookingData\KitchenAreaData.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\Localization\LocTextCatalog.cs'),
    (Join-Path $repoRoot 'Assets\Scripts\DataBase\Localization\LocTableAsset.cs'),
    $catalogPath,
    $localizedTextPath,
    $fridgeCatBlinkSchedulerPath,
    $scrollAreaPath,
    $cameraFrameRuntimePath,
    $cameraFrameDrawerPath,
    $cameraFrameMenuPath,
    (Join-Path $PSScriptRoot 'ShortCycleCoreTests.cs')
) + $shortCycleCompilePaths + $transitionCompilePaths

Add-Type -Path $sourcePaths
$result = [EatWhat.Cooking.ShortCycle.Tests.ShortCycleCoreTests]::Run()
[pscustomobject]@{
    success = $true
    result = $result
    compiledSourceCount = $sourcePaths.Count
    staticBindingAssertions = 216
} | ConvertTo-Json -Depth 3
