using System;
using System.Collections.Generic;
using System.IO;
using EatWhat.Tools.Persistence;
using Newtonsoft.Json.Linq;
namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>The application persistence host. Core continues to see only the existing ISaveBoundary.</summary>
    public sealed class SaveBoundary : ISaveBoundary
    {
        private readonly ObservableNoOpSaveBoundary observation;
        private readonly Func<PlayerProfileManager> profile;
        private readonly Func<JsonSaveStore> storeFactory;
        private readonly Action<string> warning;
        private readonly Func<DateTime> utcNow;
        private JsonSaveStore store;
        private JObject sections = new JObject();
        private bool prepared;
        private bool warned;
        private bool storageDisabled;
        internal void DisableStorage() { storageDisabled = true; store = null; }
        private const string Slot = "default";
        public bool IsPrepared { get { return prepared; } }
        public SaveBoundary(ObservableNoOpSaveBoundary observation, Func<PlayerProfileManager> profile, Func<JsonSaveStore> storeFactory, Action<string> warning, Func<DateTime> utcNow = null)
        { this.observation = observation; this.profile = profile; this.storeFactory = storeFactory; this.warning = warning; this.utcNow = utcNow ?? (() => DateTime.UtcNow); }
        public bool PrepareBeforeSession(bool isIdle)
        {
            if (storageDisabled || !isIdle) return false;
            if (prepared) return true; // Restore once per host lifetime; do not erase live unsaved favorites on re-entry.
            var manager = profile == null ? null : profile();
            if (manager == null) return false;
            var defaults = manager.GetSnapshot(); prepared = true;
            try
            {
                store = storeFactory(); SaveEnvelope envelope; string diagnostic;
                if (store.TryRead(Slot, out envelope, out diagnostic))
                {
                    PlayerProfileSnapshot restored;
                    if (!TryProfile(envelope.Payload["player_profile"], out restored))
                    { store.BlockSlot(Slot); manager.ApplySnapshot(defaults); WarnOnce("Player profile section is invalid; original save is retained and writes are blocked."); }
                    else { sections = envelope.Payload; manager.ApplySnapshot(restored); }
                }
                else if (diagnostic != null) { manager.ApplySnapshot(defaults); WarnOnce(diagnostic); }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is InvalidOperationException || error is System.Security.Cryptography.CryptographicException)
            { store = null; manager.ApplySnapshot(defaults); WarnOnce("Save storage is unavailable; using the default profile."); }
            return true;
        }
        public void Reach(ShortCycleSavePoint savePoint, ShortCycleContext context)
        {
            if (observation != null) observation.Reach(savePoint, context);
            if (savePoint != ShortCycleSavePoint.EnterPhase0 && savePoint != ShortCycleSavePoint.Phase1ToPhase2) return;
            var manager = profile == null ? null : profile();
            if (!prepared || store == null || manager == null) return;
            try
            {
                var snapshot = manager.GetSnapshot(); var payload = (JObject)sections.DeepClone();
                payload["player_profile"] = new JObject { ["favorite_recipe_ids"] = Strings(snapshot.FavoriteRecipeIds), ["unlocked_recipe_ids"] = Strings(snapshot.UnlockedRecipeIds) };
                string diagnostic;
                if (!store.Write(Slot, SaveEnvelope.Create(payload, utcNow()), out diagnostic)) WarnOnce(diagnostic);
                else sections = payload;
            }
            catch (Exception error) when (error is ArgumentException || error is FormatException || error is Newtonsoft.Json.JsonException)
            { WarnOnce("Profile snapshot could not be saved; continuing without overwriting prior data."); }
        }
        private static JArray Strings(IEnumerable<string> ids) { var result = new JArray(); foreach (var id in ids) result.Add(id); return result; }
        private static bool TryProfile(JToken value, out PlayerProfileSnapshot snapshot)
        {
            snapshot = null; var obj = value as JObject; List<string> favorites, unlocked;
            if (obj == null || !TryIds(obj["favorite_recipe_ids"], out favorites) || !TryIds(obj["unlocked_recipe_ids"], out unlocked)) return false;
            snapshot = new PlayerProfileSnapshot(favorites, unlocked); return true;
        }
        private static bool TryIds(JToken value, out List<string> ids)
        {
            ids = new List<string>(); var array = value as JArray; if (array == null) return false;
            foreach (var id in array) { if (id.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)id)) return false; ids.Add((string)id); }
            return true;
        }
        private void WarnOnce(string diagnostic) { if (!warned) { warned = true; if (warning != null) warning(diagnostic); } }
    }
}
