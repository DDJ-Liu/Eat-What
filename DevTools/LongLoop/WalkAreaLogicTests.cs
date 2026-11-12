using System;
using EatWhat.LongLoop;
using EatWhat.Toolkit.Input;
using UnityEngine;

internal sealed class InputProbe : IGameInput
{
    public float Horizontal { get; set; }
    public float Vertical { get; set; }
    public Vector2 LookAxis => Vector2.zero;
    public GameInputDevice CurrentDevice => GameInputDevice.Keyboard;
    public bool Interact => false;
    public Vector2 MouseScreenPos => Vector2.zero;
}

internal sealed class AvatarProbe : IPlayerAvatar
{
    public bool FacingRight;
    public bool Moving;
    public int MoveNotices;
    public void SetFacing(bool value) { FacingRight = value; }
    public void SetMoving(bool value) { Moving = value; MoveNotices++; }
}

internal sealed class ControllerProbe : LongLoopPlayerController
{
    public void Boot() { base.Awake(); base.OnEnable(); base.Start(); }
    public void Tick() { base.Update(); }
    public void PhysicsTick() { base.FixedUpdate(); }
    public void LoseFocus() { base.OnApplicationFocus(false); }
    public void Pause() { base.OnApplicationPause(true); }
    public void Disable() { base.OnDisable(); }
}

