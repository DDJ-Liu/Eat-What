// External stage call/value and clipboard transport only. No framing, rendering or tuning implementation.
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine { public static class GUIUtility { public static string systemCopyBuffer; } }
namespace EatWhat.Narrative.Unity
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public sealed class NarrativePortraitStage : MonoBehaviour
    {
        public readonly List<KeyValuePair<string, Vector3>> Rows = new List<KeyValuePair<string, Vector3>>();
        public readonly List<KeyValuePair<string, Vector3>> Sets = new List<KeyValuePair<string, Vector3>>();
        public int Clears;
        public IReadOnlyList<KeyValuePair<string, Vector3>> GetVisibleFramings() { return Rows.AsReadOnly(); }
        public void SetTemporaryFraming(string id, float x, float y, float scale) { Sets.Add(new KeyValuePair<string, Vector3>(id, new Vector3(x, y, scale))); }
        public void ClearAllTemporaryFraming() { Clears++; }
    }
#endif
}
