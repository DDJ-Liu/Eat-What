using System;
using EatWhat.LongLoop;
using UnityEngine;
internal static class NpcShellStateRestoreTests
{
    static int checks;static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
    static GameObject Child(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}
    public static int Main()
    {
        var root=new GameObject("root");var mount=Child(root.transform,"visual");var foot=Child(root.transform,"foot");var head=Child(root.transform,"head");
        mount.transform.localScale=new Vector3(-2,3,4);var shell=root.Add<NpcShell>();shell.ConfigureReferences(foot.transform,mount.transform,head.transform);
        var a=Child(mount.transform,"authoredA");var b=Child(mount.transform,"authoredB");b.SetActive(false);a.transform.localPosition=new Vector3(1,2,3);a.transform.localRotation=Quaternion.Euler(0,0,20);
        var backup=shell.CaptureSceneStateVisuals();Check(backup!=null&&ReferenceEquals(shell.CurrentVisual,a),"capture retains original without changing picture");
        Check(shell.CaptureSceneStateVisuals()==null,"one visual owner only");
        var prefab=new GameObject("replacement");var replacement=shell.SetVisual(prefab);shell.SetFacing(true);
        Check(mount.transform.childCount==1&&ReferenceEquals(shell.CurrentVisual,replacement),"only new replacement on mount");
        Check(!a.activeSelf&&a.transform.parent==null&&!UnityEngine.Object.PendingDestruction.Contains(a)&&!UnityEngine.Object.PendingDestruction.Contains(b),"authored objects retained inactive, never destroyed/cloned");
        backup.Restore();Check(mount.transform.childCount==2&&ReferenceEquals(mount.transform.GetChild(0).gameObject,a)&&ReferenceEquals(mount.transform.GetChild(1).gameObject,b),"restore exact child identities/order");
        Check(a.activeSelf&&!b.activeSelf&&a.transform.localPosition.Equals(new Vector3(1,2,3))&&a.transform.localRotation.degrees==20,"restore child pose/activation");
        Check(mount.transform.localScale.x==-2&&mount.transform.localScale.y==3&&mount.transform.localScale.z==4,"facing restoration preserves magnitude/other axes");
        Check(UnityEngine.Object.PendingDestruction.Contains(replacement),"only generated replacement destroyed");
        var next=shell.SetVisual(prefab);backup.Restore();Check(ReferenceEquals(shell.CurrentVisual,a)&&UnityEngine.Object.PendingDestruction.Contains(next),"retained baseline survives repeated replacements");
        backup.Dispose();var second=shell.CaptureSceneStateVisuals();Check(second!=null,"release permits next explicit owner");second.Dispose();
        var empty=new GameObject("empty");var emptyMount=Child(empty.transform,"visual");var emptyShell=empty.Add<NpcShell>();emptyShell.ConfigureReferences(null,emptyMount.transform,null);var emptyBackup=emptyShell.CaptureSceneStateVisuals();emptyShell.SetVisual(prefab);emptyBackup.Dispose();Check(emptyMount.transform.childCount==0,"authored empty visual restored to none");
        Console.WriteLine("NPC_STATE_RESTORE_PASS checks="+checks);return 0;
    }
}
