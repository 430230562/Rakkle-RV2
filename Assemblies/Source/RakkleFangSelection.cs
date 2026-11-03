using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Rakkle
{
    /// <summary>
    /// Base game sorts melee verbs into DPS tiers: verbs within 95% of the strongest are "best"
    /// and share 75% of attacks, everything above 25% of the strongest is "mid" and shares 25%.
    /// The venomous fangs are a light, low DPS bite, so they always landed in the large "mid" tier
    /// next to both fists and were picked for roughly 8% of swings - and since a Rakkle grapples
    /// after only four landed hits, the fangs were almost never seen in practice.
    /// Like RimVore-2 does for the grapple verb, give the fang verb a fixed selection weight so it
    /// is used as often as the tail regardless of the tier math.
    /// </summary>
    public static class FangSelection
    {
        public const string FangToolCapacityDefName = "RS_AnestheticBiteTool";

        /// <summary>
        /// Fixed selection weight of the fang verb. Relative to the tail (0.75) and both fists
        /// (0.125 each) this works out to roughly a third of all melee attacks.
        /// </summary>
        public const float FangSelectionWeight = 0.6f;

        private static ToolCapacityDef fangCapacity;
        private static bool fangCapacityResolved;

        public static ToolCapacityDef FangCapacity
        {
            get
            {
                if (!fangCapacityResolved)
                {
                    fangCapacityResolved = true;
                    fangCapacity = DefDatabase<ToolCapacityDef>.GetNamedSilentFail(FangToolCapacityDefName);
                    if (fangCapacity == null)
                    {
                        Log.Warning("[Rakkle-RV2] Could not find tool capacity " + FangToolCapacityDefName
                            + ", the venomous fangs will fall back to vanilla weapon selection.");
                    }
                }
                return fangCapacity;
            }
        }

        public static bool IsFangTool(Tool tool)
        {
            List<ToolCapacityDef> capacities = tool?.capacities;
            ToolCapacityDef fang = FangCapacity;
            return fang != null && capacities != null && capacities.Contains(fang);
        }
    }

    [HarmonyPatch(typeof(VerbEntry))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(Verb), typeof(Pawn), typeof(List<Verb>), typeof(float) })]
    public static class Patch_VerbEntry_FangSelection
    {
        static void Postfix(VerbEntry __instance, ref float ___cachedSelectionWeight)
        {
            if (!FangSelection.IsFangTool(__instance.verb?.tool))
            {
                return;
            }
            ___cachedSelectionWeight = FangSelection.FangSelectionWeight;
        }
    }
}
