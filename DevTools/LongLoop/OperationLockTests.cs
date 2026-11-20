using System;
using System.Collections.Generic;
using EatWhat.LongLoop;
using EatWhat.Narrative.Core;
using EatWhat.Toolkit.Input;
using UnityEngine;

internal static class OperationLockTests
{
    static readonly List<string> details = new List<string>();
    static void Check(bool value, string name) { if (!value) throw new Exception("Operation lock: " + name); details.Add(name); }
    static void Near(float value, float expected, string name) { Check(Math.Abs(value - expected) < .0001f, name); }
    sealed class Input : IGameInput
    {
        public int Reads; public float X, Y;
        public float Horizontal { get { Reads++; return X; } }
        public float Vertical { get { Reads++; return Y; } }
        public Vector2 LookAxis { get { return Vector2.zero; } }
        public GameInputDevice CurrentDevice { get { return GameInputDevice.Keyboard; } }
        public bool Interact { get { return false; } }
        public Vector2 MouseScreenPos { get { return Vector2.zero; } }
    }
    sealed class Avatar : IPlayerAvatar
    {
        public bool Moving, Facing; public int FacingCalls;
        public void SetMoving(bool value) { Moving = value; }
        public void SetFacing(bool value) { Facing = value; FacingCalls++; }
    }
    sealed class Player : LongLoopPlayerController
    {
        public int Stops;
        public void Boot() { base.Awake(); base.OnEnable(); base.Start(); }
        public void Frame() { base.Update(); }
        public void Physics() { base.FixedUpdate(); }
        public override void StopMovement() { Stops++; base.StopMovement(); }
    }
    static void Invalid(Action work, string name)
    { bool rejected = false; try { work(); } catch (ArgumentException) { rejected = true; } Check(rejected, name); }
    static int Main()
    {
        var gate = new OperationLock(); int locks = 0, unlocks = 0;
        gate.Locked += () => { Check(gate.IsLocked && gate.HolderCount == 1, "locked event observes committed first holder"); locks++; };
        gate.Unlocked += () => { Check(!gate.IsLocked && gate.HolderCount == 0, "unlocked event observes empty holders"); unlocks++; };
        Check(!gate.IsLocked && gate.HolderCount == 0, "initial state unlocked");
        Check(gate.Lock("dialogue") && gate.IsLocked && gate.HolderCount == 1, "first owner locks");
        Check(!gate.Lock("dialogue") && gate.HolderCount == 1 && locks == 1, "duplicate owner does not stack or reemit");
        Check(gate.Lock("scene-switch") && gate.HolderCount == 2 && locks == 1, "second owner shares existing lock");
        Check(!gate.Unlock("missing") && gate.HolderCount == 2 && unlocks == 0, "unknown unlock changes no state");
        Check(gate.LastDiagnostic != null && gate.LastDiagnostic.Contains("missing"), "unknown unlock has named diagnostic");
        Check(gate.Unlock("dialogue") && gate.IsLocked && gate.HolderCount == 1 && unlocks == 0, "releasing one owner cannot unlock other");
        Check(gate.Unlock("scene-switch") && !gate.IsLocked && unlocks == 1, "last owner unlocks once");
        Check(!gate.Unlock("scene-switch") && unlocks == 1, "repeated release diagnostic without event");
        INarrativeDialogueLock capability = gate;
        capability.Acquire("dlg_actual", 1); capability.Acquire("dlg_actual", 2);
        Check(gate.HolderCount == 1 && gate.IsHeldBy("dlg_actual"), "actual FSM interface uses same owner set");
        capability.Release("dlg_actual", 3); Check(!gate.IsLocked && locks == 2 && unlocks == 2, "actual FSM interface releases shared state");
        gate.Lock("A"); gate.Lock("a"); Check(gate.HolderCount == 2, "owner names ordinal and case-sensitive");
        gate.Unlock("A"); Check(gate.IsHeldBy("a") && !gate.IsHeldBy("A"), "exact named release preserves other case"); gate.Unlock("a");
        Invalid(() => gate.Lock(""), "empty owner rejected"); Invalid(() => gate.Unlock(null), "null owner rejected");
        Check(!gate.IsLocked, "invalid names preserve unlocked state");

        Time.deltaTime = .25f; Time.fixedDeltaTime = .25f;
        var input = new Input { X = 1 }; var avatar = new Avatar(); var player = new Player();
        player.ConfigureInterfaces(input, avatar); player.Boot(); player.Frame();
        Check(input.Reads == 2 && player.IsMoving && avatar.Moving, "unbound player retains original input and walk path");
        Near(player.transform.position.x, 1, "unbound player moves normally");
        player.ConfigureOperationLock(gate); gate.Lock("dialogue"); input.X = -1; input.Y = 1;
        int reads = input.Reads, faces = avatar.FacingCalls, stops = player.Stops;
        float x = player.transform.position.x, y = player.transform.position.y;
        bool facing = player.IsFacingRight;
        player.Frame(); player.Physics();
        Check(input.Reads == reads, "locked Update never reads movement provider");
        Check(player.Stops == stops + 1, "locked Update uses existing StopMovement");
        Check(player.MoveInput == Vector2.zero && !player.IsMoving && !avatar.Moving, "lock clears cached axes and returns avatar idle");
        Near(player.transform.position.x, x, "locked transform x unchanged"); Near(player.transform.position.y, y, "locked transform y unchanged");
        Check(player.IsFacingRight == facing && avatar.FacingCalls == faces, "locked input cannot flip facing");
        gate.Lock("close-up"); gate.Unlock("dialogue"); player.Frame();
        Check(input.Reads == reads && !avatar.Moving, "remaining general owner still blocks input");
        gate.Unlock("close-up"); player.Frame();
        Check(input.Reads == reads + 2 && !player.IsFacingRight && avatar.Moving, "final unlock resumes input and normal facing");
        Check(player.transform.position.x < x, "unlocked transform resumes movement");
        player.ConfigureOperationLock(null); gate.Lock("unbound-owner"); input.X = 1; player.Frame();
        Check(player.IsFacingRight && avatar.Moving, "disconnected lock remains compatible with old fixture behavior"); gate.Unlock("unbound-owner");

        var body = new Rigidbody2D(); var bodyInput = new Input { X = 1 }; var bodyAvatar = new Avatar(); var physicsPlayer = new Player { attachedRigidbody = body };
        physicsPlayer.ConfigureOperationLock(gate); physicsPlayer.ConfigureInterfaces(bodyInput, bodyAvatar); physicsPlayer.Boot();
        physicsPlayer.Frame(); physicsPlayer.Physics(); Check(body.position.x > 0 && bodyAvatar.Moving, "rigidbody original movement active before lock");
        gate.Lock("physics-dialogue"); bodyInput.X = -1; reads = bodyInput.Reads; faces = bodyAvatar.FacingCalls;
        x = body.position.x; y = body.position.y; physicsPlayer.Frame(); physicsPlayer.Physics();
        Check(bodyInput.Reads == reads && !bodyAvatar.Moving, "locked rigidbody player skips input and idles");
        Near(body.position.x, x, "locked rigidbody x unchanged after original stop"); Near(body.position.y, y, "locked rigidbody y unchanged");
        Near(body.linearVelocity.x, 0, "existing stop clears owned horizontal velocity");
        Check(bodyAvatar.FacingCalls == faces, "locked rigidbody facing unchanged");
        gate.Unlock("physics-dialogue"); physicsPlayer.Frame(); physicsPlayer.Physics();
        Check(body.position.x < x && !physicsPlayer.IsFacingRight && bodyAvatar.Moving, "rigidbody resumes through original path after unlock");
        Console.WriteLine("OPERATION_LOCK_RESULT={\"passed\":true,\"checks\":" + details.Count + ",\"actualLockControllerInterface\":true,\"nativeUnity\":false,\"details\":[\"" + string.Join("\",\"", details.ToArray()) + "\"]}");
        return 0;
    }
}
