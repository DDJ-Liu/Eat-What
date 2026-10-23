using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Numerics;
using Newtonsoft.Json;
using EatWhat.Tools.Security;
using EatWhat.Tools.Persistence;
using Newtonsoft.Json.Linq;
public static class ToolkitPersistenceTests
{
    // Synthetic public test key, not the configured client provider.
    private sealed class Keys : IKeyProvider
    { private readonly int offset; public Keys(int offset=0){this.offset=offset;} public byte[] GetKey(){var key=new byte[32];for(var i=0;i<32;i++)key[i]=(byte)(i+offset);return key;} }
    public static IList<string> PassedCases { get; private set; }
    private static void Check(bool value,string name){if(!value)throw new Exception(name);PassedCases.Add(name);}
    private static bool Equal(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(var i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    private static readonly DateTime Time = new DateTime(2026,10,19,0,0,0,DateTimeKind.Utc);
    private static SaveEnvelope Envelope(string favorite="a")
    {return SaveEnvelope.Create(new JObject{["player_profile"]=new JObject{["favorite_recipe_ids"]=new JArray(favorite),["unlocked_recipe_ids"]=new JArray()},["future_section"]=new JObject{["count"]=3,["text"]="未来"}},Time);}
    private static EnvelopeAuthenticator Auth(){return new EnvelopeAuthenticator(new Keys());}
    public static string Run(string isolatedRoot)
    {
        PassedCases=new List<string>();Directory.CreateDirectory(isolatedRoot);var auth=Auth();var envelope=Envelope();
        var signed=auth.Sign(envelope);Check(signed.Hmac.Length==44 && auth.Verify(SaveEnvelope.Parse(signed.ToJson())),"real HMAC and envelope parse roundtrip");
        var firstKey=new DerivedKeyProvider(new Keys(),"EatWhat.Toolkit.Security/v2/authentication").GetKey();
        var secondKey=new DerivedKeyProvider(new Keys(),"EatWhat.Toolkit.Persistence/save-envelope-authentication/v1").GetKey();
        Check(!Equal(firstKey,secondKey),"envelope and cipher MAC use different purpose-separated subkeys");Array.Clear(firstKey,0,firstKey.Length);Array.Clear(secondKey,0,secondKey.Length);
        var canonicalA=SaveEnvelope.Create(new JObject{["z"]=1,["a"]=new JObject{["z"]=false,["a"]="值"}},Time);
        var canonicalB=SaveEnvelope.Create(new JObject{["a"]=new JObject{["a"]="值",["z"]=false},["z"]=1},Time);
        Check(auth.Sign(canonicalA).ToJson()==auth.Sign(canonicalB).ToJson(),"canonical object order culture and Unicode authentication bytes");
        Check(!new EnvelopeAuthenticator(new Keys(1)).Verify(signed),"wrong envelope HMAC key rejected");
        var clone=signed.Payload;clone["future_section"]["count"]=99;Check((int)signed.Payload["future_section"]["count"]==3,"envelope payload getter is detached");
        var originalPayload=new JObject{["x"]=1};var snapshot=SaveEnvelope.Create(originalPayload,Time);originalPayload["x"]=2;Check((int)snapshot.Payload["x"]==1,"envelope constructor detaches caller payload");
        var rejectedSingle=false;try{SaveEnvelope.Create(new JObject{["invalid"]=new JValue(float.NaN)},Time);}catch(FormatException){rejectedSingle=true;}
        Check(rejectedSingle,"non-finite Single JSON payload rejected before writing");
        string diagnostic;SaveEnvelope read;
        var plainRoot=Path.Combine(isolatedRoot,"plain");var store=new JsonSaveStore(plainRoot,new PlainJsonCodec(),auth);
        Check(!store.TryRead("default",out read,out diagnostic) && diagnostic==null && !Directory.Exists(plainRoot),"missing file is normal default without creating root");
        Check(store.Write("default",envelope,out diagnostic),"atomic initial save write");var path=Path.Combine(plainRoot,"default.json");var original=File.ReadAllBytes(path);
        Check(store.TryRead("default",out read,out diagnostic) && (string)read.Payload["player_profile"]["favorite_recipe_ids"][0]=="a" && auth.Verify(read),"plain store actual IO read profile section");
        Check(store.Write("default",envelope,out diagnostic) && Equal(original,File.ReadAllBytes(path)) && Directory.GetFiles(plainRoot,"*.backup-*").Length==0,"identical plain envelope write is byte-idempotent");
        Check(store.Write("default",Envelope("b"),out diagnostic),"atomic replacement succeeds");var backups=Directory.GetFiles(plainRoot,"*.backup-*");
        Check(backups.Length==1 && Equal(File.ReadAllBytes(backups[0]),original),"atomic replacement retains exact original in unique backup");
        var failStore=new JsonSaveStore(plainRoot,new PlainJsonCodec(),auth,(temp,target,backup)=>{throw new IOException("injected atomic failure");});
        Check(failStore.TryRead("default",out read,out diagnostic),"atomic failure fixture starts from valid read");var beforeFailure=File.ReadAllBytes(path);
        Check(!failStore.Write("default",Envelope("c"),out diagnostic) && Equal(beforeFailure,File.ReadAllBytes(path)) && failStore.IsWriteBlocked("default"),"failed replacement leaves old file and blocks retry");
        Check(Directory.GetFiles(plainRoot,"*.tmp-*").Length==0,"only owned unchanged temporary stage is cleaned");
        var raceStore=new JsonSaveStore(plainRoot,new PlainJsonCodec(),auth);raceStore.TryRead("default",out read,out diagnostic);
        var external=Encoding.UTF8.GetBytes(auth.Sign(Envelope("external")).ToJson());File.WriteAllBytes(path,external);
        Check(!raceStore.Write("default",Envelope("stale"),out diagnostic) && Equal(external,File.ReadAllBytes(path)),"concurrent file drift refused without overwriting new external bytes");
        var unsupportedStore=new JsonSaveStore(plainRoot,new PlainJsonCodec(),auth,(temp,target,backup)=>{throw new PlatformNotSupportedException("injected unsupported atomic filesystem");});
        Check(!unsupportedStore.Write("default",Envelope("unsupported"),out diagnostic) && diagnostic!=null && unsupportedStore.IsWriteBlocked("default") && Equal(external,File.ReadAllBytes(path)),"unsupported atomic filesystem returns diagnostic and retains original");
        foreach(var slot in new[]{"../bad","..","a/b","a\\b","bad:slot","","CON","LPT1","name.","有空格"})
        {Check(!store.Write(slot,envelope,out diagnostic) && diagnostic!=null,"invalid or escaping slot refused: "+slot);}
        var encryptedRoot=Path.Combine(isolatedRoot,"encrypted");var encrypted=new JsonSaveStore(encryptedRoot,new EncryptedJsonCodec(new AesCbcHmacCipher(new Keys())),auth);
        Check(encrypted.Write("default",envelope,out diagnostic) && encrypted.TryRead("default",out read,out diagnostic) && (int)read.Payload["future_section"]["count"]==3,"encrypted shared codec actual file roundtrip with unknown payload section");
        Check(EncryptedBlob.Parse(File.ReadAllBytes(Path.Combine(encryptedRoot,"default.json"))).Version==2,"release store uses existing explicit V2 encrypted blob");
        var faults=new Dictionary<string,byte[]>();faults["broken"]=Encoding.UTF8.GetBytes("{bad");faults["bad_utf8"]=new byte[]{255,255};
        faults["format"]=Encoding.UTF8.GetBytes(auth.Sign(new SaveEnvelope(99,SaveEnvelope.CurrentSchemaVersion,envelope.CreatedUtc,envelope.Payload,"")).ToJson());
        faults["schema"]=Encoding.UTF8.GetBytes(auth.Sign(new SaveEnvelope(1,"2.5",envelope.CreatedUtc,envelope.Payload,"")).ToJson());
        faults["hmac"]=Encoding.UTF8.GetBytes(signed.WithHmac(Convert.ToBase64String(new byte[32])).ToJson());
        faults["duplicate"]=Encoding.UTF8.GetBytes("{\"format_version\":1,\"format_version\":1}");
        foreach(var fault in faults)
        {
            var folder=Path.Combine(isolatedRoot,"fault_"+fault.Key);Directory.CreateDirectory(folder);var file=Path.Combine(folder,"default.json");File.WriteAllBytes(file,fault.Value);var broken=new JsonSaveStore(folder,new PlainJsonCodec(),auth);
            Check(!broken.TryRead("default",out read,out diagnostic) && diagnostic!=null && broken.IsWriteBlocked("default"),"corrupt/version fault yields diagnostic and write block: "+fault.Key);
            Check(!broken.Write("default",envelope,out diagnostic) && Equal(File.ReadAllBytes(file),fault.Value),"fallback auto-save preserves exact invalid original: "+fault.Key);
        }
        ISaveCodec selected=SaveCodecFactory.CreateForCurrentBuild(new Keys());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Check(selected is PlainJsonCodec,"Editor or Development compile branch selects plain codec");
#else
        Check(selected is EncryptedJsonCodec,"release compile branch selects encrypted codec");
#endif
        var legacy=Convert.FromBase64String("RVdTQgIAAAAwGrmPjAgyxTLVb0W1RLcxUorLUne+Gjr8lSzddbTtKryrMV04Ig/svz/boGWunBx+gj+1eUjwgJSbU+q884US0MHL1uXk1fAFvf2shhpNZm+b2CRHLED6JPbs6LvKWedd");string json;
        Check(new EncryptedJsonCodec(new AesCbcHmacCipher(new Keys())).TryDecode(legacy,out json,out diagnostic) && json=="{\"crossRuntime\":\"Unity-Mono-and-host\"}","AI274 synthetic V2 packet still decrypts after shared KDF extraction");
        NumericRoundtrips(isolatedRoot,auth);
        return "PASS: Toolkit.Persistence "+PassedCases.Count+" assertions";
    }
    private static void NumericRoundtrips(string root,EnvelopeAuthenticator auth)
    {
        var priorJson=Encoding.UTF8.GetString(Convert.FromBase64String("eyJjcmVhdGVkX3V0YyI6IjIwMjYtMTAtMTlUMDA6MDA6MDAuMDAwMDAwMFoiLCJmb3JtYXRfdmVyc2lvbiI6MSwiaG1hYyI6InVCUEJqT1FZZkFSY3haTEpsVzl0bWdjNGZFY0hJVDJ2T2ZlcHd1S1lZWVk9IiwicGF5bG9hZCI6eyJmdXR1cmVfc2VjdGlvbiI6eyJjb3VudCI6MywidGV4dCI6Ilx1NjcyYVx1Njc2NSJ9LCJwbGF5ZXJfcHJvZmlsZSI6eyJmYXZvcml0ZV9yZWNpcGVfaWRzIjpbImV4dGVybmFsIl0sInVubG9ja2VkX3JlY2lwZV9pZHMiOltdfX0sInNjaGVtYV92ZXJzaW9uIjoiMi40LjEifQ=="));
        Check(auth.Verify(SaveEnvelope.Parse(priorJson)),"exact AI275 readable envelope authenticates without migration");
        var values=new Dictionary<string,object> {
            {"ordinary",1.25d},{"tiny",1e-30d},{"large",1e100d},
            {"double_min_positive",double.Epsilon},{"double_max",double.MaxValue},{"double_min",double.MinValue},
            {"double_fraction",0.1d},{"double_17_digits",1.2345678901234567d},{"double_integral",1e20d},{"negative_zero",-0.0d},
            {"single_tiny",float.Epsilon},{"single_max",float.MaxValue},{"single_fraction",1.2345678f},
            {"decimal_precise",0.1234567890123456789012345678m},{"decimal_max",decimal.MaxValue},
            {"decimal_min",decimal.MinValue},{"decimal_smallest",0.0000000000000000000000000001m},
            {"decimal_trailing",1.2300m},{"decimal_integral",9007199254740993m},
            {"long_max",long.MaxValue},{"long_min",long.MinValue},{"ulong_max",ulong.MaxValue},
            {"integer_above_double_exact",9007199254740993L},{"big_integer",BigInteger.Parse("12345678901234567890123456789012345678901234567890",CultureInfo.InvariantCulture)}
        };
        var rows=new JArray();
        foreach(var mode in new[]{"plain","encrypted"}) foreach(var pair in values)
        {
            var folder=Path.Combine(root,"numeric_"+mode+"_"+pair.Key);
            Func<ISaveCodec> codec=()=>mode=="plain"?(ISaveCodec)new PlainJsonCodec():new EncryptedJsonCodec(new AesCbcHmacCipher(new Keys()));
            var expected=new JValue(pair.Value);
            var envelope=SaveEnvelope.Create(new JObject{["player_profile"]=new JObject{["favorite_recipe_ids"]=new JArray("known"),["unlocked_recipe_ids"]=new JArray()},["future_section"]=new JObject{["number"]=expected,["array"]=new JArray(expected.DeepClone())}},Time);
            string diagnostic;var writer=new JsonSaveStore(folder,codec(),auth);var wrote=writer.Write("default",envelope,out diagnostic);
            var fresh=new JsonSaveStore(folder,codec(),Auth());SaveEnvelope read;var loaded=fresh.TryRead("default",out read,out diagnostic);
            var actual=loaded?read.Payload["future_section"]["number"]:null;
            var equivalent=loaded && NumericEquivalent(pair.Value,(JValue)actual) && actual.Type==expected.Type;
            var stable=loaded && auth.Verify(read) && auth.Sign(envelope).ToJson()==read.ToJson();
            rows.Add(new JObject{["codec"]=mode,["case"]=pair.Key,["inputClrType"]=pair.Value.GetType().FullName,["inputJson"]=expected.ToString(Formatting.None),["writeSucceeded"]=wrote,["newStoreReadSucceeded"]=loaded,["outputClrType"]=loaded?((JValue)actual).Value.GetType().FullName:null,["outputJson"]=loaded?actual.ToString(Formatting.None):null,["sameNumericMeaningAndJsonKind"]=equivalent,["hmacAndCanonicalTextStable"]=stable,["diagnostic"]=diagnostic});
            File.WriteAllText(Path.Combine(root,"numeric-matrix.json"),rows.ToString(Formatting.Indented));
            Check(wrote && loaded && equivalent && stable && !fresh.IsWriteBlocked("default"),"numeric exact new-store authenticated roundtrip: "+mode+":"+pair.Key);
            var pretty=auth.Sign(envelope).ToJson().Replace(",",",\r\n");var parsed=SaveEnvelope.Parse(pretty);
            Check(auth.Verify(parsed) && parsed.ToJson()==read.ToJson(),"multiline numeric lexeme and array preserved: "+mode+":"+pair.Key);
        }
        foreach(var invalid in new object[]{double.NaN,double.PositiveInfinity,double.NegativeInfinity,float.NaN,float.PositiveInfinity,float.NegativeInfinity,new JRaw("1e400")})
        {
            var folder=Path.Combine(root,"unsupported_"+rows.Count);var store=new JsonSaveStore(folder,new PlainJsonCodec(),auth);string diagnostic;
            Check(store.Write("default",Envelope("preserved"),out diagnostic),"unsupported numeric fixture writes valid original: "+rows.Count);
            var file=Path.Combine(folder,"default.json");var original=File.ReadAllBytes(file);var rejected=false;string reason=null;
            try {var value=invalid as JToken ?? new JValue(invalid);var candidate=SaveEnvelope.Create(new JObject{["future_section"]=value},Time);rejected=!store.Write("default",candidate,out reason);}
            catch(FormatException error){rejected=true;reason=error.Message;}
            var preserved=Equal(original,File.ReadAllBytes(file)) && Directory.GetFiles(folder,"*.backup-*").Length==0 && Directory.GetFiles(folder,"*.tmp-*").Length==0;
            rows.Add(new JObject{["case"]="unsupported_"+invalid.GetType().Name,["inputJson"]=invalid.ToString(),["writeSucceeded"]=false,["rejectedBeforeOriginalChanged"]=rejected&&preserved,["diagnostic"]=reason});
            Check(rejected && preserved && !string.IsNullOrEmpty(reason),"unsupported numeric rejected before existing file or backup changes: "+rows.Count);
        }
        foreach(var text in new[]{"1.23456789012345678901234567890123456789","1e400","1e-400"})
        {
            var signed=auth.Sign(Envelope()).ToJson();var raw=signed.Replace("\"count\":3","\"count\":"+text);var rejected=false;
            try{SaveEnvelope.Parse(raw);}catch(Exception error) when(error is FormatException || error is JsonException || error is OverflowException){rejected=true;}
            Check(rejected,"unrepresentable raw float rejected without decimal/double rounding: "+text);
            rows.Add(new JObject{["case"]="unsupported_raw_float",["inputJson"]=text,["parseRejected"]=rejected,["fileIO"]=false});
        }
        File.WriteAllText(Path.Combine(root,"numeric-matrix.json"),rows.ToString(Formatting.Indented));
    }
    private static bool NumericEquivalent(object expected,JValue actual)
    {
        var text=actual.ToString(Formatting.None);
        if(expected is decimal){decimal parsed;return decimal.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out parsed) && parsed==(decimal)expected;}
        if(expected is double){double parsed;return double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out parsed) && parsed==(double)expected;}
        if(expected is float){float parsed;return float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out parsed) && parsed==(float)expected;}
        BigInteger integer;return BigInteger.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out integer) && integer==BigInteger.Parse(Convert.ToString(expected,CultureInfo.InvariantCulture),CultureInfo.InvariantCulture);
    }
    public static int Main(string[] args)
    {try{Console.WriteLine(Run(args[0]));Console.WriteLine("PERSISTENCE_ASSERTIONS="+PassedCases.Count);return 0;}catch(Exception error){Console.Error.WriteLine("PERSISTENCE_TEST_FAILED="+error.GetType().Name+":"+error.Message);return 1;}}
}
