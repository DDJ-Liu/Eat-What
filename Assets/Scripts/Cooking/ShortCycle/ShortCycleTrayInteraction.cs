using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using EatWhat.Toolkit;
using TMPro;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>Stable per-unit identity for this runtime-only P1 reservation.</summary>
    public static class ShortCycleTrayIdentity
    {
        public static string ForUnit(string itemId, int index)
        {
            if (string.IsNullOrEmpty(itemId) || index < 0) throw new ArgumentException("A real item and unit index are required.");
            using (var hash = MD5.Create())
                return new Guid(hash.ComputeHash(Encoding.UTF8.GetBytes(itemId + ":" + index.ToString(CultureInfo.InvariantCulture)))).ToString("N");
        }

        public static string ForStack(string itemId) { return ForUnit(itemId + ":stack", 0); }
    }

    /// <summary>Atomic P1 reservation; real inventory changes only at DataProcessor.</summary>
    public static class ShortCycleTrayReservation
    {
        public const string CapacityFullKey = "ui.cook.tray_full";

        public static ShortCycleActionResult TryTake(ShortCycleContext context, string itemId,
            string requestedInstanceId, string stackType, int owned, int capacity)
        {
            if (context == null || context.current_phase != ShortCyclePhase.Phase1 ||
                context.prep_zone_contents == null || context.placeholder_states == null)
                return ShortCycleActionResult.Failure("A Phase1 reservation context is required.");
            if (string.IsNullOrEmpty(itemId) || owned <= 0 || capacity <= 0)
                return ShortCycleActionResult.Failure("A positive owned quantity and configured prep capacity are required.");
            StackedIconShape shape;
            if (!StackedIcon.TryParseShape(stackType, out shape))
                return ShortCycleActionResult.Failure("Unsupported Items.stack_type: " + stackType);

            var reserved = 0;
            foreach (var state in context.placeholder_states)
            {
                if (state == null || state.taken_count < 0 || string.IsNullOrEmpty(state.instance_id))
                    return ShortCycleActionResult.Failure("Malformed placeholder identity or quantity.");
                if (state.item_id == itemId) reserved += state.GetCommittedQuantity();
            }
            if (reserved >= owned) return ShortCycleActionResult.Failure("No unreserved inventory remains for " + itemId + ".");

            var id = shape == StackedIconShape.Stack ? ShortCycleTrayIdentity.ForStack(itemId) : null;
            if (shape == StackedIconShape.Stack && !string.IsNullOrEmpty(requestedInstanceId) && requestedInstanceId != id)
                return ShortCycleActionResult.Failure("Stack instance identity does not match the source slot.");
            if (shape != StackedIconShape.Stack)
            {
                if (!string.IsNullOrEmpty(requestedInstanceId))
                {
                    var found = false;
                    for (var index = 0; index < owned; index++)
                        if (ShortCycleTrayIdentity.ForUnit(itemId, index) == requestedInstanceId) { found = true; break; }
                    if (!found) return ShortCycleActionResult.Failure("Unit instance identity does not belong to the owned item.");
                    id = requestedInstanceId;
                }
                else
                {
                    for (var index = 0; index < owned; index++)
                    {
                        var candidate = ShortCycleTrayIdentity.ForUnit(itemId, index);
                        if (FindState(context, candidate) == null) { id = candidate; break; }
                    }
                }
                if (id == null || FindState(context, id) != null)
                    return ShortCycleActionResult.Failure("Unit instance is already reserved or unavailable.");
            }

            var priorState = FindState(context, id);
            var priorPrep = FindPrep(context, id);
            if ((priorState == null) != (priorPrep == null) ||
                (priorState != null && (shape != StackedIconShape.Stack || priorState.item_id != itemId ||
                    priorState.stack_type != stackType || priorPrep.item_id != itemId ||
                    priorPrep.quantity != priorState.GetCommittedQuantity())))
                return ShortCycleActionResult.Failure("Prep and placeholder identities disagree.");
            if (priorPrep == null && context.prep_zone_contents.Count >= capacity)
                return ShortCycleActionResult.Failure(CapacityFullKey);

            if (priorState != null)
            {
                priorState.taken_count++;
                priorState.is_taken = true;
                priorPrep.quantity++;
            }
            else
            {
                context.placeholder_states.Add(new PlaceholderState {
                    instance_id = id, item_id = itemId, stack_type = stackType, taken_count = 1, is_taken = true });
                context.prep_zone_contents.Add(new PrepZoneItem {
                    instance_id = id, item_id = itemId, quantity = 1,
                    drag_order = context.prep_zone_contents.Count, state = "prepared" });
            }
            return ShortCycleActionResult.Success();
        }

        public static ShortCycleActionResult TryPutBack(ShortCycleContext context, string itemId, string instanceId)
        {
            if (context == null || context.current_phase != ShortCyclePhase.Phase1 ||
                context.prep_zone_contents == null || context.placeholder_states == null)
                return ShortCycleActionResult.Failure("A Phase1 reservation context is required.");
            PrepZoneItem prep = null;
            if (!string.IsNullOrEmpty(instanceId)) prep = FindPrep(context, instanceId);
            else if (!string.IsNullOrEmpty(itemId))
                for (var index = context.prep_zone_contents.Count - 1; index >= 0; index--)
                    if (context.prep_zone_contents[index] != null && context.prep_zone_contents[index].item_id == itemId)
                    { prep = context.prep_zone_contents[index]; break; }
            if (prep == null || string.IsNullOrEmpty(prep.instance_id) ||
                (!string.IsNullOrEmpty(itemId) && prep.item_id != itemId))
                return ShortCycleActionResult.Failure("No matching reserved item instance can be returned.");
            var state = FindState(context, prep.instance_id);
            if (state == null || state.item_id != prep.item_id || prep.quantity <= 0 ||
                state.taken_count != prep.quantity || !state.is_taken)
                return ShortCycleActionResult.Failure("Prep and placeholder quantities disagree.");
            if (state.stack_type == "stack" && prep.quantity > 1)
            {
                state.taken_count--;
                prep.quantity--;
            }
            else
            {
                context.prep_zone_contents.Remove(prep);
                context.placeholder_states.Remove(state);
                for (var index = 0; index < context.prep_zone_contents.Count; index++)
                    context.prep_zone_contents[index].drag_order = index;
            }
            return ShortCycleActionResult.Success();
        }

        private static PlaceholderState FindState(ShortCycleContext context, string id)
        {
            foreach (var state in context.placeholder_states)
                if (state != null && state.instance_id == id) return state;
            return null;
        }

        private static PrepZoneItem FindPrep(ShortCycleContext context, string id)
        {
            foreach (var item in context.prep_zone_contents)
                if (item != null && item.instance_id == id) return item;
            return null;
        }
    }

    /// <summary>Uses the twenty authored TraySlot anchors without moving them.</summary>
    [DisallowMultipleComponent]
    public sealed class ShortCycleTrayInteraction : MonoBehaviour
    {
        [SerializeField] private TraySlot[] slots = new TraySlot[0];
        [SerializeField] private TMP_Text capacityText = null;
        [SerializeField] private ShortCycleTrayStateController trayStateController = null;
        public ShortCycleTrayStateController StateController { get { return trayStateController; } }
        public string LastDiagnostic { get; private set; }
        public int BoundCount { get; private set; }

        public bool CanPresent(int capacity, out string diagnostic)
        {
            var targets = slots ?? new TraySlot[0];
            if (capacity <= 0 || targets.Length < capacity || capacityText == null || trayStateController == null)
            {
                diagnostic = "Tray needs configured capacity, authored slots, capacity TMP and state controller.";
                return false;
            }
            for (var index = 0; index < capacity; index++)
                if (targets[index] == null) { diagnostic = "Tray slot " + index + " is not connected."; return false; }
            diagnostic = null;
            return true;
        }

        public bool Refresh(ShortCycleContext context, IShortCycleInventoryQuery inventory, int capacity)
        {
            string diagnostic;
            if (!CanPresent(capacity, out diagnostic) || context == null || inventory == null ||
                context.prep_zone_contents == null || context.prep_zone_contents.Count > capacity)
            {
                LastDiagnostic = diagnostic ?? "Tray context or inventory query is unavailable.";
                return false;
            }
            var items = new CK01ItemData[context.prep_zone_contents.Count];
            for (var index = 0; index < items.Length; index++)
            {
                var prep = context.prep_zone_contents[index];
                if (prep == null || string.IsNullOrEmpty(prep.instance_id) ||
                    !inventory.TryGetGeneratedItem(prep.item_id, out items[index]))
                {
                    LastDiagnostic = "Tray item " + index + " cannot be resolved by InventoryManager.";
                    return false;
                }
            }
            for (var index = 0; index < capacity; index++)
            {
                var occupied = index < items.Length;
                slots[index].gameObject.SetActive(occupied);
                if (occupied)
                {
                    var prep = context.prep_zone_contents[index];
                    slots[index].ConfigureItem(items[index], prep.quantity, prep.item_id, prep.instance_id);
                }
            }
            capacityText.text = items.Length.ToString(CultureInfo.InvariantCulture) + "/" +
                capacity.ToString(CultureInfo.InvariantCulture);
            BoundCount = items.Length;
            LastDiagnostic = null;
            return true;
        }
    }
}
