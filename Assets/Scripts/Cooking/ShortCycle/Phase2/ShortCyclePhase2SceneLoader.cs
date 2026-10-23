using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EatWhat.Cooking.ShortCycle
{
    public interface IShortCyclePhase2SceneLease
    {
        Phase2SceneEntry Entry { get; }
        bool IsLoaded { get; }
        void Unload(Action<string> completed);
    }
    public interface IShortCyclePhase2SceneLoader
    {
        bool CanLoad(out string error);
        void LoadAdditive(Action<IShortCyclePhase2SceneLease, string> completed);
    }
    /// <summary>Exact new scene only. Never owns/unloads a scene that was already loaded.</summary>
    public sealed class ShortCyclePhase2SceneLoader : IShortCyclePhase2SceneLoader
    {
        public const string ScenePath = "Assets/Scenes/ShortCycle_P2.unity";
        public bool CanLoad(out string error)
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isLoaded) { error = "Phase2 scene is already loaded by another owner."; return false; }
            if (SceneUtility.GetBuildIndexByScenePath(ScenePath) < 0) { error = "Approved Phase2 scene is not in the Build scene list."; return false; }
            error = null; return true;
        }
        public void LoadAdditive(Action<IShortCyclePhase2SceneLease, string> completed)
        {
            string error; if (!CanLoad(out error)) { completed(null, error); return; }
            AsyncOperation operation;
            try { operation = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive); }
            catch (Exception exception) { completed(null, exception.Message); return; }
            if (operation == null) { completed(null, "Phase2 load did not start."); return; }
            Action<AsyncOperation> finished = null;
            finished = value => {
                value.completed -= finished;
                var scene = SceneManager.GetSceneByPath(ScenePath);
                if (!scene.IsValid() || !scene.isLoaded) { completed(null, "Phase2 load finished without its scene."); return; }
                var lease = new SceneLease(scene);
                completed(lease, lease.Entry == null ? "Phase2 needs exactly one Phase2SceneEntry." : null);
            };
            if (operation.isDone) finished(operation); else operation.completed += finished;
        }
        private sealed class SceneLease : IShortCyclePhase2SceneLease
        {
            private readonly Scene scene;
            private bool unloading;
            public Phase2SceneEntry Entry { get; private set; }
            public bool IsLoaded { get { return scene.IsValid() && scene.isLoaded; } }
            public SceneLease(Scene scene)
            {
                this.scene = scene; var count = 0;
                foreach (var root in scene.GetRootGameObjects()) foreach (var entry in root.GetComponentsInChildren<Phase2SceneEntry>(true))
                { Entry = entry; count++; }
                if (count != 1) Entry = null;
            }
            public void Unload(Action<string> completed)
            {
                if (unloading) { completed("Phase2 unload is already pending."); return; }
                if (!IsLoaded) { completed(null); return; }
                AsyncOperation operation;
                try { operation = SceneManager.UnloadSceneAsync(scene); }
                catch (Exception exception) { completed(exception.Message); return; }
                if (operation == null) { completed("Phase2 unload did not start."); return; }
                unloading = true; Action<AsyncOperation> finished = null;
                finished = value => { value.completed -= finished; unloading = false; completed(IsLoaded ? "Phase2 unload left its scene loaded." : null); };
                if (operation.isDone) finished(operation); else operation.completed += finished;
            }
        }
    }
}
