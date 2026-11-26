using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class ShortCycleManualKeyBinding
    {
        public ShortCycleManualKeyBinding(Key key, string actionName)
        {
            Key = key;
            ActionName = actionName;
        }

        public Key Key { get; private set; }
        public string ActionName { get; private set; }
    }

    /// <summary>Stable G24 keyboard map for the optional manual driver.</summary>
    public static class ShortCycleManualVerificationKeyMap
    {
        private static readonly IList<ShortCycleManualKeyBinding> bindings =
            new List<ShortCycleManualKeyBinding>
            {
                new ShortCycleManualKeyBinding(Key.Digit1, ShortCycleActionNames.OpenCover),
                new ShortCycleManualKeyBinding(Key.Digit2, ShortCycleActionNames.OpenCatalog),
                new ShortCycleManualKeyBinding(Key.Digit3, ShortCycleActionNames.CloseCatalog),
                new ShortCycleManualKeyBinding(Key.Digit4, ShortCycleActionNames.SelectRecipe),
                new ShortCycleManualKeyBinding(Key.Digit5, ShortCycleActionNames.OpenClueBoard),
                new ShortCycleManualKeyBinding(Key.Digit6, ShortCycleActionNames.CloseClueBoard),
                new ShortCycleManualKeyBinding(Key.Digit7, ShortCycleActionNames.OpenStepDetail),
                new ShortCycleManualKeyBinding(Key.Digit8, ShortCycleActionNames.CloseStepDetail),
                new ShortCycleManualKeyBinding(Key.F, ShortCycleActionNames.ToggleFavorite),
                new ShortCycleManualKeyBinding(Key.C, ShortCycleActionNames.StartCooking),
                new ShortCycleManualKeyBinding(Key.R, ShortCycleActionNames.Return),
                new ShortCycleManualKeyBinding(Key.P, ShortCycleActionNames.AdvanceToPhase2),
                new ShortCycleManualKeyBinding(Key.T, ShortCycleActionNames.TrayToggleDetail),
                new ShortCycleManualKeyBinding(Key.Escape, ShortCycleActionNames.Escape)
            }.AsReadOnly();

        public static IList<ShortCycleManualKeyBinding> Bindings { get { return bindings; } }

        public static bool TryGetAction(Key key, out string actionName)
        {
            for (var index = 0; index < bindings.Count; index++)
            {
                if (bindings[index].Key == key)
                {
                    actionName = bindings[index].ActionName;
                    return true;
                }
            }

            actionName = null;
            return false;
        }
    }

    /// <summary>
    /// Optional DebugAndReferences/Probe_ManualDriver component. It consumes the
    /// existing InputManager backend and sends every command through ActionRouter.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cooking/Short Cycle/Debug/Manual Verification Driver")]
    public sealed class ShortCycleManualVerificationDriver : MonoBehaviour
    {
        [Header("Explicit runtime references")]
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private ShortCycleSessionManager coordinator = null;
        [SerializeField] private ShortCycleTrayStateController trayStateController = null;
        [SerializeField] private ShortCycleInputLockController inputLockController = null;
        [SerializeField] private ShortCycleCameraPanController cameraPanController = null;
        [SerializeField] private TextMeshPro statusBoard = null;
        [SerializeField, Tooltip("Optional; defaults to the tagged Main Camera.")] private Camera statusCamera = null;

        [Header("Parameterized action values")]
        [SerializeField] private string verificationRecipeId = "rcp_qingtang_noodle";

        public string RecentAction { get; private set; }
        public bool RecentActionSucceeded { get; private set; }

        private int dispatchSequence;
        private bool debugHighlightActive;
        private float originalFontSize;
        private Vector3 originalBoardPosition;
        private Quaternion originalBoardRotation;
        private Vector2 originalBoardPivot;
        private TextAlignmentOptions originalBoardAlignment;
        private bool boardCaptured;

        private void Reset()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            // Keep the optional debug listener idempotent across unusual editor
            // lifecycle sequences (domain reloads or repeated enable callbacks).
            InputManager.OnKeyPressed -= HandleKeyPressed;
            InputManager.OnKeyPressed += HandleKeyPressed;
            RefreshStatusBoard();
        }

        private void OnDisable()
        {
            InputManager.OnKeyPressed -= HandleKeyPressed;
            RestoreStatusBoard();
        }

        private void OnDestroy()
        {
            InputManager.OnKeyPressed -= HandleKeyPressed;
            RestoreStatusBoard();
        }

        private void Update()
        {
            RefreshStatusBoard();
        }

        private void LateUpdate()
        {
            if (statusBoard == null) return;
            var camera = statusCamera != null ? statusCamera : Camera.main;
            if (camera == null || !camera.orthographic) return;
            if (!boardCaptured)
            {
                originalFontSize = statusBoard.fontSize;
                originalBoardPosition = statusBoard.transform.position;
                originalBoardRotation = statusBoard.transform.rotation;
                originalBoardPivot = statusBoard.rectTransform.pivot;
                originalBoardAlignment = statusBoard.alignment;
                boardCaptured = true;
            }
            var depth = Mathf.Max(camera.nearClipPlane + 0.1f, 1f);
            statusBoard.rectTransform.pivot = new Vector2(0f, 1f);
            statusBoard.alignment = TextAlignmentOptions.TopLeft;
            statusBoard.transform.position = camera.ViewportToWorldPoint(new Vector3(0.03f, 0.97f, depth));
            statusBoard.transform.rotation = camera.transform.rotation;
            statusBoard.fontSize = originalFontSize * camera.orthographicSize / 7.2f;
        }

        private void RestoreStatusBoard()
        {
            if (!boardCaptured || statusBoard == null) return;
            statusBoard.fontSize = originalFontSize;
            statusBoard.transform.position = originalBoardPosition;
            statusBoard.transform.rotation = originalBoardRotation;
            statusBoard.rectTransform.pivot = originalBoardPivot;
            statusBoard.alignment = originalBoardAlignment;
            boardCaptured = false;
        }

        private bool HandleKeyPressed(Key key)
        {
            if (key == Key.H)
            {
                // DEBUG-ONLY direct call: highlight is a transient hover state,
                // not a production keyboard action (D14/G5).
                debugHighlightActive = !debugHighlightActive;
                if (trayStateController != null)
                {
                    if (debugHighlightActive) trayStateController.ShowHighlight();
                    else trayStateController.ShowCollapsed();
                }
                RefreshStatusBoard();
                return true;
            }

            string actionName;
            if (!ShortCycleManualVerificationKeyMap.TryGetAction(key, out actionName))
            {
                return false;
            }

            Dispatch(actionName);
            return true;
        }

        public ShortCycleActionResult Dispatch(string actionName)
        {
            var sequence = ++dispatchSequence;
            var before = ReadState();
            var request = new ShortCycleActionRequest
            {
                ActionName = actionName,
                recipe_id = actionName == ShortCycleActionNames.SelectRecipe
                    ? verificationRecipeId
                    : null
            };
            var result = actionRouter == null
                ? ShortCycleActionResult.Failure("ShortCycleActionRouter is not connected.")
                : actionRouter.DispatchAction(request);

            RecentAction = actionName;
            RecentActionSucceeded = result.Succeeded;
            var after = ReadState();
            Debug.Log(
                "CK01-C-DRIVER seq=" + sequence + " action=" + actionName +
                " succeeded=" + result.Succeeded +
                " before=" + before +
                " after=" + after +
                " error=" + (result.Error ?? string.Empty),
                this);
            RefreshStatusBoard();
            return result;
        }

        private string ReadState()
        {
            var phase = coordinator == null ? ShortCyclePhase.None : coordinator.CurrentPhase;
            var view = coordinator == null ? ShortCyclePhase0View.Cover : coordinator.CurrentPhase0View;
            var tray = trayStateController == null ? ShortCycleTrayView.Collapsed : trayStateController.CurrentView;
            var inputLocked = inputLockController != null && inputLockController.Gate.IsLocked;
            var cameraX = cameraPanController == null ? 0f : cameraPanController.CameraX;
            var hasSelectedRecipe = coordinator != null && coordinator.Context != null &&
                coordinator.Context.HasSelectedRecipe;
            return "phase=" + phase +
                ",view=" + view +
                ",hasSelectedRecipe=" + hasSelectedRecipe +
                ",tray=" + tray +
                ",inputLock=" + inputLocked +
                ",cameraX=" + cameraX.ToString("0.###");
        }

        private void RefreshStatusBoard()
        {
            if (statusBoard == null)
            {
                return;
            }

            statusBoard.text = "CK01-C Manual Driver\n" +
                ReadState().Replace(",", "\n") +
                "\nrecent=" + (RecentAction ?? "none") +
                "\nsucceeded=" + RecentActionSucceeded;
        }
    }
}
