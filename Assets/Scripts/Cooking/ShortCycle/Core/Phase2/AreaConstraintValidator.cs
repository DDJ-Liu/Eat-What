using System;
using System.Collections.Generic;
namespace EatWhat.Cooking.ShortCycle
{
    public enum ProcessingRejectionCode
    { None, InvalidRequest, ToolActionUnsupported, ActionNotAllowed, CarrierNotAllowed, CarrierUnavailable, NoWhitelistedMatchInCurrentArea }

    /// <summary>Checks the existing global AreaConstraint snapshot; it grants no tools or areas.</summary>
    public static class AreaConstraintValidator
    {
        public static ProcessingRejectionCode Check(AreaConstraint area, string action, string carrier, IEnumerable<string> currentAreaCarriers)
        {
            if (area == null) return ProcessingRejectionCode.InvalidRequest;
            if (!area.AllowedActions.Contains(action)) return ProcessingRejectionCode.ActionNotAllowed;
            if (string.IsNullOrEmpty(carrier)) return ProcessingRejectionCode.None;
            if (!area.AllowedCarriers.Contains(carrier)) return ProcessingRejectionCode.CarrierNotAllowed;
            return new HashSet<string>(currentAreaCarriers ?? new string[0], StringComparer.Ordinal).Contains(carrier)
                ? ProcessingRejectionCode.None : ProcessingRejectionCode.CarrierUnavailable;
        }
    }
}
