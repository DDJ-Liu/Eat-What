using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>
    /// Runtime-only state shared by the P0/P1 short loop. It intentionally holds
    /// no persistent inventory mutation: consumption is deferred to P1 -> P2.
    /// </summary>
    [Serializable]
    public sealed class ShortCycleContext
    {
        public string current_recipe_id;
        public ShortCycleTriggerContext trigger_context;
        public bool is_mismatch_dish;
        public List<PrepZoneItem> prep_zone_contents = new List<PrepZoneItem>();
        public List<PlaceholderState> placeholder_states = new List<PlaceholderState>();
        public string current_filter_tag;
        public float current_scroll_offset;
        public ShortCyclePhase current_phase;
        public ShortCyclePhase0View phase0_view;
        public ShortCycleModalId? active_modal;
        public bool HasSelectedRecipe { get; private set; }

        public void Enter(string recipeId, ShortCycleTriggerContext triggerContext)
        {
            ClearAll();
            // A supplied ID is an entry hint for later catalog highlighting. It
            // does not become a player selection until SelectRecipe succeeds.
            current_recipe_id = recipeId;
            trigger_context = triggerContext;
            current_phase = ShortCyclePhase.Phase0;
            phase0_view = ShortCyclePhase0View.Cover;
            active_modal = null;
        }

        /// <summary>
        /// Switches the selected recipe while remaining in P0. A different dish
        /// clears every P1-only value without a confirmation dialog (G6).
        /// </summary>
        public bool SelectRecipe(string recipeId, bool isMismatchDish)
        {
            if (current_phase != ShortCyclePhase.Phase0 || string.IsNullOrEmpty(recipeId))
            {
                return false;
            }

            if (!string.Equals(current_recipe_id, recipeId, StringComparison.Ordinal))
            {
                ClearPhase1State();
            }

            current_recipe_id = recipeId;
            is_mismatch_dish = isMismatchDish;
            HasSelectedRecipe = true;
            return true;
        }

        public bool EnterPhase1()
        {
            if (current_phase != ShortCyclePhase.Phase0 || !HasSelectedRecipe ||
                string.IsNullOrEmpty(current_recipe_id))
            {
                return false;
            }

            current_phase = ShortCyclePhase.Phase1;
            return true;
        }

        public bool ReturnToPhase0()
        {
            if (current_phase != ShortCyclePhase.Phase1)
            {
                return false;
            }

            // P1 -> P0 intentionally retains prep, placeholders, filtering and
            // scroll position. Returning is navigation, not a cancellation.
            current_phase = ShortCyclePhase.Phase0;
            return true;
        }

        public bool SetPhase0View(ShortCyclePhase0View view)
        {
            if (current_phase != ShortCyclePhase.Phase0)
            {
                return false;
            }

            phase0_view = view;
            return true;
        }

        public void CompletePhase2()
        {
            current_phase = ShortCyclePhase.Phase2;
            placeholder_states.Clear();
            current_filter_tag = null;
            // prep_zone_contents and current_recipe_id are deliberate handoff
            // state and must remain available to Phase 2.
        }

        public void ExitShortCycle()
        {
            ClearAll();
        }

        public ShortCycleDataHandoff CreatePhase2Handoff()
        {
            var copiedPrepItems = new List<PrepZoneItem>();
            foreach (var item in prep_zone_contents)
            {
                if (item != null)
                {
                    copiedPrepItems.Add(item.Clone());
                }
            }

            return new ShortCycleDataHandoff(current_recipe_id, copiedPrepItems);
        }

        public bool TryResolveCurrentRecipe(
            CK01GeneratedDataCatalog dataCatalog,
            out CK01RecipeData recipe,
            out string diagnostic)
        {
            recipe = null;
            if (dataCatalog == null)
            {
                diagnostic = "CK01 generated data catalog is not connected.";
                return false;
            }

            return dataCatalog.TryGetRecipe(current_recipe_id, out recipe, out diagnostic);
        }

        public IList<PlaceholderState> CopyPlaceholderStates()
        {
            var copiedStates = new List<PlaceholderState>();
            foreach (var state in placeholder_states)
            {
                if (state != null)
                {
                    copiedStates.Add(state.Clone());
                }
            }

            return copiedStates;
        }

        public void ClearPhase1State()
        {
            prep_zone_contents.Clear();
            placeholder_states.Clear();
            current_filter_tag = null;
            current_scroll_offset = 0f;
            is_mismatch_dish = false;
            active_modal = null;
            HasSelectedRecipe = false;
        }

        private void ClearAll()
        {
            current_recipe_id = null;
            trigger_context = ShortCycleTriggerContext.None;
            is_mismatch_dish = false;
            prep_zone_contents.Clear();
            placeholder_states.Clear();
            current_filter_tag = null;
            current_scroll_offset = 0f;
            current_phase = ShortCyclePhase.None;
            phase0_view = ShortCyclePhase0View.Cover;
            active_modal = null;
            HasSelectedRecipe = false;
        }
    }
}
