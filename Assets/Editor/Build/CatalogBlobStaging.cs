using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EatWhat.Tools.Persistence;
using EatWhat.Tools.Security;
using Newtonsoft.Json.Linq;
/// <summary>Explicit opt-in task output export. Default DATA flow never calls it.</summary>
public static class CatalogBlobStaging
{
    public static string Export(JObject graph,string directory,string approvedRoot,Action<string> fault = null)
    {
        var full=Path.GetFullPath(directory);var root=Path.GetFullPath(approvedRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!Path.IsPathFullyQualified(directory)||!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Task-output staging only.");
        for(var item=new DirectoryInfo(full);item!=null&&item.FullName.Length>=root.Length-1;item=item.Parent)if(item.Exists&&(item.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("No linked staging paths.");
        if(Directory.Exists(full)&&Directory.EnumerateFileSystemEntries(full).Any())throw new IOException("Refuse nonempty staging; never overwrite unknown output.");
        CatalogBlobSerializer.ValidateMetadata(graph);
        var created=new List<string>();var newDirectory=!Directory.Exists(full);Directory.CreateDirectory(full);var success=false;
        try
        {
            var fingerprint=CatalogBlobSerializer.Fingerprint(graph);var cipher=new CatalogBlobCipher(new ObfuscatedStaticKeyProvider());var packet=CatalogBlobSerializer.Seal(graph,cipher);
            if(SaveEnvelope.SerializeCanonicalObject(CatalogBlobSerializer.Authenticate(packet,fingerprint,cipher))!=SaveEnvelope.SerializeCanonicalObject(graph))throw new InvalidDataException("Catalog staged readback mismatch.");
            var values=new Dictionary<string,byte[]> { ["catalog.graph.json"]=Encoding.UTF8.GetBytes(SaveEnvelope.SerializeCanonicalObject(graph)),["catalog.bin"]=packet,["CatalogBuildFingerprint.g.cs"]=Encoding.UTF8.GetBytes(CatalogBlobSerializer.FingerprintSource(fingerprint)),["export.json"]=Encoding.UTF8.GetBytes(new JObject{["fingerprint"]=fingerprint,["dataAssets"]=((JArray)graph["assets"]).Count,["spriteKeys"]=((JArray)graph["sprites"]).Count,["authenticatedReadback"]=true}.ToString()) };
            foreach(var pair in values){var path=Path.Combine(full,pair.Key);using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write)){created.Add(path);stream.Write(pair.Value,0,pair.Value.Length);stream.Flush(true);}if(fault!=null)fault(pair.Key);}
            success=true;return fingerprint;
        }
        finally
        {
            if(!success){foreach(var path in created)File.Delete(path);if(newDirectory&&Directory.Exists(full)&&!Directory.EnumerateFileSystemEntries(full).Any())Directory.Delete(full,false);}
        }
    }
}
