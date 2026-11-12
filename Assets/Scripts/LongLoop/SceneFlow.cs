using System;
using EatWhat.Toolkit.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EatWhat.LongLoop
{
    /// <summary>Owns only a Street scene loaded from Boot by this component.</summary>
    [DisallowMultipleComponent]
    public sealed class SceneFlow : MonoBehaviour
    {
        public const string BootSceneId = "Boot";
        public const string StreetSceneId = "LongLoop_Street";
        public const string BootScenePath = "Assets/Scenes/LongLoop/Boot.unity";
        public const string StreetScenePath = "Assets/Scenes/LongLoop/LongLoop_Street.unity";

        [SerializeField] private GameObject bootPresentationRoot_TUNE;
        [SerializeField] private Camera bootCamera_TUNE;
        [SerializeField] private MonoBehaviour navigationInputProvider;

        private enum FlowState { Boot, Loading, Street, Unloading }
        private FlowState state;
        private Scene bootScene;
        private Scene ownedStreet;
        private AsyncOperation loadOperation;
        private AsyncOperation unloadOperation;
        private INavigationInput navigationInput;
        private bool presentationWasActive;
        private bool cameraWasEnabled;
        private bool presentationWasHidden;
        private bool destroyed;

        private void Awake()
        {
            bootScene = gameObject.scene;
            navigationInput = navigationInputProvider as INavigationInput;
        }

        private void Update()
        {
            if (state == FlowState.Street && navigationInput != null && navigationInput.BackPressed)
                Back();
        }

        /// <summary>Bind this method to Button_MouseInteract.selectEvent.</summary>
        public void StartGame() { Go(StreetSceneId); }

        public void Go(string sceneId)
        {
            if (sceneId == BootSceneId) { Back(); return; }
            if (sceneId != StreetSceneId)
            {
                Debug.LogWarning("SceneFlow: scene ID is outside the Boot/Street allowlist.", this);
                return;
            }
            if (destroyed || state != FlowState.Boot) return;
            if (!IsBootReady()) return;

            presentationWasActive = bootPresentationRoot_TUNE.activeSelf;
            cameraWasEnabled = bootCamera_TUNE.enabled;
            presentationWasHidden = false;

            AsyncOperation operation;
            state = FlowState.Loading;
            try { operation = SceneManager.LoadSceneAsync(StreetScenePath, LoadSceneMode.Additive); }
            catch (Exception exception)
            {
                state = FlowState.Boot;
                Debug.LogError("SceneFlow: Street load failed: " + exception, this);
                return;
            }
            if (operation == null)
            {
                state = FlowState.Boot;
                Debug.LogError("SceneFlow: Street load returned no operation.", this);
                return;
            }
            loadOperation = operation;
            operation.completed += OnLoadCompleted;
            if (operation.isDone) OnLoadCompleted(operation);
        }

        public void Back()
        {
            if (destroyed || state != FlowState.Street) return;
            BeginUnload();
        }

        private bool IsBootReady()
        {
            if (!bootScene.IsValid() || !bootScene.isLoaded || bootScene.path != BootScenePath ||
                navigationInput == null || bootPresentationRoot_TUNE == null || bootCamera_TUNE == null ||
                transform.IsChildOf(bootPresentationRoot_TUNE.transform) ||
                navigationInputProvider.transform.IsChildOf(bootPresentationRoot_TUNE.transform) ||
                SceneUtility.GetBuildIndexByScenePath(StreetScenePath) < 0 ||
                SceneManager.GetSceneByPath(StreetScenePath).isLoaded)
            {
                Debug.LogError("SceneFlow: Boot bindings, Build entry, or Street ownership are invalid.", this);
                return false;
            }
            return true;
        }

        private void OnLoadCompleted(AsyncOperation operation)
        {
            if (loadOperation != operation) return;
            operation.completed -= OnLoadCompleted;
            loadOperation = null;
            Scene street = SceneManager.GetSceneByPath(StreetScenePath);
            if (!street.IsValid() || !street.isLoaded)
            {
                state = FlowState.Boot;
                Debug.LogError("SceneFlow: Street load completed without a loaded scene.", this);
                return;
            }
            ownedStreet = street;
            if (destroyed)
            {
                UnloadAfterDestroy();
                return;
            }
            if (!bootScene.IsValid() || !bootScene.isLoaded || bootPresentationRoot_TUNE == null ||
                bootCamera_TUNE == null || navigationInputProvider == null)
            {
                Debug.LogError("SceneFlow: Boot owner or presentation was lost during load; unloading Street.", this);
                state = FlowState.Street;
                BeginUnload();
                return;
            }
            if (!SceneManager.SetActiveScene(street))
            {
                Debug.LogError("SceneFlow: could not activate Street; unloading it.", this);
                state = FlowState.Street;
                BeginUnload();
                return;
            }
            bootPresentationRoot_TUNE.SetActive(false);
            bootCamera_TUNE.enabled = false;
            presentationWasHidden = true;
            state = FlowState.Street;
        }

        private void BeginUnload()
        {
            if (!ownedStreet.IsValid() || !ownedStreet.isLoaded)
            {
                FinishReturn();
                return;
            }
            AsyncOperation operation;
            state = FlowState.Unloading;
            try { operation = SceneManager.UnloadSceneAsync(ownedStreet); }
            catch (Exception exception)
            {
                state = FlowState.Street;
                Debug.LogError("SceneFlow: Street unload failed: " + exception, this);
                return;
            }
            if (operation == null)
            {
                state = FlowState.Street;
                Debug.LogError("SceneFlow: Street unload returned no operation.", this);
                return;
            }
            unloadOperation = operation;
            operation.completed += OnUnloadCompleted;
            if (operation.isDone) OnUnloadCompleted(operation);
        }

        private void OnUnloadCompleted(AsyncOperation operation)
        {
            if (unloadOperation != operation) return;
            operation.completed -= OnUnloadCompleted;
            unloadOperation = null;
            if (destroyed) return;
            if (ownedStreet.IsValid() && ownedStreet.isLoaded)
            {
                state = FlowState.Street;
                Debug.LogError("SceneFlow: Street remains loaded; Back can retry.", this);
                return;
            }
            FinishReturn();
        }

        private void FinishReturn()
        {
            ownedStreet = default;
            bool bootActive = false;
            if (!bootScene.IsValid() || !bootScene.isLoaded)
            {
                Debug.LogError("SceneFlow: Boot is invalid or not loaded after Street unload.", this);
            }
            else
            {
                Scene active = SceneManager.GetActiveScene();
                if (active != bootScene)
                {
                    bool activationReturned = false;
                    bool activationThrew = false;
                    try { activationReturned = SceneManager.SetActiveScene(bootScene); }
                    catch (Exception exception)
                    {
                        activationThrew = true;
                        Debug.LogError("SceneFlow: Boot activation threw: " + exception, this);
                    }
                    active = SceneManager.GetActiveScene();
                    if (active != bootScene && !activationThrew)
                        Debug.LogError("SceneFlow: Boot activation did not reach its target (returned " +
                            activationReturned + ", active=" + active.path + ").", this);
                }
                bootActive = active == bootScene;
            }
            if (presentationWasHidden)
            {
                if (bootPresentationRoot_TUNE != null) bootPresentationRoot_TUNE.SetActive(presentationWasActive);
                if (bootCamera_TUNE != null) bootCamera_TUNE.enabled = cameraWasEnabled;
                presentationWasHidden = false;
            }
            // Keep Back retryable and block a new Start if Boot activation was not achieved.
            state = bootActive ? FlowState.Boot : FlowState.Street;
        }

        private void OnDestroy()
        {
            destroyed = true;
            if (unloadOperation != null) unloadOperation.completed -= OnUnloadCompleted;
            if (ownedStreet.IsValid() && ownedStreet.isLoaded && unloadOperation == null)
                UnloadAfterDestroy();
            // A pending load keeps its one completion callback so the newly loaded scene can be released.
        }

        private void UnloadAfterDestroy()
        {
            if (!ownedStreet.IsValid() || !ownedStreet.isLoaded) return;
            try { SceneManager.UnloadSceneAsync(ownedStreet); }
            catch (Exception exception) { Debug.LogError("SceneFlow: owner cleanup failed: " + exception); }
            ownedStreet = default;
        }
    }
}
