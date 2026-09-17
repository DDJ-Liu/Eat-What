using System;
using System.Reflection;
using UnityEngine;

public static class CoordinateSpaceRegressionTests
{
    private const float Tolerance = 0.0001f;

    public static void Run()
    {
        VerifyLocalInnerWorldTargetAndBounds();
        VerifyColliderOffsetAndModeAxisPreservation();
        VerifyRectPivotEquivalentOffset();
        VerifyOuterWorldSpaceAndManualCompatibility();
        Console.WriteLine("COORDINATE_SPACE_REGRESSION_PASS tests=4 productionPath=true");
    }

    private static void VerifyLocalInnerWorldTargetAndBounds()
    {
        Transform parent = NewTransform(new Vector3(-30f, 5f, 0f), new Vector3(2f, 3f, 1f));
        DragContainer container = NewContainer(parent, new Vector3(1f, 2f, 0f), new Vector2(4f, 2f), new Vector2(0.25f, -0.5f));
        container.useLocalSpace = true;
        container.limitMode = DragContainer.LimitMode.DragLimit;
        container.mode = DragContainer.DragMode.Composite;
        container.dragLimit = NewLimit(new Vector3(-20f, 10f, 0f), new Vector2(4f, 2f), DragLimit.LimitType.Inner);

        container.UpdateBounds();
        AssertNear(-22.5f, container.LimitMinX, "REGRESSION_WORLD_HALF_SIZE_MIN_X");
        AssertNear(-18.5f, container.LimitMaxX, "REGRESSION_WORLD_HALF_SIZE_MAX_X");
        AssertNear(9.5f, container.LimitMinY, "REGRESSION_WORLD_HALF_SIZE_MIN_Y");
        AssertNear(13.5f, container.LimitMaxY, "REGRESSION_WORLD_HALF_SIZE_MAX_Y");

        Vector3 legalWorldTarget = new Vector3(-19f, 11f, 0f);
        container.transform.ResetWriteCounts();
        container.moveToTargetPos(legalWorldTarget);
        AssertVector(legalWorldTarget, container.transform.position, "REGRESSION_SYNC_WORLD_TARGET");
        Assert(container.transform.LocalPositionWriteCount == 1, "REGRESSION_SYNC_SINGLE_INTERNAL_WRITE");
        Assert(container.transform.WorldPositionWriteCount == 0, "REGRESSION_SYNC_NO_REDUNDANT_WORLD_WRITE");

        float normalizedY = (container.transform.position.y - container.LimitMinY) / (container.LimitMaxY - container.LimitMinY);
        AssertNear(0.375f, normalizedY, "REGRESSION_NORMALIZED_BOUNDARY");

        TransitionBehaviour_Position transition = new TransitionBehaviour_Position();
        transition.targetTransform = new Transform { parent = parent, localScale = Vector3.one };
        transition.targetTransform.position = new Vector3(-21f, 9.5f, 0f);
        transition.PlayTo(legalWorldTarget, 0.25f, false, null, null);
        AssertVector(container.transform.position, transition.targetTransform.position, "REGRESSION_SYNC_SMOOTH_WORLD_PARITY");

        object limited = InvokeLimited(container, parent.InverseTransformPoint(new Vector3(-100f, 100f, 0f)));
        bool hitBoundary = (bool)limited.GetType().GetField("Item2").GetValue(limited);
        Assert(hitBoundary, "REGRESSION_HIT_BOUNDARY");
    }

    private static void VerifyColliderOffsetAndModeAxisPreservation()
    {
        Transform parent = NewTransform(new Vector3(-30f, 5f, 0f), new Vector3(2f, 3f, 1f));
        DragContainer container = NewContainer(parent, new Vector3(1f, 2f, 7f), new Vector2(4f, 2f), new Vector2(0.25f, -0.5f));
        container.useLocalSpace = true;
        container.limitMode = DragContainer.LimitMode.DragLimit;
        container.dragLimit = NewLimit(new Vector3(-20f, 10f, 0f), new Vector2(4f, 2f), DragLimit.LimitType.Inner);
        container.UpdateBounds();

        Vector3 before = container.transform.position;
        container.mode = DragContainer.DragMode.Vertical;
        container.moveToTargetPos(new Vector3(-19f, 11f, 99f));
        AssertNear(before.x, container.transform.position.x, "REGRESSION_VERTICAL_PRESERVES_X");
        AssertNear(11f, container.transform.position.y, "REGRESSION_VERTICAL_APPLIES_WORLD_Y");
        AssertNear(before.z, container.transform.position.z, "REGRESSION_VERTICAL_PRESERVES_Z");

        before = container.transform.position;
        container.mode = DragContainer.DragMode.Horizontal;
        container.moveToTargetPos(new Vector3(-19f, 12f, 99f));
        AssertNear(-19f, container.transform.position.x, "REGRESSION_HORIZONTAL_APPLIES_WORLD_X");
        AssertNear(before.y, container.transform.position.y, "REGRESSION_HORIZONTAL_PRESERVES_Y");
        AssertNear(before.z, container.transform.position.z, "REGRESSION_HORIZONTAL_PRESERVES_Z");
    }

