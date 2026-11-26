using System;
using UnityEngine;

[Serializable]
public class CK01FridgeCapacityLevelData : ScriptableObject
{
    public int level_index;
    public int capacity;
    public string upgrade_condition;
}
