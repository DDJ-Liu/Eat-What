using UnityEngine;

/// <summary>
/// Scene-instance bridge from a physical tool or fixed facility to CK01 Tools.
/// Attach to the actual object; ToolData, ContainerTag and object names are not IDs.
/// </summary>
public sealed class SceneToolIdentity : MonoBehaviour
{
    [Tooltip("Exact Tools.tool_id; required for this instance to grant tool availability.")]
    public string toolId = string.Empty;
    [Tooltip("Explicit owning area. Must equal the nearest CookingRoomManager in the hierarchy.")]
    public CookingRoomManager areaOwner;
}
