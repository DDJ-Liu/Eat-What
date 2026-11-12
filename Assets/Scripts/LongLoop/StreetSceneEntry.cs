using UnityEngine;

namespace EatWhat.LongLoop
{
    /// <summary>Explicit street player bindings. The scene and TUNE markers remain authored in Editor.</summary>
    public sealed class StreetSceneEntry : MonoBehaviour
    {
        [SerializeField] private LongLoopPlayerController playerController;
        [SerializeField] private Transform playerSpawn_TUNE;
        [SerializeField] private Transform boundaryL_TUNE;
        [SerializeField] private Transform boundaryR_TUNE;
        [SerializeField] private MonoBehaviour gameInputProvider;
        [SerializeField] private MonoBehaviour playerAvatar;
        [SerializeField] private bool placePlayerAtSpawnOnStart = true;

        private void Start()
        {
            if (playerController == null)
            {
                Debug.LogWarning("StreetSceneEntry: player controller is not bound.", this);
                return;
            }
            var input = gameInputProvider as EatWhat.Toolkit.Input.IGameInput;
            var avatar = playerAvatar as IPlayerAvatar;
            if (input == null || avatar == null)
            {
                Debug.LogWarning("StreetSceneEntry: input provider or player avatar is not bound.", this);
                return;
            }
            if (boundaryL_TUNE == null || boundaryR_TUNE == null ||
                (placePlayerAtSpawnOnStart && playerSpawn_TUNE == null))
            {
                Debug.LogWarning("StreetSceneEntry: boundary or required spawn marker is not bound.", this);
                return;
            }
            playerController.ConfigureInterfaces(input, avatar);
            playerController.ConfigureBoundaries(boundaryL_TUNE, boundaryR_TUNE);
            if (placePlayerAtSpawnOnStart)
                playerController.PlaceAtSpawn(playerSpawn_TUNE);
        }
    }
}
