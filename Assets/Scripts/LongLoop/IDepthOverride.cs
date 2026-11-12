using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Future region policy: true explicitly enables the returned per-world-unit coefficient.</summary>
    public interface IDepthOverride
    {
        bool TryGetDepthScaleFactor(Vector3 footWorldPosition, out float depthScaleFactor);
    }
}
