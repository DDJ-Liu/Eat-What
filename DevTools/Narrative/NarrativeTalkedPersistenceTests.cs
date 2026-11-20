using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EatWhat.Tools.Security;
using EatWhat.Tools.Persistence;
using EatWhat.Narrative.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class NarrativeTalkedPersistenceTests
{
#if UNITY_EDITOR
    const string Mode="UNITY_EDITOR";const bool Plain=true;
#elif DEVELOPMENT_BUILD
    const string Mode="DEVELOPMENT_BUILD";const bool Plain=true;
#else
    const string Mode="release";const bool Plain=false;
#endif
    // Deterministic isolated test credential; never reads or prints a production key or player save.
    sealed class Keys : IKeyProvider { public byte[] GetKey(){var key=new byte[32];for(int i=0;i<key.Length;i++)key[i]=(byte)(i+1);return key;} }
    static readonly List<string> details=new List<string>();
    static readonly DateTime stamp=new DateTime(2026,11,19,0,0,0,DateTimeKind.Utc);
    static readonly Keys keys=new Keys();static string root;static int maximumPath;
    static void Check(bool value,string name){if(!value)throw new Exception(Mode+": "+name);details.Add(name);}
    static string Case(string name){string path=Path.Combine(root,name);maximumPath=Math.Max(maximumPath,(Path.Combine(path,"long_loop.json")+".tmp-"+new string('a',32)).Length);return path;}
    static string FilePath(string dir){return Path.Combine(dir,NarrativeTalkedPersistence.Slot+".json");}
    static ISaveCodec Codec(){return SaveCodecFactory.CreateForCurrentBuild(keys);}
    static NarrativeTalkedPersistence Host(string dir,List<string> warnings){return NarrativeTalkedPersistence.CreateForCurrentBuild(dir,keys,warnings.Add,()=>stamp);}
    static SaveEnvelope Envelope(string dir)
    {
        string json,issue;Check(Codec().TryDecode(File.ReadAllBytes(FilePath(dir)),out json,out issue),"actual build codec decodes persisted file");
        var envelope=SaveEnvelope.Parse(json);Check(new EnvelopeAuthenticator(keys).Verify(envelope),"persisted envelope has actual valid HMAC");return envelope;
    }
    static byte[] Seed(string dir,JObject payload,string schema=null,int? format=null,bool badHmac=false)
    {
        Directory.CreateDirectory(dir);var envelope=SaveEnvelope.Create(payload,stamp);
        if(schema!=null||format.HasValue)envelope=new SaveEnvelope(format??SaveEnvelope.CurrentFormatVersion,schema??SaveEnvelope.CurrentSchemaVersion,envelope.CreatedUtc,payload,"");
        envelope=new EnvelopeAuthenticator(keys).Sign(envelope);if(badHmac)envelope=envelope.WithHmac(Convert.ToBase64String(new byte[32]));
        byte[] bytes=Codec().Encode(envelope.ToJson());File.WriteAllBytes(FilePath(dir),bytes);return bytes;
    }
    static bool Same(byte[] a,byte[] b){return Convert.ToBase64String(a)==Convert.ToBase64String(b);}
    static NarrativeTalkedPersistence Cold(string dir)
    {var warnings=new List<string>();var value=Host(dir,warnings);Check(value.InitializeWhenAvailable(true)&&warnings.Count==0,"cold instance reads valid file without warning");return value;}
    static void Rejected(string dir,byte[] original,string name)
    {
        var warnings=new List<string>();var value=Host(dir,warnings);
        Check(value.InitializeWhenAvailable(true)&&value.Count==0&&warnings.Count==1,name+": starts empty with one diagnostic and remains usable");
        Check(value.IsWriteBlocked,name+": writes blocked");value.InitializeWhenAvailable(true);value.InitializeWhenAvailable(false);
        Check(warnings.Count==1,name+": initialize does not reread or repeat diagnostic");
        value.MarkTalked("memory_only");Check(value.HasTalked("memory_only")&&value.LastSaveSucceeded==false,name+": blocked persistence does not block memory record");
        value.ClearAll();Check(value.Count==0&&warnings.Count==1,name+": clearing does not repeat load diagnostic");
        Check(Same(original,File.ReadAllBytes(FilePath(dir))),name+": exact original bytes preserved");
    }
    static int Main(string[] args)
    {
        try
        {
            if(args.Length!=1||!Path.IsPathRooted(args[0])||Directory.Exists(args[0]))throw new ArgumentException("Fresh absolute isolated test root required.");
            root=args[0];Check((Codec() is PlainJsonCodec)==Plain&&(Codec() is EncryptedJsonCodec)!=Plain,"factory selects actual compile-time codec branch "+Mode);
            var unavailableDir=Case("unavailable");var warnings=new List<string>();var notReady=Host(unavailableDir,warnings);
            Check(!notReady.InitializeWhenAvailable(false)&&!notReady.IsInitialized&&warnings.Count==0&&!Directory.Exists(unavailableDir),"unavailable runtime does no IO or diagnostic");
            bool rejected=false;try{notReady.MarkTalked("too_early");}catch(InvalidOperationException){rejected=true;}
            Check(rejected&&!Directory.Exists(unavailableDir),"mutation before runtime initialization refused without touching files");
            Check(notReady.InitializeWhenAvailable(true)&&notReady.Count==0&&warnings.Count==1&&!Directory.Exists(unavailableDir),"missing file loads empty with one warning and creates no directory");
            notReady.InitializeWhenAvailable(true);Check(warnings.Count==1,"missing diagnostic only once per owner");
            notReady.MarkTalked("dlg_甲");Check(notReady.LastSaveSucceeded==true&&File.Exists(FilePath(unavailableDir)),"mark immediately creates the independent real file");
            var saved=Envelope(unavailableDir);Check(saved.SchemaVersion==SaveEnvelope.CurrentSchemaVersion&&saved.FormatVersion==SaveEnvelope.CurrentFormatVersion,"writes use store's own unchanged version constants");
            Check(saved.Payload.Count==1&&saved.Payload[NarrativeTalkedPersistence.Section] is JArray,"new envelope creates only dialogue_talked section");
            byte[] raw=File.ReadAllBytes(FilePath(unavailableDir));string readable=Encoding.UTF8.GetString(raw);Check((readable.StartsWith("{",StringComparison.Ordinal))==Plain,"on-disk file matches plain versus encrypted branch");
            Check(Cold(unavailableDir).HasTalked("dlg_甲"),"cold restart restores Unicode ID from real file");
            notReady.MarkTalked("dlg_甲");notReady.MarkTalked("dlg_b");Check(Cold(unavailableDir).Count==2,"duplicate mark remains one ID on disk");
            Check(notReady.Clear(new[]{"dlg_甲","absent"})==1&&Cold(unavailableDir).HasTalked("dlg_b")&&!Cold(unavailableDir).HasTalked("dlg_甲"),"specified clear is immediately persisted");
            notReady.ClearAll();Check(Cold(unavailableDir).Count==0,"clear all immediately persists empty list");
            notReady.Restore(new[]{"dlg_z","dlg_y","dlg_z"});var restored=Cold(unavailableDir).Export();Check(restored.Count==2&&restored[0]=="dlg_y"&&restored[1]=="dlg_z","restore writes unique deterministic list and roundtrips");
            var detached=notReady.Export();detached.Clear();Check(notReady.Count==2&&Cold(unavailableDir).Count==2,"export list cannot silently mutate owner or persisted file");
            var snapshots=Directory.GetFiles(unavailableDir,"long_loop.json.backup-*");Check(snapshots.Length>0,"real atomic replacements retain existing store backups");

            var absent=Case("missing-section");Seed(absent,new JObject{["future_section"]=new JObject{["keep"]="original"}});warnings=new List<string>();var missing=Host(absent,warnings);
            Check(missing.InitializeWhenAvailable(true)&&missing.Count==0&&warnings.Count==0&&!missing.IsWriteBlocked,"valid older file missing new section is empty without warning");
            missing.MarkTalked("new_id");var withNew=Envelope(absent);Check(withNew.Payload["future_section"]["keep"].Value<string>()=="original"&&withNew.Payload.Count==2,"adding this section preserves unrelated existing section");
            Check(withNew.Payload["scene_state"]==null&&withNew.Payload["story_flags"]==null,"adapter never creates scene-state or story-flag sections");

            var corrupt=Case("corrupt");Directory.CreateDirectory(corrupt);byte[] invalid=Encoding.UTF8.GetBytes("invalid-save");File.WriteAllBytes(FilePath(corrupt),invalid);Rejected(corrupt,invalid,"corrupt file");
            var mismatch=Case("schema");string differentSchema=SaveEnvelope.CurrentSchemaVersion+"-incompatible-test";
            Check(differentSchema!=SaveEnvelope.CurrentSchemaVersion,"wrong-version fixture derived from current source constant");
            Rejected(mismatch,Seed(mismatch,new JObject{[NarrativeTalkedPersistence.Section]=new JArray("old")},differentSchema),"schema mismatch");
            var format=Case("format");Rejected(format,Seed(format,new JObject(),format:SaveEnvelope.CurrentFormatVersion+1),"format mismatch");
            var signature=Case("hmac");Rejected(signature,Seed(signature,new JObject(),badHmac:true),"envelope HMAC mismatch");
            int index=0;foreach(JToken bad in new JToken[]{new JValue("wrong"),new JArray("good",1),new JArray("good"," "),JValue.CreateNull()})
            {var dir=Case("section-"+index);Rejected(dir,Seed(dir,new JObject{[NarrativeTalkedPersistence.Section]=bad}),"malformed section "+index);index++;}
            if(!Plain)
            {var dir=Case("cipher-auth");var bytes=Seed(dir,new JObject());bytes[bytes.Length/2]^=1;File.WriteAllBytes(FilePath(dir),bytes);Rejected(dir,bytes,"cipher authentication failure");}

            var once=Case("once-and-drift");Seed(once,new JObject{[NarrativeTalkedPersistence.Section]=new JArray("loaded")});warnings=new List<string>();var current=Host(once,warnings);current.InitializeWhenAvailable(true);
            byte[] replacement=Seed(once,new JObject{[NarrativeTalkedPersistence.Section]=new JArray("external")});current.InitializeWhenAvailable(true);
            Check(current.HasTalked("loaded")&&!current.HasTalked("external")&&warnings.Count==0,"available-again does not reload over live record");
            current.MarkTalked("local");Check(current.HasTalked("local")&&current.IsWriteBlocked&&warnings.Count==1,"concurrent drift blocks write with one warning but retains memory change");
            current.MarkTalked("local2");current.Clear(new[]{"local"});Check(warnings.Count==1&&Same(replacement,File.ReadAllBytes(FilePath(once))),"subsequent writes preserve external bytes without warning spam");

            var collision=Case("new-external-file");warnings=new List<string>();var first=Host(collision,warnings);first.InitializeWhenAvailable(true);byte[] external=Seed(collision,new JObject{[NarrativeTalkedPersistence.Section]=new JArray("external")});
            first.MarkTalked("local");Check(first.IsWriteBlocked&&warnings.Count==2&&Same(external,File.ReadAllBytes(FilePath(collision))),"file appearing after missing read is protected by store observation");
            var targetDir=Case("write-fault");warnings=new List<string>();var fault=Host(targetDir,warnings);fault.InitializeWhenAvailable(true);Directory.CreateDirectory(FilePath(targetDir));fault.MarkTalked("local");
            Check(fault.IsWriteBlocked&&fault.HasTalked("local")&&warnings.Count==2&&Directory.Exists(FilePath(targetDir)),"write fault does not remove target directory or block memory dialogue");
            fault.ClearAll();Check(warnings.Count==2,"later blocked write does not repeat write diagnostic");

            var separate=Case("short-cycle");Directory.CreateDirectory(separate);string shortFile=Path.Combine(separate,"default.json");byte[] sentinel=Encoding.UTF8.GetBytes("SHORT-CYCLE-UNTOUCHED");File.WriteAllBytes(shortFile,sentinel);warnings=new List<string>();
            using(var exclusive=new FileStream(shortFile,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            {
                var isolated=Host(separate,warnings);isolated.InitializeWhenAvailable(true);isolated.MarkTalked("talked");isolated.Clear(new[]{"talked"});isolated.MarkTalked("again");isolated.ClearAll();
                Check(warnings.Count==1&&isolated.LastSaveSucceeded==true&&Cold(separate).Count==0,"all persistence operations succeed while short-cycle file is exclusively locked");
            }
            Check(Same(sentinel,File.ReadAllBytes(shortFile)),"short-cycle default file bytes unchanged");
            Check(maximumPath<240,"isolated staging paths stay below diagnosed Windows Mono boundary");
            Console.WriteLine("TALKED_PERSISTENCE_RESULT "+JsonConvert.SerializeObject(new{passed=true,mode=Mode,codec=Codec().GetType().Name,checks=details.Count,actualFiles=true,actualFactory=true,maximumStagingPath=maximumPath,root=root,details=details}));return 0;
        }
        catch(Exception error){Console.Error.WriteLine("TALKED_PERSISTENCE_FAILED "+error);return 1;}
    }
}
