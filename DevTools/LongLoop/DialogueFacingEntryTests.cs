using System;
using System.Collections.Generic;
using EatWhat.LongLoop;
using UnityEngine;

internal static class DialogueFacingEntryTests
{
    static readonly List<string> details=new List<string>();
    static void Check(bool v,string reason){if(!v)throw new Exception(reason);details.Add(reason);}
    sealed class Player:LongLoopPlayerController { public void Init(){Awake();} public void Tick(){Update();} }
    sealed class Avatar:IPlayerAvatar
    { public bool Facing;public int Calls; public void SetMoving(bool moving){} public void SetFacing(bool right){Facing=right;Calls++;} }
    static int Main()
    {
        try
        {
            var p=new Player();var avatar=new Avatar();p.ConfigureInterfaces(null,avatar);p.Init();
            p.transform.position=new Vector3(4,5,6);var position=p.transform.position;
            p.SetDialogueFacing(false);Check(!p.IsFacingRight&&!avatar.Facing,"left entry reaches original avatar/facing path");
            p.SetDialogueFacing(true);Check(p.IsFacingRight&&avatar.Facing,"right entry reaches original avatar/facing path");
            Check(p.transform.position.Equals(position),"left/right requests never move player");
            var gate=new OperationLock();p.ConfigureOperationLock(gate);gate.Lock("dialogue");p.Tick();
            int calls=avatar.Calls;p.SetDialogueFacing(false);Check(!p.IsFacingRight&&!avatar.Facing&&avatar.Calls==calls+1,"locked entry still reaches existing facing writer");
            p.Tick();Check(!p.IsFacingRight&&p.transform.position.Equals(position),"locked tick keeps dialogue facing and position");
            p.SetDialogueFacing(true);Check(p.IsFacingRight&&avatar.Facing&&p.transform.position.Equals(position),"locked right facing remains effective without movement");
            Console.WriteLine("DIALOGUE_FACING_PASS checks="+details.Count);return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
