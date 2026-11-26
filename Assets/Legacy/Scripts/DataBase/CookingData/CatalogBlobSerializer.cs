using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json.Linq;
using UnityEngine;

public sealed class CatalogDecodedData : IDisposable
{
    private readonly List<ScriptableObject> owned;
    private readonly Dictionary<string,ScriptableObject> assets;
    public IReadOnlyDictionary<string,ScriptableObject> Assets { get; private set; }
    public IList<CK01GeneratedDataTable> Tables { get; private set; }
    internal CatalogDecodedData(List<CK01GeneratedDataTable> tables, List<ScriptableObject> owned, Dictionary<string,ScriptableObject> assets = null)
    { Tables = tables.AsReadOnly(); this.owned = owned; this.assets = assets ?? new Dictionary<string,ScriptableObject>(); Assets = new System.Collections.ObjectModel.ReadOnlyDictionary<string,ScriptableObject>(this.assets); }
    public void Dispose()
    {
        foreach (var item in owned) if (item != null)
        {
#if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(item);
#else
            UnityEngine.Object.Destroy(item);
#endif
        }
        owned.Clear(); assets.Clear(); Tables = new List<CK01GeneratedDataTable>().AsReadOnly();
    }
}
/// <summary>Full typed asset graph. Runtime decoding never calls AssetDatabase or resolves arbitrary types.</summary>
public static class CatalogBlobSerializer
{
    public const string Schema = "2.5.1";
    private static bool SupportedSchema(string value) { return value == Schema || value == "2.4.1"; }
    private static readonly Type[] assetTypes = { typeof(CK01RecipeData), typeof(CK01RecipeSlotData), typeof(CK01RecipeStepData), typeof(CK01ItemData), typeof(CK01TagData), typeof(CK01ToolData), typeof(CK01InitialInventoryData), typeof(CK01CookingGameConfigData), typeof(CK01FridgeCapacityLevelData), typeof(CK01ProcessActionData), typeof(CK01ProcessingRecordData), typeof(CK01RecipeVariantData), typeof(KitchenAreaData), typeof(LocTableAsset), typeof(CK01CharacterData), typeof(CK01DialogueData), typeof(CK01DialogueLineData), typeof(CK01CGData), typeof(CK01SceneData), typeof(CK01SceneStateData), typeof(CK01SceneStatePlacementData), typeof(CK01AmbientPoolData) };
    public static IReadOnlyList<Type> AssetTypes { get { return Array.AsReadOnly(assetTypes); } }
    private static readonly Type[] valueTypes = { typeof(LocTableEntry), typeof(CK01OptionalFloat), typeof(CK01OptionalInt), typeof(CK01OptionalBool) };
    public static IReadOnlyList<Type> ValueTypes { get { return Array.AsReadOnly(valueTypes); } }
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
    public static FieldInfo[] Fields(Type type)
    {
        var result = new List<FieldInfo>();
        for (var t = type; t != null && t != typeof(ScriptableObject) && t != typeof(UnityEngine.Object) && t != typeof(object); t = t.BaseType)
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (!field.IsStatic && !field.IsDefined(typeof(NonSerializedAttribute),false) && (field.IsPublic || field.GetCustomAttributes(typeof(SerializeField), true).Length != 0)) result.Add(field);
        if (result.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != result.Count) throw new FormatException("Ambiguous serialized fields.");
        return result.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
    }
    public static string Key(string value)
    {
        if (value == null || value.Length < 34 || value[32] != ':' || value.Substring(0,32).Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))) throw new FormatException("Invalid stable GUID/localID key.");
        long id; if (!long.TryParse(value.Substring(33), NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || id == 0) throw new FormatException("Invalid localID.");
        return value;
    }
    // Shared formal build collection: capture's actual identity walk supplies both persistent registries.
    public static JObject CaptureReferences(CK01GeneratedDataCatalog catalog, Func<UnityEngine.Object,string> identity,
        IEnumerable<ScriptableObject> allDataAssets, Func<ScriptableObject,string,Sprite,bool> emptySprite,
        Func<ScriptableObject,string,GameObject,bool> emptyPrefab, out List<CatalogSpriteReference> sprites,
        out List<CatalogPrefabReference> prefabs)
    {
        var spriteRegistry = new SortedDictionary<string,Sprite>(StringComparer.Ordinal);
        var prefabRegistry = new SortedDictionary<string,GameObject>(StringComparer.Ordinal);
        var graph = Capture(catalog, asset => {
            var key = Key(identity(asset)); var sprite = asset as Sprite; if (sprite != null) spriteRegistry[key] = sprite;
            var prefab = asset as GameObject; if (prefab != null) prefabRegistry[key] = prefab; return key;
        }, allDataAssets, emptySprite, emptyPrefab);
        sprites = spriteRegistry.Select(x => new CatalogSpriteReference(x.Key,x.Value)).ToList();
        prefabs = prefabRegistry.Select(x => new CatalogPrefabReference(x.Key,x.Value)).ToList(); return graph;
    }
    public static JObject Capture(CK01GeneratedDataCatalog catalog, Func<UnityEngine.Object,string> identity, IEnumerable<ScriptableObject> allDataAssets = null)
    { return Capture(catalog, identity, allDataAssets, null); }
    public static JObject Capture(CK01GeneratedDataCatalog catalog, Func<UnityEngine.Object,string> identity, IEnumerable<ScriptableObject> allDataAssets,
        Func<ScriptableObject,string,Sprite,bool> isSerializedEmptySprite)
    { return Capture(catalog, identity, allDataAssets, isSerializedEmptySprite, null); }
    public static JObject Capture(CK01GeneratedDataCatalog catalog, Func<UnityEngine.Object,string> identity, IEnumerable<ScriptableObject> allDataAssets,
        Func<ScriptableObject,string,Sprite,bool> isSerializedEmptySprite, Func<ScriptableObject,string,GameObject,bool> isSerializedEmptyPrefab)
    {
        if (catalog == null || identity == null) throw new ArgumentNullException();
        var assets = new SortedDictionary<string, ScriptableObject>(StringComparer.Ordinal); var queue = new Queue<ScriptableObject>();
        var sprites = new SortedSet<string>(StringComparer.Ordinal);
        var prefabs = new SortedSet<string>(StringComparer.Ordinal);
        Func<ScriptableObject,string> add = obj => { var key = Key(identity(obj)); ScriptableObject previous; if (assets.TryGetValue(key,out previous)) { if (!ReferenceEquals(previous,obj)) throw new FormatException("Duplicate asset identity."); } else { if (!AssetTypes.Contains(obj.GetType())) throw new FormatException("Unsupported catalog asset type: " + obj.GetType().FullName); assets.Add(key,obj); queue.Enqueue(obj); } return key; };
        if(allDataAssets!=null)foreach(var asset in allDataAssets)if(asset!=null&&!ReferenceEquals(asset,catalog))add(asset);
        var tables = new JArray();
        foreach (var table in catalog.Tables)
        {
            if (table == null) throw new FormatException("Null catalog table."); var rows = new JArray();
            foreach (var row in table.Rows) { if (row == null || row.Asset == null) throw new FormatException("Null catalog row."); rows.Add(new JObject { ["id"]=row.StableId,["asset"]=add(row.Asset) }); }
            tables.Add(new JObject { ["name"]=table.TableName,["aggregate"]=table.AggregateAsset == null ? JValue.CreateNull() : new JValue(add(table.AggregateAsset)),["rows"]=rows });
        }
        var encoded = new SortedDictionary<string,JObject>(StringComparer.Ordinal);
        while (queue.Count != 0)
        {
            var asset = queue.Dequeue(); var fields = new JObject();
            foreach (var field in Fields(asset.GetType())) fields.Add(field.Name, Encode(field.GetValue(asset), field.FieldType, identity, add, sprites, prefabs, 0, asset, field.Name, isSerializedEmptySprite, isSerializedEmptyPrefab));
            var key = Key(identity(asset)); encoded.Add(key,new JObject { ["key"]=key,["type"]=asset.GetType().FullName,["name"]=asset.name ?? string.Empty,["fields"]=fields });
        }
        return new JObject { ["format"]=1,["schema"]=Schema,["catalog"]=Key(identity(catalog)),["assets"]=new JArray(encoded.Values),["tables"]=tables,["sprites"]=new JArray(sprites),["prefabs"]=new JArray(prefabs) };
    }
    private static JToken Encode(object value, Type type, Func<UnityEngine.Object,string> identity, Func<ScriptableObject,string> add, ISet<string> sprites, ISet<string> prefabs, int depth,
        ScriptableObject owner, string fieldPath, Func<ScriptableObject,string,Sprite,bool> isSerializedEmptySprite, Func<ScriptableObject,string,GameObject,bool> isSerializedEmptyPrefab)
    {
        if (depth > 32) throw new FormatException("Catalog field graph too deep.");
        if (value == null) return JValue.CreateNull();
        if (type == typeof(Sprite)) {
            var sprite=(Sprite)value;
            if (sprite==null && isSerializedEmptySprite!=null && isSerializedEmptySprite(owner,fieldPath,sprite)) return JValue.CreateNull();
            var key=Key(identity(sprite)); sprites.Add(key); return new JObject { ["sprite"]=key };
        }
        if (type == typeof(GameObject)) {
            var prefab=(GameObject)value;
            if(prefab==null && isSerializedEmptyPrefab!=null && isSerializedEmptyPrefab(owner,fieldPath,prefab))return JValue.CreateNull();
            if(prefab==null)throw new FormatException("Unresolved Prefab reference; no silent reference loss.");
            var key=Key(identity(prefab));prefabs.Add(key);return new JObject { ["prefab"]=key };
        }
        if (typeof(ScriptableObject).IsAssignableFrom(type)) return new JObject { ["asset"]=add((ScriptableObject)value) };
        if (type.IsEnum) return new JValue(Convert.ToInt64(value,CultureInfo.InvariantCulture));
        if (type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double)) return new JValue(value);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) { var array=new JArray(); var index=0; foreach(var item in (IEnumerable)value) { array.Add(Encode(item,type.GetGenericArguments()[0],identity,add,sprites,prefabs,depth+1,owner,fieldPath+".Array.data["+index+"]",isSerializedEmptySprite,isSerializedEmptyPrefab)); index++; } return array; }
        if (!ValueTypes.Contains(type)) throw new FormatException("Unsupported serialized field type: " + type.FullName);
        var obj=new JObject();foreach(var field in Fields(type))obj.Add(field.Name,Encode(field.GetValue(value),field.FieldType,identity,add,sprites,prefabs,depth+1,owner,fieldPath+"."+field.Name,isSerializedEmptySprite,isSerializedEmptyPrefab));return obj;
    }
    public static string Fingerprint(JObject graph)
    { using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Utf8.GetBytes(SaveEnvelope.SerializeCanonicalObject(graph)))).Replace("-",string.Empty).ToLowerInvariant(); }
    public static string FingerprintSource(string fingerprint)
    { ValidateFingerprint(fingerprint); return "// Prepared before compilation; never emitted in Build callback.\npublic static class CatalogBuildFingerprint { public const string Expected = \"" + fingerprint + "\"; }\n"; }
    private static void ValidateFingerprint(string value)
    { if(value==null||value.Length!=64||value.Any(c=>!(c>='0'&&c<='9'||c>='a'&&c<='f')))throw new FormatException("Catalog fingerprint is unprepared or invalid."); }
    public static byte[] Seal(JObject graph, CatalogBlobCipher cipher)
    {
        var root=new JObject { ["format"]=1,["schema"]=(string)graph["schema"],["fingerprint"]=Fingerprint(graph),["data"]=graph.DeepClone() }; var bytes=Utf8.GetBytes(SaveEnvelope.SerializeCanonicalObject(root));
        try{return cipher.Encrypt(bytes);}finally{Array.Clear(bytes,0,bytes.Length);}
    }
    public static JObject Authenticate(byte[] packet,string expected,CatalogBlobCipher cipher)
    {
        ValidateFingerprint(expected);var plain=cipher.Decrypt(packet);
        try
        {
            var root=SaveEnvelope.ParseCanonicalObject(Utf8.GetString(plain));
            if(root.Count!=4||(int?)root["format"]!=1||!SupportedSchema((string)root["schema"])||!(root["data"]is JObject))throw new FormatException("Unknown catalog payload version/schema.");
            var graph=(JObject)root["data"];if((string)graph["schema"]!=(string)root["schema"])throw new FormatException("Envelope/graph schema mismatch.");var actual=Fingerprint(graph);
            if((string)root["fingerprint"]!=actual||expected!=actual)throw new FormatException("Catalog fingerprint mismatch; no SO fallback.");
            return graph;
        }
        finally{Array.Clear(plain,0,plain.Length);}
    }
    public static CatalogDecodedData Open(byte[] packet,string expected,Func<string,Sprite> sprites,CatalogBlobCipher cipher)
    { return Open(packet,expected,sprites,key=>null,cipher); }
    public static CatalogDecodedData Open(byte[] packet,string expected,Func<string,Sprite> sprites,Func<string,GameObject> prefabs,CatalogBlobCipher cipher)
    { return Rehydrate(Authenticate(packet,expected,cipher),sprites,prefabs); }
    private static void ExactFields(JObject node,IEnumerable<string> names)
    { if(node==null||!node.Properties().Select(p=>p.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(names.OrderBy(x=>x,StringComparer.Ordinal)))throw new FormatException("Unknown or missing catalog fields."); }
    public static void ValidateMetadata(JObject graph) { using(var result=RehydrateCore(graph,null,null,true)) { } }
    public static CatalogDecodedData Rehydrate(JObject graph,Func<string,Sprite> resolve)
    { return Rehydrate(graph,resolve,key=>null); }
    public static CatalogDecodedData Rehydrate(JObject graph,Func<string,Sprite> resolve,Func<string,GameObject> prefabs)
    { if(resolve==null||prefabs==null)throw new ArgumentNullException("resolve");return RehydrateCore(graph,resolve,prefabs,false); }
    private static CatalogDecodedData RehydrateCore(JObject graph,Func<string,Sprite> resolve,Func<string,GameObject> resolvePrefab,bool metadataOnly)
    {
        ExactFields(graph,(string)graph?["schema"]==Schema ? new[]{"format","schema","catalog","assets","tables","sprites","prefabs"} : new[]{"format","schema","catalog","assets","tables","sprites"});
        if((int?)graph["format"]!=1||!SupportedSchema((string)graph["schema"]))throw new FormatException("Unknown graph schema.");Key((string)graph["catalog"]);
        var records=graph["assets"]as JArray;var tableRecords=graph["tables"]as JArray;var spriteRecords=graph["sprites"]as JArray;
        if(records==null||records.Count>10000||tableRecords==null||spriteRecords==null)throw new FormatException("Invalid catalog arrays.");
        var types=new Dictionary<string,Type>(StringComparer.Ordinal);var nodes=new Dictionary<string,JObject>(StringComparer.Ordinal);var sprites=new Dictionary<string,Sprite>(StringComparer.Ordinal);
        foreach(var record in records)
        {
            var node=record as JObject;ExactFields(node,new[]{"key","type","name","fields"});var key=Key((string)node["key"]);var type=AssetTypes.SingleOrDefault(t=>t.FullName==(string)node["type"]);
            if(type==null||nodes.ContainsKey(key)||node["name"].Type!=JTokenType.String)throw new FormatException("Unknown type/duplicate catalog asset.");nodes.Add(key,node);types.Add(key,type);
        }
        foreach(var token in spriteRecords){var key=Key((string)token);if(sprites.ContainsKey(key))throw new FormatException("Duplicate sprite key.");var sprite=metadataOnly?null:resolve(key);if(!metadataOnly&&sprite==null)throw new FormatException("Missing stable sprite: "+key);sprites.Add(key,sprite);}
        var prefabs=new Dictionary<string,GameObject>(StringComparer.Ordinal);
        if((string)graph["schema"]==Schema){
            var prefabRecords=graph["prefabs"] as JArray;if(prefabRecords==null)throw new FormatException("Invalid Prefab registry.");
            foreach(var token in prefabRecords){var key=Key((string)token);if(prefabs.ContainsKey(key))throw new FormatException("Duplicate Prefab key.");var prefab=metadataOnly?null:resolvePrefab(key);if(!metadataOnly&&prefab==null)throw new FormatException("Missing stable Prefab: "+key);prefabs.Add(key,prefab);}
        }
        var rowFields = new Dictionary<string,JObject>(StringComparer.Ordinal);
        foreach(var pair in nodes)
        {
            var fields=pair.Value["fields"] as JObject;
            // Old Character packets alone may omit the newly derived nullable Sprite. Never mutate authenticated input.
            if(types[pair.Key]==typeof(CK01CharacterData) && fields!=null && fields.Property(nameof(CK01CharacterData.portrait_sprite_asset))==null)
            { fields=(JObject)fields.DeepClone();fields.Add(nameof(CK01CharacterData.portrait_sprite_asset),JValue.CreateNull()); }
            ExactFields(fields,Fields(types[pair.Key]).Select(f=>f.Name));
            foreach(var field in Fields(types[pair.Key]))ReadValue(fields[field.Name],field.FieldType,null,types,sprites,prefabs,0);
            rowFields.Add(pair.Key,fields);
        }
        var tableNames=new HashSet<string>(StringComparer.Ordinal);
        foreach(var entry in tableRecords)
        {
            var table=entry as JObject;ExactFields(table,new[]{"name","aggregate","rows"});if(table["name"].Type!=JTokenType.String||string.IsNullOrEmpty((string)table["name"])||!tableNames.Add((string)table["name"])||!(table["rows"]is JArray))throw new FormatException("Invalid/duplicate table.");
            if(table["aggregate"].Type!=JTokenType.Null&&!types.ContainsKey(Key((string)table["aggregate"])))throw new FormatException("Unknown aggregate.");
            var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var entryRow in (JArray)table["rows"]){var row=entryRow as JObject;ExactFields(row,new[]{"id","asset"});if(row["id"].Type!=JTokenType.String||string.IsNullOrEmpty((string)row["id"])||!ids.Add((string)row["id"])||!types.ContainsKey(Key((string)row["asset"])))throw new FormatException("Invalid/duplicate row.");}
        }
        if(metadataOnly)return new CatalogDecodedData(new List<CK01GeneratedDataTable>(),new List<ScriptableObject>());
        // All authentication, schema, type, relation and sprite checks precede native object creation.
        var objects=new Dictionary<string,ScriptableObject>(StringComparer.Ordinal);var owned=new List<ScriptableObject>();
        try
        {
            foreach(var pair in types){var item=ScriptableObject.CreateInstance(pair.Value);owned.Add(item);objects.Add(pair.Key,item);item.name=(string)nodes[pair.Key]["name"];}
            foreach(var pair in objects)foreach(var field in Fields(types[pair.Key]))field.SetValue(pair.Value,ReadValue(rowFields[pair.Key][field.Name],field.FieldType,objects,types,sprites,prefabs,0));
            var tables=new List<CK01GeneratedDataTable>();foreach(JObject table in tableRecords){var rows=new List<CK01GeneratedDataRowReference>();foreach(JObject row in (JArray)table["rows"])rows.Add(new CK01GeneratedDataRowReference((string)row["id"],objects[(string)row["asset"]]));tables.Add(new CK01GeneratedDataTable((string)table["name"],table["aggregate"].Type==JTokenType.Null?null:objects[(string)table["aggregate"]],rows));}
            foreach(var item in owned){var locale=item as LocTableAsset;if(locale!=null)LocService.Configure(locale);}
            return new CatalogDecodedData(tables,owned,objects);
        }
        catch { new CatalogDecodedData(new List<CK01GeneratedDataTable>(),owned).Dispose();throw; }
    }
    private static object ReadValue(JToken node,Type type,IDictionary<string,ScriptableObject> objects,IDictionary<string,Type> types,IDictionary<string,Sprite> sprites,IDictionary<string,GameObject> prefabs,int depth)
    {
        if(depth>32||node==null)throw new FormatException("Invalid catalog field depth.");
        if(node.Type==JTokenType.Null){if(type.IsValueType||type==typeof(LocTableEntry))throw new FormatException("Invalid null value.");return null;}
        if(type==typeof(Sprite)){var obj=node as JObject;ExactFields(obj,new[]{"sprite"});var key=Key((string)obj["sprite"]);Sprite sprite;if(!sprites.TryGetValue(key,out sprite))throw new FormatException("Unknown sprite reference.");return sprite;}
        if(type==typeof(GameObject)){var obj=node as JObject;ExactFields(obj,new[]{"prefab"});var key=Key((string)obj["prefab"]);GameObject prefab;if(!prefabs.TryGetValue(key,out prefab))throw new FormatException("Unknown Prefab reference.");return prefab;}
        if(typeof(ScriptableObject).IsAssignableFrom(type)){var obj=node as JObject;ExactFields(obj,new[]{"asset"});var key=Key((string)obj["asset"]);Type actual;if(!types.TryGetValue(key,out actual)||!type.IsAssignableFrom(actual))throw new FormatException("Unknown/wrong asset relation.");return objects==null?null:objects[key];}
        if(type==typeof(string)){if(node.Type!=JTokenType.String)throw new FormatException("Expected string.");return (string)node;}
        if(type==typeof(bool)){if(node.Type!=JTokenType.Boolean)throw new FormatException("Expected bool.");return (bool)node;}
        if(type.IsEnum||type==typeof(int)||type==typeof(long)){if(node.Type!=JTokenType.Integer)throw new FormatException("Expected integer.");var number=(long)node;if(type.IsEnum)return Enum.ToObject(type,checked((int)number));return type==typeof(int)?(object)checked((int)number):number;}
        if(type==typeof(float)||type==typeof(double)){if(node.Type!=JTokenType.Float&&node.Type!=JTokenType.Integer)throw new FormatException("Expected number.");var number=(double)node;if(double.IsNaN(number)||double.IsInfinity(number))throw new FormatException("Nonfinite number.");if(type==typeof(float)){var f=(float)number;if(float.IsInfinity(f))throw new FormatException("Single overflow.");return f;}return number;}
        if(type.IsGenericType&&type.GetGenericTypeDefinition()==typeof(List<>)){var values=node as JArray;if(values==null)throw new FormatException("Expected list.");var list=(IList)Activator.CreateInstance(type);foreach(var value in values)list.Add(ReadValue(value,type.GetGenericArguments()[0],objects,types,sprites,prefabs,depth+1));return list;}
        if(!ValueTypes.Contains(type))throw new FormatException("Unsupported serialized type.");var fields=node as JObject;ExactFields(fields,Fields(type).Select(f=>f.Name));var target=Activator.CreateInstance(type);foreach(var field in Fields(type))field.SetValue(target,ReadValue(fields[field.Name],field.FieldType,objects,types,sprites,prefabs,depth+1));return target;
    }
}
