using UnityEngine;
using Cinemachine;

/// <summary>
/// 使 GameObject 跟随 CinemachineVirtualCamera 的位置，支持平滑插值。
/// Makes a GameObject follow a CinemachineVirtualCamera's position with optional lerp smoothing.
/// </summary>
public class CameraFollower : MonoBehaviour
{
    #region Serialized Fields
    [Header("相机跟随设置 / Camera Follow Settings")]
    [Tooltip("要跟随的 Cinemachine 虚拟相机 / The Cinemachine Virtual Camera to follow")]
    [SerializeField] private CinemachineVirtualCamera targetCamera;

    [Tooltip("是否启用位置跟随 / Enable position following")]
    [SerializeField] private bool enableFollow = true;

    [Tooltip("是否使用平滑插值跟随 / Use smooth lerp for following")]
    [SerializeField] private bool useLerp = false;

    [Tooltip("平滑跟随速度（仅在启用Lerp时生效）/ Lerp speed (only when useLerp is enabled)")]
    [SerializeField] [Range(0.1f, 20f)] private float lerpSpeed = 5f;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        ValidateCameraReference();
    }

    void LateUpdate()
    {
        UpdatePosition();
    }
    #endregion

    #region Core Logic
    private void UpdatePosition()
    {
        if (!ValidateCameraState()) return;

        Vector3 targetCameraPos = GetTargetFollowPosition();
        Vector3 followPos = ApplyFollowBehavior(transform.position, targetCameraPos);

        transform.position = followPos;
    }

    private bool ValidateCameraState()
    {
        if (targetCamera == null)
            return false;

        if (!targetCamera.gameObject.activeInHierarchy)
            return false;

        return true;
    }

    private Vector3 GetTargetFollowPosition()
    {
        if (targetCamera == null || !targetCamera.gameObject.activeInHierarchy)
            return transform.position;

        // 2D项目，保持当前Z轴位置 / 2D project, preserve current Z position
        Vector3 camPos = targetCamera.transform.position;
        return new Vector3(camPos.x, camPos.y, transform.position.z);
    }

    private Vector3 ApplyFollowBehavior(Vector3 currentPos, Vector3 targetPos)
    {
        if (!enableFollow)
            return currentPos;

        if (useLerp)
        {
            return Vector3.Lerp(currentPos, targetPos, lerpSpeed * Time.deltaTime);
        }
        else
        {
            return targetPos;
        }
    }

    private void ValidateCameraReference()
    {
        if (targetCamera == null)
        {
            Debug.LogError(
                $"[CameraFollower] targetCamera is not assigned on GameObject '{gameObject.name}'. " +
                "Component will not function. Please assign a CinemachineVirtualCamera.",
                this
            );
        }
    }
    #endregion
}
