using UnityEngine;

public class ToolEditIcon : SpawnableIcon
{
    public ToolData data;
    public string ToolName;

    public void onSpawnTool()
    {
        onSpawn("Tool", ToolName);
    }
}
