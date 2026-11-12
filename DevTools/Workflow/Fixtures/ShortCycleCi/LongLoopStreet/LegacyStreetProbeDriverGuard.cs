using System;
using System.Linq;
// Actual expression extracted from LongLoopAutomatedProbe.PreAI432.cs.txt; runner checks it against the readonly snapshot.
internal static class LegacyStreetProbeDriverGuard
{
    private class CinemachineExtension { public bool enabled=true; }
    private class CinemachineConfiner2D : CinemachineExtension {}
    private class CinemachineComponentBase { public bool enabled=true; }
    private class Binding { public CinemachineComponentBase Composer=new CinemachineComponentBase(); }
    private class DriverCamera
    {
        public CinemachineExtension[] extensions;
        public T[] GetComponents<T>()=>typeof(T)==typeof(CinemachineExtension)?extensions.Cast<T>().ToArray():Array.Empty<T>();
    }
    public static bool RejectsCurrentConfiner()
    {
        var f01=new CinemachineExtension();var channels=new CinemachineExtension();var binding=new Binding();
        var camera=new DriverCamera {extensions=new CinemachineExtension[]{f01,channels,new CinemachineConfiner2D()}};
        return camera.GetComponents<CinemachineExtension>().Any(e=>e.enabled&&e!=f01&&e!=channels)||
                    camera.GetComponents<CinemachineComponentBase>().Any(c=>c.enabled&&c!=binding.Composer);
    }
}