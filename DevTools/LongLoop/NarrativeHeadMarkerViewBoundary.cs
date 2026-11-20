// External render call handle only. No marker policy, timer, color or positioning algorithm.
namespace UnityEngine
{
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
    }
}
namespace EatWhat.Narrative.Unity
{
    public sealed class NarrativeHeadMarkerView:UnityEngine.MonoBehaviour
    {
        public bool Hidden=true;public int Calls,Hides;public string Glyph;public UnityEngine.Color Tint;
        public float Alpha,Bob;public UnityEngine.Transform Anchor;
        public bool Apply(UnityEngine.Transform anchor,string glyph,UnityEngine.Color tint,float alpha,float bob)
        {Calls++;Anchor=anchor;Glyph=glyph;Tint=tint;Alpha=alpha;Bob=bob;Hidden=false;return true;}
        public void HideImmediate(){Hides++;Hidden=true;Alpha=0;}
    }
}
