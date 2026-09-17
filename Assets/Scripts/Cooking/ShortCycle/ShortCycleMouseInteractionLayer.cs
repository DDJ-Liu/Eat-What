namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Serialization-compatibility alias for the C-T2 scene. New ShortCycle
    /// wiring uses the project MouseInteractionLayer directly; C-T5 removes this
    /// alias from the scene after migrating the serialized references.
    /// </summary>
    [System.Obsolete("Use MouseInteractionLayer directly. This alias exists only for C-T2 scene migration.")]
    public sealed class ShortCycleMouseInteractionLayer : MouseInteractionLayer { }
}
