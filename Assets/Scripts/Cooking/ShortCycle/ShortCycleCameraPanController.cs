using System;
using System.Collections;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public enum ShortCycleCameraSlot
    {
        Phase0,
        Phase1,
        RightReserved
    }

    /// <summary>
    /// New deterministic same-scene camera pan. It reads new scene marker
    /// transforms and never switches or reuses legacy Cinemachine vcams.
    /// </summary>
    public sealed class ShortCycleCameraPanController : MonoBehaviour
    {
        [SerializeField] private Camera controlledCamera = null;
        [SerializeField] private Transform phase0Marker = null;
        [SerializeField] private Transform phase1Marker = null;
        [SerializeField] private Transform rightReservedMarker = null;
        [SerializeField] private ShortCycleInputLockController inputLockController = null;
        [SerializeField] private AnimationCurve panCurve = null;
        [SerializeField] private float panDuration = 0.45f;
        [SerializeField] private ShortCycleCameraSlot initialSlot = ShortCycleCameraSlot.Phase0;

        private readonly ShortCycleInputGate fallbackInputGate = new ShortCycleInputGate();
        private Coroutine activePan;
        private IDisposable activeInputLock;

        public event Action<ShortCycleCameraSlot, ShortCycleCameraSlot> PanStarted;
        public event Action<ShortCycleCameraSlot> PanCompleted;

        public ShortCycleCameraSlot CurrentSlot { get; private set; }
        public float CameraX
        {
            get { return controlledCamera == null ? 0f : controlledCamera.transform.position.x; }
        }

        private void Awake()
        {
            if (!HasUsableCurve(panCurve))
            {
                panCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }
            CurrentSlot = ResolveCurrentSlot(initialSlot);
        }

        public bool RegisterMarker(ShortCycleCameraSlot slot, Transform marker)
        {
            if (marker == null)
            {
                return false;
            }

            switch (slot)
            {
                case ShortCycleCameraSlot.Phase0:
                    phase0Marker = marker;
                    return true;
                case ShortCycleCameraSlot.Phase1:
                    phase1Marker = marker;
                    return true;
                case ShortCycleCameraSlot.RightReserved:
                    rightReservedMarker = marker;
                    return true;
                default:
                    return false;
            }
        }

        public bool TryGetMarkerPosition(ShortCycleCameraSlot slot, out Vector3 position)
        {
            var marker = GetMarker(slot);
            if (marker == null)
            {
                position = default(Vector3);
                return false;
            }

            position = marker.position;
            return true;
        }

        public bool PanTo(ShortCycleCameraSlot slot, Action onComplete = null)
        {
            Vector3 destination;
            if (controlledCamera == null || !TryGetMarkerPosition(slot, out destination))
            {
                return false;
            }

            if (activePan != null)
            {
                StopActivePan();
            }

            if (slot == CurrentSlot && IsAtMarker(controlledCamera.transform.position, destination))
            {
                if (onComplete != null) onComplete();
                return true;
            }

            var fromSlot = CurrentSlot;
            var started = PanStarted;
            if (started != null)
            {
                started(fromSlot, slot);
            }

            activePan = StartCoroutine(PanRoutine(slot, destination, onComplete));
            return true;
        }

        private IEnumerator PanRoutine(ShortCycleCameraSlot targetSlot, Vector3 destination, Action onComplete)
        {
            var inputGate = inputLockController == null ? fallbackInputGate : inputLockController.Gate;
            activeInputLock = inputGate.Acquire("ShortCycle camera pan");
            try
            {
                var start = controlledCamera.transform.position;
                var elapsed = 0f;
                var duration = IsUsableDuration(panDuration) ? panDuration : 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var progress = Math.Min(1f, elapsed / duration);
                    var curveValue = EvaluatePanProgress(panCurve, progress);
                    controlledCamera.transform.position = Vector3.LerpUnclamped(start, destination, curveValue);
                    yield return null;
                }

                controlledCamera.transform.position = destination;
                CurrentSlot = targetSlot;
            }
            finally
            {
                ReleaseInputLock();
                activePan = null;
            }

            var completed = PanCompleted;
            if (completed != null)
            {
                completed(targetSlot);
            }

            if (onComplete != null)
            {
                onComplete();
            }
        }

        private static bool HasUsableCurve(AnimationCurve curve)
        {
            return curve != null && curve.length > 0;
        }

        private static float EvaluatePanProgress(AnimationCurve curve, float progress)
        {
            return HasUsableCurve(curve) ? curve.Evaluate(progress) : progress;
        }

        private static bool IsUsableDuration(float duration)
        {
            return duration > 0f && !float.IsNaN(duration) && !float.IsInfinity(duration);
        }

        private static bool IsAtMarker(Vector3 position, Vector3 markerPosition)
        {
            return Vector3.Distance(position, markerPosition) <= 0.0001f;
        }

        private void OnDisable()
        {
            StopActivePan();
        }

        private void StopActivePan()
        {
            if (activePan != null)
            {
                StopCoroutine(activePan);
                activePan = null;
            }

            ReleaseInputLock();
        }

        private void ReleaseInputLock()
        {
            if (activeInputLock == null)
            {
                return;
            }

            activeInputLock.Dispose();
            activeInputLock = null;
        }

        private Transform GetMarker(ShortCycleCameraSlot slot)
        {
            switch (slot)
            {
                case ShortCycleCameraSlot.Phase0:
                    return phase0Marker;
                case ShortCycleCameraSlot.Phase1:
                    return phase1Marker;
                case ShortCycleCameraSlot.RightReserved:
                    return rightReservedMarker;
                default:
                    return null;
            }
        }

        private ShortCycleCameraSlot ResolveCurrentSlot(ShortCycleCameraSlot fallback)
        {
            if (controlledCamera == null)
            {
                return fallback;
            }

            var cameraPosition = controlledCamera.transform.position;
            var bestSlot = fallback;
            var bestDistance = float.MaxValue;
            EvaluateMarkerDistance(ShortCycleCameraSlot.Phase0, phase0Marker, cameraPosition, ref bestSlot, ref bestDistance);
            EvaluateMarkerDistance(ShortCycleCameraSlot.Phase1, phase1Marker, cameraPosition, ref bestSlot, ref bestDistance);
            EvaluateMarkerDistance(ShortCycleCameraSlot.RightReserved, rightReservedMarker, cameraPosition, ref bestSlot, ref bestDistance);
            return bestSlot;
        }

        private static void EvaluateMarkerDistance(
            ShortCycleCameraSlot slot,
            Transform marker,
            Vector3 cameraPosition,
            ref ShortCycleCameraSlot bestSlot,
            ref float bestDistance)
        {
            if (marker == null)
            {
                return;
            }

            var distance = Vector3.Distance(cameraPosition, marker.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestSlot = slot;
            }
        }
    }
}
