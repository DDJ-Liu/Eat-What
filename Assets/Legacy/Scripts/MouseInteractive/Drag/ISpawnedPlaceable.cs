public interface ISpawnedPlaceable
{
    void onSpawned(SpawnableIcon icon);
    void onPickUp();
    void onTossed();
    void onPlaceRight();
    void onPlaceWrong();
    void onPlaceCancel();
}
