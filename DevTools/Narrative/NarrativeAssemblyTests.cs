using System;
using EatWhat.Narrative.Core;
using EatWhat.Narrative.Unity;

internal static class NarrativeAssemblyTests
{
    private static int Main()
    {
        var core = typeof(NarrativeCoreMarker).Assembly;
        var unity = typeof(NarrativeUnityMarker).Assembly;
        if (core.GetName().Name != "Narrative.Core") throw new Exception("Core assembly identity");
        if (unity.GetName().Name != "Narrative.Unity") throw new Exception("Unity assembly identity");
        foreach (var reference in core.GetReferencedAssemblies())
            if (reference.Name != "mscorlib") throw new Exception("Core must remain BCL only: " + reference.FullName);
        Console.WriteLine("PASS narrative assembly example (3 assertions)");
        Console.WriteLine("CORE_REFERENCES=" + string.Join(",", Array.ConvertAll(core.GetReferencedAssemblies(), x => x.Name)));
        return 0;
    }
}