    private static void VerifyRectPivotEquivalentOffset()
    {
        Transform parent = NewTransform(new Vector3(10f, -4f, 0f), new Vector3(1.5f, 2f, 1f));
        DragContainer container = NewContainer(parent, Vector3.zero, new Vector2(1f, 1f), Vector2.zero);
        container.useLocalSpace = true;
        container.limitMode = DragContainer.LimitMode.DragLimit;
        container.mode = DragContainer.DragMode.Composite;
        container.sizeSource = DragContainer.SizeSource.RectTransform;
        container.sizeRect = new RectTransform
        {
            parent = parent,
            localScale = Vector3.one,
            rect = new Rect(0f, 0f, 4f, 2f),
            pivot = new Vector2(0.25f, 0.75f)
        };
        container.dragLimit = NewLimit(new Vector3(20f, 5f, 0f), new Vector2(2f, 2f), DragLimit.LimitType.Inner);
        Vector3 target = new Vector3(19f, 6f, 0f);
        container.UpdateBounds();
        container.moveToTargetPos(target);
        AssertVector(target, container.transform.position, "REGRESSION_RECT_PIVOT_WORLD_TARGET");
        AssertVector(new Vector3(20.5f, 5f, 0f), container.transform.TransformPoint(container.col.offset), "REGRESSION_RECT_PIVOT_COLLIDER_CENTER");
    }

    private static void VerifyOuterWorldSpaceAndManualCompatibility()
    {
        DragContainer worldContainer = NewContainer(null, Vector3.zero, new Vector2(2f, 2f), Vector2.zero);
        worldContainer.useLocalSpace = false;
        worldContainer.limitMode = DragContainer.LimitMode.DragLimit;
        worldContainer.mode = DragContainer.DragMode.Composite;
        worldContainer.dragLimit = NewLimit(Vector3.zero, new Vector2(10f, 8f), DragLimit.LimitType.Outer);
        worldContainer.UpdateBounds();
        worldContainer.moveToTargetPos(new Vector3(100f, -100f, 3f));
        AssertVector(new Vector3(4f, -3f, 3f), worldContainer.transform.position, "REGRESSION_OUTER_WORLD_CLAMP");

        Transform parent = NewTransform(new Vector3(10f, 20f, 0f), new Vector3(2f, 2f, 1f));
        DragContainer manual = NewContainer(parent, new Vector3(2f, 2f, 0f), new Vector2(2f, 2f), Vector2.zero);
        manual.useLocalSpace = true;
        manual.limitMode = DragContainer.LimitMode.Manual;
        manual.leftLimit = 12f;
        manual.rightLimit = 16f;
        manual.bottomLimit = 22f;
        manual.topLimit = 28f;
        manual.mode = DragContainer.DragMode.Composite;
        manual.UpdateBounds();
        manual.moveToTargetPos(new Vector3(100f, 100f, 9f));
        AssertVector(new Vector3(16f, 28f, 9f), manual.transform.position, "REGRESSION_MANUAL_LOCAL_COMPATIBILITY");
    }

    private static Transform NewTransform(Vector3 worldPosition, Vector3 localScale)
    {
        Transform transform = new Transform { localScale = localScale };
        transform.position = worldPosition;
        return transform;
    }

    private static DragContainer NewContainer(Transform parent, Vector3 localPosition, Vector2 size, Vector2 offset)
    {
        DragContainer container = new DragContainer();
        container.transform.parent = parent;
        container.transform.localScale = Vector3.one;
        container.transform.localPosition = localPosition;
        container.col = new BoxCollider2D { transform = container.transform, size = size, offset = offset };
        container.sizeSource = DragContainer.SizeSource.BoxCollider2D;
        return container;
    }

    private static DragLimit NewLimit(Vector3 worldPosition, Vector2 size, DragLimit.LimitType type)
    {
        DragLimit limit = new DragLimit();
        limit.transform.position = worldPosition;
        limit.limitType = type;
        limit.allowExceed = false;
        BoxCollider2D collider = new BoxCollider2D { transform = limit.transform, size = size, offset = Vector2.zero };
        SetField(limit, "_col", collider);
        SetField(limit, "_rect", null);
        SetField(limit, "_initialized", true);
        return limit;
    }

    private static object InvokeLimited(DragContainer container, Vector3 internalTarget)
    {
        MethodInfo method = typeof(DragContainer).GetMethod("CalculateLimitedPosition", BindingFlags.Instance | BindingFlags.NonPublic);
        return method.Invoke(container, new object[] { internalTarget });
    }

    private static void SetField(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void AssertVector(Vector3 expected, Vector3 actual, string marker)
    {
        AssertNear(expected.x, actual.x, marker + "_X");
        AssertNear(expected.y, actual.y, marker + "_Y");
        AssertNear(expected.z, actual.z, marker + "_Z");
    }

    private static void AssertNear(float expected, float actual, string marker)
    {
        if (Math.Abs(expected - actual) > Tolerance)
            throw new InvalidOperationException(marker + ": expected=" + expected + " actual=" + actual);
    }

    private static void Assert(bool condition, string marker)
    {
        if (!condition) throw new InvalidOperationException(marker);
    }
}
