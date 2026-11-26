using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.ExceptionServices;
/// <summary>Shared decision/finally policy; no Editor IO and no assertion of native package exclusion.</summary>
public sealed class CatalogBuildPlan
{
    public const string Define = "CK01_ENCRYPTED_DATA";
    public readonly string CatalogGuid;
    private readonly HashSet<string> excluded;
    public CatalogBuildPlan(string catalogGuid, IEnumerable<string> assetGuids)
    { CatalogGuid = catalogGuid; excluded = new HashSet<string>(assetGuids,StringComparer.Ordinal); excluded.Add(catalogGuid); }
    public int ExcludedCount { get { return excluded.Count; } }
    public bool RemapCatalog(string guid)
    {
        if (guid == CatalogGuid) return true;
        if (excluded.Contains(guid)) throw new InvalidOperationException("Direct generated row SO reference cannot be shipped: " + guid);
        return false;
    }
    public void AssertNoExcluded(IEnumerable<string> guids)
    { foreach (var guid in guids) if (excluded.Contains(guid)) throw new InvalidOperationException("Generated SO remains in build dependencies: " + guid); }
    public static string[] Defines(IEnumerable<string> original, bool development)
    { var values = new List<string>((original ?? new string[0]).Where(x => x != Define)); if (!development) values.Add(Define); return values.Distinct(StringComparer.Ordinal).ToArray(); }
    public static void ValidateOwnedPath(string path,string root)
    {
        if(string.IsNullOrEmpty(path)||path.Contains("\\")||path.Contains(":")||path.Split('/').Any(x=>x.Length==0||x=="."||x==".."||x.TrimEnd(' ','.')!=x))throw new InvalidOperationException("Noncanonical owned asset path.");
        if(!(path==root||path.StartsWith(root+"/",StringComparison.Ordinal)||path=="Assets/StreamingAssets/data/catalog.bin"||path=="Assets/StreamingAssets/data"||path=="Assets/StreamingAssets"))throw new InvalidOperationException("Owned asset path escape.");
    }
    public const int OwnershipSchema = 2;
    public sealed class OwnedInput
    {
        public readonly string Path, Sha256, MetaSha256;
        public readonly bool IsDirectory;
        public OwnedInput(string path,bool directory,string sha256,string metaSha256)
        { Path=path;IsDirectory=directory;Sha256=sha256;MetaSha256=metaSha256; }
    }
    private static FileAttributes? Attributes(string path)
    {
        try{return File.GetAttributes(path);}
        catch(FileNotFoundException){return null;}
        catch(DirectoryNotFoundException){return null;}
    }
    private static void AssertAncestors(string full)
    {
        for(var dir=new DirectoryInfo(System.IO.Path.GetDirectoryName(full));dir!=null;dir=dir.Parent)
        {
            var flags=Attributes(dir.FullName);
            if(flags.HasValue&&((flags.Value&FileAttributes.ReparsePoint)!=0||(flags.Value&FileAttributes.Directory)==0))
                throw new IOException("Linked/non-directory build input ancestor: "+dir.FullName);
        }
    }
    private static string FullOwnedPath(string project,string root,string path)
    {
        ValidateOwnedPath(path,root);
        if(path.EndsWith(".meta",StringComparison.OrdinalIgnoreCase))throw new IOException("Metadata requires explicit companion proof, not an asset record: "+path);
        var full=System.IO.Path.GetFullPath(System.IO.Path.Combine(project,path));
        var prefix=System.IO.Path.GetFullPath(project).TrimEnd('\\','/')+System.IO.Path.DirectorySeparatorChar;
        if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new IOException("Owned input escaped project.");
        AssertAncestors(full);return full;
    }
    private static string Digest(string path)
    {using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-",string.Empty);}
    private static bool HashValid(string value)
    {return value!=null&&System.Text.RegularExpressions.Regex.IsMatch(value,"^[0-9A-F]{64}$");}
    private static void RequireKind(string path,FileAttributes flags,bool directory)
    {
        if((flags&FileAttributes.ReparsePoint)!=0||((flags&FileAttributes.Directory)!=0)!=directory)
            throw new IOException("Linked or changed input kind: "+path);
    }
    public static void AssertNewOwnedInput(string project,string root,string path)
    {
        var full=FullOwnedPath(project,root,path);
        if(Attributes(full).HasValue||Attributes(full+".meta").HasValue)throw new IOException("Pre-existing asset or metadata is not owned: "+path);
    }
    public static OwnedInput CaptureCreatedInput(string project,string root,string path,bool directory)
    {
        var full=FullOwnedPath(project,root,path);var asset=Attributes(full);var meta=Attributes(full+".meta");
        if(!asset.HasValue||!meta.HasValue)throw new IOException("Creation incomplete; preserve pending journal: "+path);
        RequireKind(full,asset.Value,directory);RequireKind(full+".meta",meta.Value,false);
        return new OwnedInput(path,directory,directory?null:Digest(full),Digest(full+".meta"));
    }
    private static bool VerifyPair(string project,string root,OwnedInput item)
    {
        var full=FullOwnedPath(project,root,item.Path);var asset=Attributes(full);var meta=Attributes(full+".meta");
        if(!asset.HasValue&&!meta.HasValue)return false; // An already removed complete pair is idempotent.
        if(!asset.HasValue||!meta.HasValue)throw new IOException("Missing asset/companion pair; preserve: "+item.Path);
        RequireKind(full,asset.Value,item.IsDirectory);RequireKind(full+".meta",meta.Value,false);
        if(Digest(full+".meta")!=item.MetaSha256||(!item.IsDirectory&&Digest(full)!=item.Sha256))
            throw new IOException("Owned asset/metadata drift; preserve: "+item.Path);
        return true;
    }
    public sealed class OwnedCleanup
    {
        private readonly string project,root;
        private readonly OwnedInput[] items;
        internal OwnedCleanup(string project,string root,IEnumerable<OwnedInput> items)
        {this.project=project;this.root=root;this.items=items.OrderByDescending(x=>x.Path.Count(c=>c=='/')).ThenBy(x=>x.IsDirectory).ToArray();}
        public int Count {get{return items.Length;}}
        public void Execute(Action<string> deleteAssetAndMeta)
        {
            foreach(var item in items)
            {
                if(!VerifyPair(project,root,item))continue;
                var full=FullOwnedPath(project,root,item.Path);
                if(item.IsDirectory&&Directory.EnumerateFileSystemEntries(full).Any())
                    throw new IOException("Owned directory is not truly empty: "+item.Path);
                deleteAssetAndMeta(item.Path);
                if(Attributes(full).HasValue||Attributes(full+".meta").HasValue)
                    throw new IOException("Owned pair deletion incomplete; preserve journal: "+item.Path);
            }
        }
    }
    public static OwnedCleanup PlanOwnedCleanup(string project,string root,int schema,string pending,IEnumerable<OwnedInput> records)
    {
        if(schema!=OwnershipSchema||pending!=null||records==null)throw new IOException("Missing/old/incomplete ownership journal; preserve all inputs.");
        if(!System.Text.RegularExpressions.Regex.IsMatch(root??string.Empty,@"^Assets/__CatalogBlobBuild_[0-9a-f]{32}$"))
            throw new IOException("Untrusted cleanup root.");
        var owned=records.ToArray();var nodes=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var directories=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var present=new List<OwnedInput>();
        foreach(var item in owned)
        {
            if(item==null||!HashValid(item.MetaSha256)||(item.IsDirectory?item.Sha256!=null:!HashValid(item.Sha256)))
                throw new IOException("Incomplete asset/metadata ownership proof.");
            var full=FullOwnedPath(project,root,item.Path);
            if(!nodes.Add(full)||!nodes.Add(full+".meta"))throw new IOException("Duplicate/aliased ownership path.");
            if(item.IsDirectory)directories.Add(full);
            if(VerifyPair(project,root,item))present.Add(item);
        }
        var temp=FullOwnedPath(project,root,root);
        if((Attributes(temp).HasValue||Attributes(temp+".meta").HasValue)&&!directories.Contains(temp))
            throw new IOException("Temporary root has no creation proof; preserve.");
        var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var queue=new Stack<string>(directories);
        while(queue.Count!=0)
        {
            var dir=queue.Pop();if(!visited.Add(dir)||!Attributes(dir).HasValue)continue;
            foreach(var child in Directory.EnumerateFileSystemEntries(dir))
            {
                var flags=File.GetAttributes(child);
                if((flags&FileAttributes.ReparsePoint)!=0||!nodes.Contains(child))
                    throw new IOException("Unknown/linked build input; preserve: "+child);
                if((flags&FileAttributes.Directory)!=0)
                {
                    if(!directories.Contains(child))throw new IOException("Unknown build directory; preserve: "+child);
                    queue.Push(child);
                }
            }
        }
        // All pairs and every directory entry were checked before a deletion callback is available.
        return new OwnedCleanup(project,root,present);
    }
    public static T Execute<T>(Action prepare, Func<T> build, Action cleanup)
    {
        T result=default(T);
        ExceptionDispatchInfo workError=null,cleanupError=null;
        try { prepare(); result=build(); }
        catch(Exception error) { workError=ExceptionDispatchInfo.Capture(error); }
        try { cleanup(); }
        catch(Exception error) { cleanupError=ExceptionDispatchInfo.Capture(error); }
        if(workError!=null&&cleanupError!=null)
            throw new AggregateException("Build work and owned-input cleanup both failed.",workError.SourceException,cleanupError.SourceException);
        if(workError!=null)workError.Throw();
        if(cleanupError!=null)cleanupError.Throw();
        return result;
    }
}
