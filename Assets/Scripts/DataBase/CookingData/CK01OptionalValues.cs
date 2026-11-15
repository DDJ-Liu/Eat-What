using System;

// Unity-serializable presence for new table cells. An empty cell differs from an explicit zero/false.
[Serializable]
public struct CK01OptionalFloat
{
    public bool hasValue;
    public float value;
    public CK01OptionalFloat(float value) { hasValue = true; this.value = value; }
}
[Serializable]
public struct CK01OptionalInt
{
    public bool hasValue;
    public int value;
    public CK01OptionalInt(int value) { hasValue = true; this.value = value; }
}
[Serializable]
public struct CK01OptionalBool
{
    public bool hasValue;
    public bool value;
    public CK01OptionalBool(bool value) { hasValue = true; this.value = value; }
}
