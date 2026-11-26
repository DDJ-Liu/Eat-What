using Unity.Cinemachine;
using UnityEngine;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Eat What/Street/Street Parallax Layer")]
    public sealed class StreetParallaxLayer : MonoBehaviour
    {
        [SerializeField, Tooltip("横向层系数：1不补正，小于1为远层，大于1为前层。")]
        private float horizontalCoefficient = 1;
        [SerializeField, Tooltip("纵向层系数，独立于横向。")]
        private float verticalCoefficient = 1;
        [SerializeField, Tooltip("独立的世界空间视差原点，不应放在任何视差层内。")]
        private Transform parallaxOrigin;
        [SerializeField, Tooltip("输出街景相机的Brain；只消费该Brain更新后的最终输出相机位置。")]
        private CinemachineBrain cameraBrain;

        private bool captured;
        private Transform originalParent;
        private Vector3 originalLocal, originalWorld;
        private CinemachineCore.BrainEvent subscribedEvent;

        public float HorizontalCoefficient { get => horizontalCoefficient; set => horizontalCoefficient = value; }
        public float VerticalCoefficient { get => verticalCoefficient; set => verticalCoefficient = value; }
        public Transform ParallaxOrigin { get => parallaxOrigin; set => parallaxOrigin = value; }
        public CinemachineBrain CameraBrain { get => cameraBrain; set => cameraBrain = value; }

        private void OnEnable()
        {
            Stop();
            if (!Application.isPlaying || transform == null) return;
            originalParent = transform.parent;
            originalLocal = transform.localPosition; originalWorld = transform.position;
            if (!StreetFollowPipeline.Finite(originalWorld) || !StreetFollowPipeline.Finite(originalLocal)) return;
            captured = true;
            subscribedEvent = CinemachineCore.CameraUpdatedEvent;
            if (subscribedEvent != null) subscribedEvent.AddListener(OnCameraUpdated);
        }

        private void OnCameraUpdated(CinemachineBrain updated)
        {
            if (!Application.isPlaying) { Stop(); return; }
            if (!captured || !isActiveAndEnabled || updated == null || updated != cameraBrain) return;
            var output = updated.OutputCamera;
            if (parallaxOrigin == null || output == null || !updated.isActiveAndEnabled ||
                parallaxOrigin.IsChildOf(transform) || output.transform.IsChildOf(transform))
            { RestorePosition(); return; }
            var result = StreetParallax.Evaluate(output.transform.position, parallaxOrigin.position,
                new Vector2(horizontalCoefficient, verticalCoefficient));
            var target = originalWorld + result.Correction;
            if (result.Valid && StreetFollowPipeline.Finite(target)) transform.position = target;
            else RestorePosition();
        }

        private void RestorePosition()
        {
            if (!captured || transform == null) return;
            // Restore authored local coordinates exactly when the parent is unchanged.
            if (transform.parent == originalParent) transform.localPosition = originalLocal;
            else transform.position = originalWorld;
        }
        private void Stop()
        {
            if (subscribedEvent != null) subscribedEvent.RemoveListener(OnCameraUpdated);
            subscribedEvent = null;
            RestorePosition(); captured = false; originalParent = null;
        }
        private void OnDisable() => Stop();
        private void OnDestroy() => Stop();
        private void OnApplicationQuit() => Stop();
    }
}
