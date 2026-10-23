using System;
using System.Collections.Generic;
using System.IO;
using EatWhat.Tools.Persistence;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Explicit acceptance-only lease. Install before target Awake; no default-path probing.</summary>
    public sealed class ShortCycleSaveIsolation : IDisposable
    {
        private static ShortCycleSaveIsolation active;
        private static readonly HashSet<string> cancelledScenes = new HashSet<string>(StringComparer.Ordinal);
        private ShortCycleSessionManager plannedTarget;
        private ShortCycleSessionManager boundTarget;
        private readonly string scenePath;
        private readonly bool instanceTarget;
        private readonly Scene installedScene;
        private readonly string targetPath;
        private readonly string root;
        private Func<JsonSaveStore> factory;
        private bool disposed;
        public bool IsActive { get { return !disposed; } }
        private ShortCycleSaveIsolation(ShortCycleSessionManager target, string scene, string path, string root, Func<JsonSaveStore> factory)
        {
            instanceTarget = target != null; installedScene = target == null ? default(Scene) : target.gameObject.scene;
            plannedTarget = target; scenePath = scene; targetPath = path; this.root = root; this.factory = factory;
            active = this; SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        public static ShortCycleSaveIsolation ForTarget(ShortCycleSessionManager target, string absoluteRoot, Func<JsonSaveStore> factory)
        {
            if (target == null || target.PersistenceAwakeStarted) throw new InvalidOperationException("Install isolation on the exact target before Awake.");
            CheckAvailable(); var root = ValidateRoot(absoluteRoot, factory);
            target.RequirePersistenceIsolation();
            return new ShortCycleSaveIsolation(target, null, null, root, factory);
        }
        // For a fresh synchronous load. A startup BeforeSceneLoad bootstrap uses ForTarget
        // on its exact not-yet-awakened instance. Never install in sceneLoaded.
        public static ShortCycleSaveIsolation ForScene(string scenePath, string hierarchyPath, string absoluteRoot, Func<JsonSaveStore> factory)
        {
            if (string.IsNullOrEmpty(scenePath) || !scenePath.StartsWith("Assets/", StringComparison.Ordinal) || !scenePath.EndsWith(".unity", StringComparison.Ordinal) || scenePath.Contains("..") || scenePath.Contains("\\") ||
                string.IsNullOrEmpty(hierarchyPath) || hierarchyPath.StartsWith("/", StringComparison.Ordinal) || hierarchyPath.EndsWith("/", StringComparison.Ordinal) || hierarchyPath.Contains("//"))
                throw new ArgumentException("Exact project scene and root-to-target hierarchy paths are required.");
            if (SceneManager.GetSceneByPath(scenePath).isLoaded || cancelledScenes.Contains(scenePath))
                throw new InvalidOperationException("Install before loading a fresh target scene; cancelled loads require unload or a new Play lifecycle.");
            CheckAvailable(); return new ShortCycleSaveIsolation(null, scenePath, hierarchyPath, ValidateRoot(absoluteRoot, factory), factory);
        }
        private static void CheckAvailable()
        {
            if (active != null) throw new InvalidOperationException("An isolation lease is already installed.");
            foreach (var session in Resources.FindObjectsOfTypeAll<ShortCycleSessionManager>())
                if (session != null && session.PersistenceAwakeStarted && !session.PersistenceEnded)
                    throw new InvalidOperationException("Destroy/unload existing awakened sessions before installing isolation.");
        }
        private static string ValidateRoot(string root, Func<JsonSaveStore> factory)
        {
            if (factory == null || string.IsNullOrWhiteSpace(root) || !Path.IsPathFullyQualified(root)) throw new ArgumentException("An explicit absolute isolated root and store factory are required.");
            var full = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (full.Length < 4) throw new ArgumentException("A non-root isolated directory is required.");
            return full;
        }
        internal static Func<JsonSaveStore> Resolve(ShortCycleSessionManager session, Func<JsonSaveStore> productionFactory)
        {
            if (cancelledScenes.Contains(session.gameObject.scene.path ?? string.Empty)) throw new InvalidOperationException("Cancelled isolated scene cannot fall back to production storage.");
            var lease = active;
            if (lease == null)
            {
                if (session.PersistenceIsolationRequired) throw new InvalidOperationException("The target isolation lease has ended; recreate the target.");
                return productionFactory;
            }
            if (lease.disposed || lease.boundTarget != null ||
                (lease.plannedTarget != null ? !ReferenceEquals(lease.plannedTarget, session) :
                 session.gameObject.scene.path != lease.scenePath || HierarchyPath(session.transform) != lease.targetPath))
                throw new InvalidOperationException("Awake target does not match the unique installed isolation lease.");
            lease.boundTarget = session; session.RequirePersistenceIsolation();
            return () => lease.CreateStore(session);
        }
        private JsonSaveStore CreateStore(ShortCycleSessionManager session)
        {
            if (disposed || !ReferenceEquals(session, boundTarget)) throw new InvalidOperationException("Isolation lease is no longer valid.");
            try
            {
                var store = factory();
                if (store == null || !string.Equals(store.RootPath, root, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Store factory did not return the declared isolated root.");
                return store;
            }
            catch { Dispose(); throw; }
        }
        private static string HierarchyPath(Transform target)
        {
            var parts = new List<string>();
            for (var current = target; current != null; current = current.parent)
            {
                var name = current.gameObject.name;
                if (string.IsNullOrEmpty(name) || name.Contains("/")) throw new InvalidOperationException("Isolation target names must be nonempty and unambiguous.");
                parts.Insert(0, name);
            }
            return string.Join("/", parts);
        }
        internal static void ReleaseTarget(ShortCycleSessionManager target)
        {
            var lease = active;
            if (lease != null && (ReferenceEquals(lease.boundTarget, target) || ReferenceEquals(lease.plannedTarget, target))) lease.Dispose();
        }
        private void OnSceneUnloaded(Scene scene)
        { if (scenePath != null && scene.path == scenePath || instanceTarget && installedScene == scene) { Dispose(); ClearCancelledScene(scene); } }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true; SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (scenePath != null && boundTarget == null)
            {
                cancelledScenes.Add(scenePath);
                SceneManager.sceneUnloaded -= ClearCancelledScene; SceneManager.sceneUnloaded += ClearCancelledScene;
            }
            if (boundTarget != null) boundTarget.DisableIsolatedStorage();
            factory = null; plannedTarget = null; boundTarget = null;
            if (ReferenceEquals(active, this)) active = null;
        }
        private static void ClearCancelledScene(Scene scene)
        { cancelledScenes.Remove(scene.path); if (cancelledScenes.Count == 0) SceneManager.sceneUnloaded -= ClearCancelledScene; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLifecycle()
        {
            if (active != null) active.Dispose();
            cancelledScenes.Clear(); SceneManager.sceneUnloaded -= ClearCancelledScene;
        }
    }
}
