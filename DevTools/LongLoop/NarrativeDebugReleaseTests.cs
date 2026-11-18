using System;
using System.IO;
using System.Reflection;
internal static class NarrativeDebugReleaseTests
{
    public static int Main(string[] args)
    {
        bool included=args[0]!="Release";string own=Path.GetDirectoryName(args[1]),shared=args[3],scripts=args[4],engine=args[5];
        AppDomain.CurrentDomain.AssemblyResolve+=(sender,eventArgs)=>{var name=new AssemblyName(eventArgs.Name).Name+".dll";foreach(var dir in new[]{own,shared,scripts,engine}){var path=Path.Combine(dir,name);if(File.Exists(path))return Assembly.LoadFrom(path);}return null;};
        var core=Assembly.LoadFrom(args[2]);var loop=Assembly.LoadFrom(args[1]);int checks=0;
        Action<bool,string> check=(value,label)=>{checks++;if(!value)throw new Exception(label);};
        check((loop.GetType("EatWhat.LongLoop.NarrativeSceneDebugPanel")!=null)==included,"panel type inclusion");
        check((loop.GetType("EatWhat.LongLoop.NarrativeAmbientBubbleTrigger").GetMethod("DebugForceBubble")!=null)==included,"trigger debug entry inclusion");
        check((loop.GetType("EatWhat.LongLoop.NarrativeAmbientBubbleDriver").GetMethod("DebugForce")!=null)==included,"driver debug entry inclusion");
        check((core.GetType("EatWhat.Narrative.Core.NarrativeAmbientBubbleModel").GetMethod("DebugForce")!=null)==included,"model debug entry inclusion");
        check(loop.GetType("EatWhat.LongLoop.NarrativeSceneStateApplier").GetMethod("ApplyState")!=null,"normal state API retained");
        check(core.GetType("EatWhat.Narrative.Core.NarrativeAmbientBubbleModel").GetMethod("TryTrigger")!=null,"normal bubble API retained");
        Console.WriteLine("DEBUG_COMPILE_PASS mode="+args[0]+" checks="+checks);return 0;
    }
}
