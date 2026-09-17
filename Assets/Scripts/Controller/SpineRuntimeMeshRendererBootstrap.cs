using UnityEngine;

/// <summary>
/// Enables a Spine-owned MeshRenderer after Unity finishes restoring the scene
/// for Play Mode. The renderer must remain disabled in the serialized scene so
/// Unity does not evaluate a stale editor-generated Spine mesh during restore.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public sealed class SpineRuntimeMeshRendererBootstrap : MonoBehaviour
{
    private MeshRenderer targetRenderer;

    private void Awake()
    {
        targetRenderer = GetComponent<MeshRenderer>();
    }

    private void Start()
    {
        if (targetRenderer != null)
            targetRenderer.enabled = true;
    }
}
