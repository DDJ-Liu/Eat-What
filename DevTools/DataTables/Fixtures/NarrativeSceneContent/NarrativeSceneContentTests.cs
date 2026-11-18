using System;
using System.Collections.Generic;
using System.IO;
using EatWhat.DataTables;

public static class NarrativeSceneContentTests
{
    static readonly string Actor = "4da9856f2e554aed9155c3c6b086640e", Anchor = "93cf2eb1d8644a628af58e5fd46ea8bb";
    static readonly string Prefab = "18482e8563931f945979fc4facbec5b8";
    static int count;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); count++; }
    static string Text(string root, string name) { return File.ReadAllText(Path.Combine(root, name)); }
    static NarrativeSceneContentAudit.Inventory Read(string text, string prefab)
    { return NarrativeSceneContentAudit.ReadInventory(text, Actor, Anchor, guid => guid == Prefab ? prefab : null); }
    static IList<string> Warnings(NarrativeSceneContentAudit.Inventory scene, params NarrativeSceneContentAudit.Placement[] rows)
    { return NarrativeSceneContentAudit.AuditState(scene, "scn_audit", "day", rows); }
    static NarrativeSceneContentAudit.Placement Row(string id, string anchor = "")
    { return new NarrativeSceneContentAudit.Placement("actor", id, anchor); }
    static bool Has(IList<string> warnings, string code)
    { foreach (string text in warnings) if (text.Contains("[" + code + "]")) return true; return false; }
    static void Unknown(NarrativeSceneContentAudit.Inventory scene, string name)
    { var warnings = Warnings(scene, Row("chr_missing")); Check(!scene.Complete && warnings.Count > 0 && Has(warnings, "UNVERIFIED") && !Has(warnings, "NO_PREPLACED"), name); }
    public static int Run(string root, string project)
    {
        count = 0;
        string clean = Text(root,"clean.scene.txt"), dirty = Text(root,"dirty.scene.txt"), prefab = Text(root,"generic.prefab.txt"), instance = Text(root,"override.scene.txt");
        var scene = Read(clean,prefab); Check(scene.Complete, "direct inventory");
        Check(scene.Actors.Contains("chr_vendor#2") && scene.Anchors.Contains("anc_gate"), "full IDs and real GUIDs");
        Check(Warnings(scene,Row("chr_vendor#2")).Count == 0, "clean fixture");
        var warnings = Warnings(Read(dirty,prefab),Row("chr_missing","anc_absent"),Row("chr_unplaced"));
        Check(warnings.Count == 3, "exactly original three dirty warnings");
        foreach (string code in new[]{"MISSING_ANCHOR","NO_PREPLACED","UNLISTED_ACTOR"}) Check(Has(warnings,code),code);
        Check(Warnings(scene,Row("chr_vendor#2"),new NarrativeSceneContentAudit.Placement("object","obj_a","absent"),new NarrativeSceneContentAudit.Placement("exit","exit_a","absent")).Count == 0, "object and exit excluded");
        Check(Has(Warnings(scene,Row("chr_vendor")),"NO_PREPLACED"), "suffix not base ID");
        Check(Has(Warnings(scene,Row("CHR_vendor#2")),"NO_PREPLACED"), "ordinal identity");
        Check(Warnings(Read(clean.Replace("characterId: chr_vendor#2","characterId: 'chr_vendor#2'"),prefab),Row("chr_vendor#2")).Count == 0,"single quoted scalar");
        Check(Warnings(Read(clean.Replace("characterId: chr_vendor#2","characterId: \"chr_vendor#2\""),prefab),Row("chr_vendor#2")).Count == 0,"double quoted scalar");
        Check(Warnings(Read(clean.Replace("m_Enabled: 1","m_Enabled: 0"),prefab)).Count == 0,"disabled markers excluded");
        scene = Read(instance,prefab); Check(scene.Complete && scene.Actors.Contains("chr_vendor#2"),"inherited marker exact override");
        Check(Warnings(scene,Row("chr_vendor#2")).Count == 0,"prefab clean");
        Unknown(Read(instance.Replace("9007199254740993123","9007199254740993124"),prefab),"adjacent localID above 2^53 does not alias");
        Unknown(Read(instance.Replace("guid: "+Prefab+", type: 3}\n      propertyPath: characterId","guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa, type: 3}\n      propertyPath: characterId"),prefab),"wrong override GUID");
        Unknown(Read(instance.Replace("propertyPath: characterId","propertyPath: m_Name"),prefab),"never infer ID from object name");
        Unknown(Read(instance,null),"missing prefab");
        Unknown(Read(instance,prefab.Replace("characterId: ","characterId: [bad]")),"malformed prefab scalar");
        Unknown(Read(instance,instance),"nested prefab unsupported");
        Unknown(Read(instance.Replace("m_RemovedComponents: []","m_RemovedComponents:\n    - {fileID: 9007199254740993123}"),prefab),"removed component unsupported");
        Unknown(Read("",prefab),"empty scene"); Unknown(Read("bad",prefab),"bad scene");
        Unknown(Read(clean.Replace("MonoBehaviour:","wrong"),prefab),"malformed component");
        Unknown(Read(clean.Replace("characterId: chr_vendor#2","characterId:"),prefab),"empty direct ID");
        Unknown(NarrativeSceneContentAudit.ReadInventory(clean,"",Anchor,null),"unknown marker metadata");
        // Two instances of the same source have independent overrides.
        string second = instance.Substring(instance.IndexOf("--- !u!",StringComparison.Ordinal)).Replace("&1001","&1002").Replace("chr_vendor#2","chr_vendor#3");
        scene = Read(instance + second,prefab); Check(scene.Complete && scene.Actors.Count == 2,"two instances independent");
        Check(Warnings(scene,Row("chr_vendor#2"),Row("chr_vendor#3")).Count == 0,"two exact instance rows");
        string stripped = "--- !u!114 &2001 stripped\nMonoBehaviour:\n  m_CorrespondingSourceObject: {fileID: 9007199254740993123, guid: "+Prefab+", type: 3}\n  m_PrefabInstance: {fileID: 1001}\n";
        scene = Read(instance+stripped,prefab); Check(scene.Complete && scene.Actors.Count == 1,"stripped inherited component not double counted");
        // Read actual Street and GenericNPC; the current empty inherited ID must remain unverified.
        string actualPrefab = File.ReadAllText(Path.Combine(project,"Assets/Prefabs/LongLoop/NPC/GenericNPC.prefab"));
        string actualScene = File.ReadAllText(Path.Combine(project,"Assets/Scenes/LongLoop/LongLoop_Street.unity"));
        scene = Read(actualScene,actualPrefab);
        bool emptyId = false; foreach(string issue in scene.Issues) if(issue.Contains("Empty/invalid characterId")) emptyId = true;
        Check(!scene.Complete && emptyId,"actual current representation reaches inherited empty ID");
        // In-memory only: reproduce the exact future scene override against the actual accepted source localID.
        string actualOverride = instance.Replace("9007199254740993123","4850488659401005512");
        scene = Read(actualOverride,actualPrefab);
        Check(scene.Complete && Warnings(scene,Row("chr_vendor#2")).Count == 0,"actual accepted prefab source localID override");
        return count;
    }
}
