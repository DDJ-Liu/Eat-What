using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Debug-only Play Mode probe. Keep this component under DebugAndReferences
    /// and disabled by default; production presentation has no embedded tests.
    /// </summary>
    [AddComponentMenu("Cooking/Short Cycle/Debug/Automated Probe")]
    [DisallowMultipleComponent]
    public sealed class ShortCycleAutomatedProbe : MonoBehaviour
    {
        [Header("Explicit debug wiring")]
        [SerializeField] private ShortCycleSessionManager coordinator = null;
        [SerializeField] private ShortCycleActionRouter actionRouter = null;
        [SerializeField] private ShortCyclePresentationDirector presentationDirector = null;
        [SerializeField] private ShortCycleCameraPanController cameraPanController = null;
        [SerializeField] private ShortCycleInputLockController inputLockController = null;
        [SerializeField] private ShortCycleInteractionLayerStack interactionLayerStack = null;
        [SerializeField] private MouseInteractionLayer clueBoardLayer = null;
        [SerializeField] private Camera controlledCamera = null;
        [SerializeField] private Transform phase0CameraMarker = null;
        [SerializeField] private Transform phase1CameraMarker = null;
        [SerializeField] private Transform clueBoardShell = null;
        [SerializeField] private Transform clueBoardPhase0Anchor = null;
        [SerializeField] private Transform clueBoardPhase1Anchor = null;
        [SerializeField] private float presentationDuration = 0.45f;
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private string verificationRecipeId = "rcp_qingtang_noodle";

        public bool AutomatedProbeCompleted { get; private set; }
        public bool AutomatedProbeSucceeded { get; private set; }
        public string AutomatedProbeSummary { get; private set; }

        private void Reset()
        {
            enabled = false;
            runOnStart = false;
        }

        private void Start()
        {
            if (runOnStart)
            {
                RunProbe();
            }
        }

        public void RunProbe()
        {
            if (!enabled || AutomatedProbeCompleted)
            {
                return;
            }

            StartCoroutine(RunAutomatedProbe());
        }

        private IEnumerator RunAutomatedProbe()
        {
            var originalRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            var failures = new List<string>();

            yield return null;
            yield return WaitForInputUnlock(presentationDuration + 1f);
            if (presentationDirector == null)
            {
                failures.Add("presentation director is not connected");
            }
            if (inputLockController != null && inputLockController.Gate.IsLocked)
            {
                failures.Add("initial Phase0 camera pan did not release the input gate");
            }
            if (coordinator == null || coordinator.Context == null ||
                coordinator.Context.current_phase != ShortCyclePhase.Phase0)
            {
                failures.Add("default phase is not Phase0");
            }

            var catalog = Dispatch(ShortCycleActionNames.OpenCover);
            if (!catalog.Succeeded || coordinator == null || coordinator.Context == null ||
                coordinator.Context.phase0_view != ShortCyclePhase0View.RecipeCatalog ||
                coordinator.Context.HasSelectedRecipe)
            {
                failures.Add("cover -> catalog state transition failed: " + catalog.Error);
            }

            var selection = Dispatch(new ShortCycleActionRequest
            {
                ActionName = ShortCycleActionNames.SelectRecipe,
                recipe_id = verificationRecipeId
            });
            if (!selection.Succeeded || coordinator == null || coordinator.Context == null ||
                coordinator.Context.phase0_view != ShortCyclePhase0View.RecipeBrowse ||
                !coordinator.Context.HasSelectedRecipe)
            {
                failures.Add("catalog -> select -> browse sequence failed: " + selection.Error);
            }

            Dispatch(ShortCycleActionNames.OpenClueBoard);
            if (interactionLayerStack == null || !interactionLayerStack.IsTop(clueBoardLayer))
            {
                failures.Add("clue-board interaction layer was not pushed");
            }
            Dispatch(ShortCycleActionNames.CloseClueBoard);

            if (coordinator != null && coordinator.Context != null)
            {
                coordinator.Context.current_scroll_offset = 7.25f;
                coordinator.Context.current_filter_tag = "tag_ingredient_vegetable";
                coordinator.Context.prep_zone_contents.Add(new PrepZoneItem
                {
                    item_id = "ing_tomato",
                    quantity = 1,
                    drag_order = 0,
                    state = "probe"
                });
            }

            var start = Dispatch(ShortCycleActionNames.StartCooking);
            var lockedDuringStartPan = inputLockController != null && inputLockController.Gate.IsLocked;
            var blockedReturn = Dispatch(ShortCycleActionNames.Return);
            if (!start.Succeeded) failures.Add("start-cooking named action failed: " + start.Error);
            if (!lockedDuringStartPan) failures.Add("input gate was not locked during Phase0->Phase1 pan");
            if (blockedReturn.Succeeded) failures.Add("return action was not blocked during camera pan");

            yield return WaitForPanCompleted(ShortCycleCameraSlot.Phase1, presentationDuration + 1f);
            yield return WaitForTransformNear(clueBoardShell, clueBoardPhase1Anchor, presentationDuration + 1f);
            if (coordinator == null || coordinator.Context == null ||
                coordinator.Context.current_phase != ShortCyclePhase.Phase1)
                failures.Add("context did not reach Phase1");
            if (!Near(controlledCamera, phase1CameraMarker))
                failures.Add("camera did not reach the Phase1 marker");
            if (!Near(clueBoardShell, clueBoardPhase1Anchor))
                failures.Add("clue-board shell did not reach the Phase1 anchor");

            var back = Dispatch(ShortCycleActionNames.Return);
            var lockedDuringReturnPan = inputLockController != null && inputLockController.Gate.IsLocked;
            if (!back.Succeeded) failures.Add("return named action failed: " + back.Error);
            if (!lockedDuringReturnPan) failures.Add("input gate was not locked during Phase1->Phase0 pan");

            yield return WaitForPanCompleted(ShortCycleCameraSlot.Phase0, presentationDuration + 1f);
            yield return WaitForTransformNear(clueBoardShell, clueBoardPhase0Anchor, presentationDuration + 1f);
            if (coordinator == null || coordinator.Context == null ||
                coordinator.Context.current_phase != ShortCyclePhase.Phase0)
                failures.Add("context did not return to Phase0");
            if (!Near(controlledCamera, phase0CameraMarker))
                failures.Add("camera did not return to the Phase0 marker");
            if (!Near(clueBoardShell, clueBoardPhase0Anchor))
                failures.Add("clue-board shell did not return to the Phase0 anchor");
            if (coordinator != null && coordinator.Context != null &&
                (Math.Abs(coordinator.Context.current_scroll_offset - 7.25f) > 0.001f ||
                 coordinator.Context.current_filter_tag != "tag_ingredient_vegetable" ||
                 coordinator.Context.prep_zone_contents.Count != 1))
                failures.Add("Phase1 temporary state was not retained on return");

            var browseEscape = Dispatch(ShortCycleActionNames.Escape);
            var catalogEscape = Dispatch(ShortCycleActionNames.Escape);
            var coverEscape = Dispatch(ShortCycleActionNames.Escape);
            if (!browseEscape.Succeeded || !catalogEscape.Succeeded || !coverEscape.Succeeded)
                failures.Add("layered ESC sequence failed");
            if (coordinator != null && coordinator.Context != null &&
                (coordinator.Context.current_phase != ShortCyclePhase.None ||
                 !string.IsNullOrEmpty(coordinator.Context.current_recipe_id)))
                failures.Add("ESC did not clear ShortCycleContext");

            AutomatedProbeCompleted = true;
            AutomatedProbeSucceeded = failures.Count == 0;
            AutomatedProbeSummary = failures.Count == 0
                ? "cover/catalog/select/browse; named start/return; bidirectional camera pan; input lock; clue anchors; P1 retention; layered ESC clear; generated recipe chain"
                : string.Join("; ", failures.ToArray());
            Application.runInBackground = originalRunInBackground;

            if (AutomatedProbeSucceeded)
            {
                Debug.Log("CK01-C-PROBE PASS " + AutomatedProbeSummary, this);
            }
            else
            {
                Debug.LogWarning("CK01-C-PROBE NEEDS_REVIEW " + AutomatedProbeSummary, this);
            }
        }

        private IEnumerator WaitForInputUnlock(float timeoutSeconds)
        {
            var startedAt = Time.unscaledTime;
            while (inputLockController != null && inputLockController.Gate.IsLocked &&
                   Time.unscaledTime - startedAt < timeoutSeconds)
            {
                yield return null;
            }
            yield return null;
        }

        private IEnumerator WaitForPanCompleted(ShortCycleCameraSlot targetSlot, float timeoutSeconds)
        {
            if (cameraPanController == null || cameraPanController.CurrentSlot == targetSlot) yield break;
            var completed = false;
            Action<ShortCycleCameraSlot> handler = slot => completed = slot == targetSlot;
            cameraPanController.PanCompleted += handler;
            var startedAt = Time.unscaledTime;
            while (!completed && cameraPanController.CurrentSlot != targetSlot &&
                   Time.unscaledTime - startedAt < timeoutSeconds)
                yield return null;
            cameraPanController.PanCompleted -= handler;
        }

        private ShortCycleActionResult Dispatch(string actionName)
        {
            return Dispatch(new ShortCycleActionRequest { ActionName = actionName });
        }

        private ShortCycleActionResult Dispatch(ShortCycleActionRequest request)
        {
            return actionRouter == null
                ? ShortCycleActionResult.Failure("action router missing")
                : actionRouter.DispatchAction(request);
        }

        private static IEnumerator WaitForTransformNear(Transform value, Transform marker, float timeoutSeconds)
        {
            var startedAt = Time.unscaledTime;
            while (!Near(value, marker) && Time.unscaledTime - startedAt < timeoutSeconds)
            {
                yield return null;
            }
        }

        private static bool Near(Camera camera, Transform marker)
        {
            return camera != null && marker != null &&
                Vector3.Distance(camera.transform.position, marker.position) < 0.02f;
        }

        private static bool Near(Transform value, Transform marker)
        {
            return value != null && marker != null &&
                Vector3.Distance(value.position, marker.position) < 0.02f;
        }
    }
}
