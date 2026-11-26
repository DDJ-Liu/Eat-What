using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TransitionVisibilityTarget
{
    public GameObject target;
    public bool visible = true;
}

[Serializable]
public sealed class TransitionPreset
{
    public string name;
    public bool applyPosition;
    public Vector3 position;
    public TransitionCoordinateSpace positionSpace = TransitionCoordinateSpace.Local;
    public bool applyScale;
    public Vector3 localScale = Vector3.one;
    public bool applyAlpha;
    public float alpha = 1f;
    public List<TransitionVisibilityTarget> visibility = new List<TransitionVisibilityTarget>();
}
