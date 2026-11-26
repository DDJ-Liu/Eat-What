using UnityEngine;

public abstract class SpawnableIcon : MonoBehaviour
{
    public bool used = false;
    public GameObject mySpawned = null;

    public virtual void onSpawnedUsed()
    {
        used = false;
        mySpawned = null;
    }

    public virtual void onSpawnedTossed()
    {
        used = false;
        mySpawned = null;
    }

    public void buttonPassIn(ConditionReceiver button)
    {
        button.ReportCondition(used);
    }

    public virtual void onSpawn(string path, string prefabName, Transform parent = null)
    {
        used = true;
        GameObject spawned = Tools.LoadAndInstantiatePrefab($"{path}/{prefabName}", parent);
        spawned.GetComponent<ISpawnedPlaceable>().onSpawned(this);
        mySpawned = spawned;
    }
}