internal static class WalkAreaLogicTests
{
    private static int count;
    private static void Check(bool value, string label)
    {
        count++;
        if (!value) throw new Exception(label);
    }
    private static void Near(float actual, float expected, string label)
    {
        count++;
        if (Math.Abs(actual - expected) > 0.0001f)
            throw new Exception(label + ": expected " + expected + ", got " + actual);
    }
    private static PolygonCollider2D Poly(params Vector2[] points)
    {
        return new PolygonCollider2D { paths = new[] { points } };
    }
    private static PolygonCollider2D Rect(float x0, float y0, float x1, float y1)
    {
        return Poly(new Vector2(x0, y0), new Vector2(x1, y0),
            new Vector2(x1, y1), new Vector2(x0, y1));
    }
    private static WalkArea Area(params PolygonCollider2D[] regions)
    {
        var area = new WalkArea();
        area.ConfigureRegions(regions);
        return area;
    }
    public static int Main()
    {
        var empty = Area();
        Check(!empty.IsUsable && !empty.Contains(new Vector2(0f, 0f)), "empty area unavailable");
        var fallback = PlanarMotion.Step(new Vector2(9f, 3f), new Vector2(1f, 1f),
            2f, 0.6f, 1f, empty, 0f, 10f);
        Near(fallback.x, 10f, "empty fallback X clamp");
        Near(fallback.y, 3f, "empty fallback preserves Y");
        var first = Rect(0f, -1f, 10f, 1f);
        var area = Area(first);
        Check(area.IsUsable && area.Contains(new Vector2(5f, 0f)), "inside first polygon");
        Check(!area.Contains(new Vector2(11f, 0f)), "outside polygon");
        var projected = area.ClosestPoint(new Vector2(11f, 0f));
        Near(projected.x, 10f, "nearest edge X");
        Near(projected.y, 0f, "nearest edge Y");
        projected = area.ClosestPoint(new Vector2(11f, 2f));
        Near(projected.x, 10f, "nearest corner X");
        Near(projected.y, 1f, "nearest corner Y");
        var second = Rect(15f, -1f, 20f, 1f);
        area.ConfigureRegions(first, second);
        Check(area.Contains(new Vector2(17f, 0f)), "union second region");
        Near(area.ClosestPoint(new Vector2(13f, 0f)).x, 15f, "union nearest region");
        var multiPath = Area(new PolygonCollider2D
        {
            paths = new[] { Rect(0f, -1f, 10f, 1f).paths[0],
                Rect(15f, -1f, 20f, 1f).paths[0] }
        });
        Check(multiPath.Contains(new Vector2(17f, 0f)),
            "second path in one collider is usable");
        Near(multiPath.ClosestPoint(new Vector2(13f, 0f)).x, 15f,
            "nearest point across paths in one collider");
        second.isActiveAndEnabled = false;
        Check(!area.Contains(new Vector2(17f, 0f)) && area.IsUsable, "disabled region omitted");
        first.isActiveAndEnabled = false;
        Check(!area.IsUsable, "all regions disabled");
        first.isActiveAndEnabled = true;
        area.isActiveAndEnabled = false;
        Check(!area.IsUsable, "disabled area unavailable");
        area.isActiveAndEnabled = true;

        var concave = Area(Poly(new Vector2(0f, 0f), new Vector2(4f, 0f),
            new Vector2(4f, 1f), new Vector2(1f, 1f), new Vector2(1f, 4f),
            new Vector2(0f, 4f)));
        Check(concave.Contains(new Vector2(0.5f, 3f)) &&
            !concave.Contains(new Vector2(2f, 2f)), "concave interior and notch");
        projected = concave.ClosestPoint(new Vector2(2f, 2f));
        Near((projected - new Vector2(2f, 2f)).sqrMagnitude, 1f,
            "concave notch nearest boundary");
        var notchSlide = PlanarMotion.Step(new Vector2(0.5f, 2f),
            new Vector2(1f, 1f), 1f, 0.6f, 1f, concave, -100f, 100f);
        Near(notchSlide.x, 1f, "concave boundary stops outward X");
        Near(notchSlide.y, 2.6f, "concave boundary keeps tangential Y");
        var rotated = Rect(0f, 0f, 2f, 1f);
        rotated.position = new Vector2(10f, 0f);
        rotated.scale = new Vector2(2f, 1f);
        rotated.rotationDegrees = 90f;
        var rotatedArea = Area(rotated);
        Check(rotatedArea.Contains(new Vector2(9.5f, 2f)), "world rotation and scale");
        Near(rotatedArea.ClosestPoint(new Vector2(11f, 2f)).x, 10f,
            "world transformed nearest edge");

        var slideArea = Area(Rect(0f, 0f, 10f, 10f));
        var step = PlanarMotion.Step(new Vector2(9f, 2f), new Vector2(1f, 1f),
            2f, 0.6f, 1f, slideArea, -100f, 100f);
        var idleOutside = PlanarMotion.Step(new Vector2(11f, 5f), Vector2.zero,
            2f, 0.6f, 1f, slideArea, -100f, 100f);
        Near(idleOutside.x, 11f, "idle outside area does not relocate saved spawn");
        Near(step.x, 10f, "wall stops outward X");
        Near(step.y, 3.2f, "wall preserves tangential Y");
        step = PlanarMotion.Step(new Vector2(10f, 3.2f), new Vector2(1f, 1f),
            2f, 0.6f, 1f, slideArea, -100f, 100f);
        Near(step.x, 10f, "wall remains bounded");
        Near(step.y, 4.4f, "wall continues sliding");
        step = PlanarMotion.Step(new Vector2(5f, 5f), new Vector2(1f, 1f),
            2f, 0.6f, 1f, slideArea, -100f, 100f);
        Near(step.x, 7f, "diagonal X is not normalized");
        Near(step.y, 6.2f, "diagonal Y uses .6 factor");
        step = PlanarMotion.Step(new Vector2(5f, 5f), new Vector2(0f, -1f),
            2f, 0.6f, 1f, slideArea, -100f, 100f);
        Near(step.y, 3.8f, "negative vertical movement");

        var avatar = new AvatarProbe();
        var controller = new ControllerProbe();
        controller.transform.position = new Vector3(5f, 5f, 17f);
        controller.MoveSpeed = 2f;
        controller.ConfigureInterfaces(null, avatar);
        controller.Boot();
        controller.ConfigureWalkArea(slideArea);
        controller.SetMoveInput(new Vector2(0f, 1f));
        Check(controller.IsMoving && avatar.Moving && avatar.FacingRight,
            "pure vertical is walking without turning");
        controller.Tick();
        Near(controller.transform.position.y, 6.2f, "Transform vertical movement");
        Near(controller.transform.position.z, 17f, "Transform Z preserved");
        Check(controller.VerticalInput == 1f && controller.MoveInput.x == 0f,
            "public two-axis input reading");
        controller.SetMoveInput(new Vector2(-1f, 0f));
        Check(!controller.IsFacingRight && !avatar.FacingRight, "only horizontal flips facing");
        controller.SetMoveInput(new Vector2(0f, -1f));
        Check(!controller.IsFacingRight && controller.IsMoving, "vertical retains left facing");
        controller.StopMovement();
        Check(controller.HorizontalInput == 0f && controller.VerticalInput == 0f && !avatar.Moving,
            "Stop clears both axes and animation");
        controller.ConfigureWalkArea(empty);
        controller.SetHorizontalBounds(0f, 6f);
        controller.SetHorizontalInput(1f);
        controller.Tick();
        Near(controller.transform.position.x, 6f, "legacy horizontal bound fallback");
        Near(controller.transform.position.y, 6.2f, "fallback does not move Y");
        controller.SetMoveInput(new Vector2(0f, 1f));
        Check(!controller.IsMoving, "vertical input without area cannot animate movement");
        controller.Tick();
        Near(controller.transform.position.y, 6.2f, "vertical input without area stays put");
        controller.StopMovement();
        controller.ConfigureBoundaries(
            new Transform { position = new Vector3(6f, 0f, 0f) },
            new Transform { position = new Vector3(0f, 0f, 0f) });
        controller.SetHorizontalInput(-1f);
        controller.Tick();
        Near(controller.transform.position.x, 4f,
            "reversed legacy boundary transforms remain sorted");

        var input = new InputProbe { Horizontal = 0f, Vertical = -1f };
        var providerController = new ControllerProbe();
        providerController.transform.position = new Vector3(5f, 5f, 3f);
        providerController.MoveSpeed = 2f;
        providerController.ConfigureInterfaces(input, new AvatarProbe());
        providerController.Boot();
        providerController.ConfigureWalkArea(slideArea);
        providerController.Tick();
        Near(providerController.transform.position.y, 3.8f, "Update reads IGameInput.Vertical");
        providerController.LoseFocus();
        Check(providerController.MoveInput == Vector2.zero, "focus loss clears both axes");
        providerController.Pause();
        Check(providerController.MoveInput == Vector2.zero, "pause clears both axes");
        providerController.Disable();
        Check(providerController.MoveInput == Vector2.zero, "disable clears both axes");

        var rb = new Rigidbody2D { position = new Vector2(5f, 5f),
            constraints = RigidbodyConstraints2D.FreezePositionX };
        var physics = new ControllerProbe { attachedRigidbody = rb };
        physics.MoveSpeed = 2f;
        physics.Boot();
        Check((rb.constraints & RigidbodyConstraints2D.FreezePositionY) != 0,
            "fallback owns Y freeze");
        physics.ConfigureWalkArea(slideArea);
        Check((rb.constraints & RigidbodyConstraints2D.FreezePositionY) == 0 &&
            (rb.constraints & RigidbodyConstraints2D.FreezePositionX) != 0,
            "2D area releases own Y freeze, preserves external X");
        physics.SetMoveInput(new Vector2(1f, 0f));
        Check(!physics.IsMoving, "external X freeze does not report walking");
        physics.SetMoveInput(new Vector2(0f, 1f));
        Time.fixedDeltaTime = 0.5f;
        physics.PhysicsTick();
        Near(rb.position.y, 5.6f, "Rigidbody vertical MovePosition");
        Check(rb.moveCalls == 1, "Rigidbody path used");
        physics.StopMovement();
        Near(rb.linearVelocity.y, 0f, "owned vertical velocity stopped");
        Check(physics.MoveInput == Vector2.zero, "Rigidbody input stopped");
        slideArea.isActiveAndEnabled = false;
        physics.Tick();
        Check((rb.constraints & RigidbodyConstraints2D.FreezePositionY) != 0,
            "area disabled restores own Y freeze");
        slideArea.isActiveAndEnabled = true;
        physics.Tick();
        Check((rb.constraints & RigidbodyConstraints2D.FreezePositionY) == 0,
            "area reenabled releases own Y freeze");
        physics.Disable();
        Check(rb.constraints == RigidbodyConstraints2D.FreezePositionX,
            "disable restores captured external constraints");

        var external = new Rigidbody2D { position = new Vector2(5f, 5f),
            constraints = RigidbodyConstraints2D.FreezePositionY };
        var frozen = new ControllerProbe { attachedRigidbody = external };
        frozen.Boot();
        frozen.ConfigureWalkArea(slideArea);
        frozen.SetMoveInput(new Vector2(0f, 1f));
        Check(!frozen.IsMoving, "external initial Y freeze prevents fake vertical movement");
        frozen.PhysicsTick();
        Near(external.position.y, 5f, "external initial Y freeze respected");
        frozen.Disable();
        Check(external.constraints == RigidbodyConstraints2D.FreezePositionY,
            "external initial Y freeze restored exactly");

        var driftBody = new Rigidbody2D { position = new Vector2(5f, 5f),
            constraints = RigidbodyConstraints2D.None };
        var drift = new ControllerProbe { attachedRigidbody = driftBody };
        drift.Boot();
        driftBody.constraints = RigidbodyConstraints2D.FreezePositionX;
        drift.ConfigureWalkArea(slideArea);
        Check(driftBody.constraints == RigidbodyConstraints2D.FreezePositionX,
            "unexpected runtime constraint change is not overwritten on mode switch");
        drift.Disable();
        Check(driftBody.constraints == RigidbodyConstraints2D.FreezePositionX,
            "unexpected runtime constraint change is not overwritten on disable");

        Console.WriteLine("PASS " + count + " actual-source walk assertions");
        return 0;
    }
}
