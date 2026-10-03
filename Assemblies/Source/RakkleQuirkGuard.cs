using System.Collections.Generic;
using HarmonyLib;
using RimVore2;
using RimWorld;
using Verse;

namespace Rakkle
{
    /// <summary>
    /// The Rakkle's predator toolkit is engineered into them, so a randomly rolled negative vore-capability
    /// quirk would undermine what they were built for. This "floor" keeps a Rakkle from ever acquiring the
    /// negative quirks of the pools that drive predator ability. It works by refusing those quirks in
    /// RimVore2's QuirkDef.IsValid, which is the single gate every random roll, ideology replacement and
    /// post-init add passes through.
    /// </summary>
    public static class RakkleQuirkGuard
    {
        public const string RakkleRaceDefName = "Rakkle";

        // Negative quirks from the predator-capability pools: grapple, swallow speed, digestion strength,
        // persuasiveness, keeping prey down, storage capacity, mobility and nutrition extraction.
        private static readonly HashSet<string> BlockedQuirks = new HashSet<string>
        {
            "GrappleStrength_Bad",
            "GrappleStrength_VeryBad",
            "SwallowSpeed_Bad",
            "SwallowSpeed_VeryBad",
            "DigestionStrength_Bad",
            "DigestionStrength_VeryBad",
            "Persuasiveness_Bad",
            "Persuasiveness_VeryBad",
            "StruggleStrength_Predator_Bad",
            "StruggleStrength_Predator_VeryBad",
            "StorageCapacity_Bad",
            "StorageCapacity_VeryBad",
            "Mobility_Bad",
            "Mobility_VeryBad",
            "NutritionEfficiency_Bad",
            "NutritionEfficiency_VeryBad",
        };

        public static bool ShouldBlock(QuirkDef quirk, Pawn pawn)
        {
            if (quirk == null || pawn?.def == null)
            {
                return false;
            }
            if (!BlockedQuirks.Contains(quirk.defName))
            {
                return false;
            }
            return pawn.def.defName == RakkleRaceDefName;
        }
    }

    [StaticConstructorOnStartup]
    public static class RakkleQuirkGuardInit
    {
        static RakkleQuirkGuardInit()
        {
            // match the exact override signature: IsValid(Pawn, out string, List<TraitDef>, List<QuirkDef>, List<string>)
            System.Reflection.MethodBase target = AccessTools.Method(typeof(QuirkDef), "IsValid", new[]
            {
                typeof(Pawn),
                typeof(string).MakeByRefType(),
                typeof(List<TraitDef>),
                typeof(List<QuirkDef>),
                typeof(List<string>)
            });
            if (target == null)
            {
                Log.Error("[Rakkle-RV2] Could not find QuirkDef.IsValid, the Rakkle quirk guard is disabled.");
                return;
            }
            Harmony harmony = new Harmony("jkviolet.rakkle.rv2quirkguard");
            harmony.Patch(target, prefix: new HarmonyMethod(AccessTools.Method(typeof(RakkleQuirkGuardInit), nameof(GuardNegativeVoreQuirks))));
            Log.Message("[Rakkle-RV2] Applied the Rakkle quirk guard patch.");
        }

        public static bool GuardNegativeVoreQuirks(QuirkDef __instance, Pawn pawn, ref string reason, ref bool __result)
        {
            if (!RakkleQuirkGuard.ShouldBlock(__instance, pawn))
            {
                return true;
            }
            reason = "Rakkle physiology rejects this quirk";
            __result = false;
            return false;
        }
    }
}
