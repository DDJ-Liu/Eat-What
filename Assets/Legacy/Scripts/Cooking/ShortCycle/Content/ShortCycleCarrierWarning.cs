using System;
using System.Collections.Generic;

namespace EatWhat.Cooking.ShortCycle
{
    public interface IShortCycleCarrierWarningLog
    {
        void LogWarning(string message);
    }

    /// <summary>Session-scoped diagnostic only; never changes the chosen carrier.</summary>
    public sealed class ShortCycleCarrierWarning
    {
        private readonly IShortCycleCarrierWarningLog log;
        private readonly HashSet<Tuple<string, string, string>> reported =
            new HashSet<Tuple<string, string, string>>();

        public ShortCycleCarrierWarning(IShortCycleCarrierWarningLog log)
        {
            this.log = log;
        }

        public void Report(string recipeId, string areaId, string itemId,
            ShortCycleCarrierResolution resolution)
        {
            if (log == null || resolution == null || !resolution.IsAmbiguous ||
                !reported.Add(Tuple.Create(recipeId, areaId, itemId))) return;
            log.LogWarning("[CK01-S02] 多候选承载物：recipe=" + recipeId + " area=" + areaId +
                " item=" + itemId + " candidates=[" + string.Join(",", resolution.Candidates) +
                "] → 取白名单首条 " + resolution.CarrierId +
                "（配置缺陷：请收敛 T1.whitelist_proc_ids）");
        }

        public void ResetSession()
        {
            reported.Clear();
        }
    }
}
