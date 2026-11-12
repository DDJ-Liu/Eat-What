// Offline target-property boundaries; no native rendering or SortingGroup composition is simulated.
namespace UnityEngine
{
    public class Renderer
    {
        private int order;
        public int Writes;
        public int sortingLayerID;
        public bool enabled = true;
        public int sortingOrder { get => order; set { order=value; Writes++; } }
    }
}
namespace UnityEngine.Rendering
{
    public class SortingGroup
    {
        private int order;
        public int Writes;
        public int sortingLayerID;
        public bool enabled = true;
        public int sortingOrder { get => order; set { order=value; Writes++; } }
    }
}
