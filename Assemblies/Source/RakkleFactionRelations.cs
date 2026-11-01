using HarmonyLib;
using RimWorld;

namespace Rakkle
{
    /// <summary>
    /// The Cradle slavers are permanent enemies of a Rakkle player colony, but only
    /// ordinary -80 relations (which can still be improved) towards everyone else.
    /// </summary>
    public static class CradleRelations
    {
        public const string CradleDefName = "Cradle";
        public const string RakklePlayerFactionDefName = "RS_PlayerFaction";
        public const int InitialGoodwill = -80;
        public const int PermanentGoodwill = -100;

        public static bool IsCradle(Faction faction)
        {
            return faction?.def?.defName == CradleDefName;
        }

        public static bool IsCradlePair(Faction first, Faction second)
        {
            if (first == null || second == null || first == second)
            {
                return false;
            }
            return IsCradle(first) || IsCradle(second);
        }

        public static bool IsRakklePlayerPair(Faction first, Faction second)
        {
            if (!IsCradlePair(first, second))
            {
                return false;
            }
            Faction other = IsCradle(first) ? second : first;
            return other.def?.defName == RakklePlayerFactionDefName;
        }
    }

    [HarmonyPatch(typeof(Faction), nameof(Faction.TryMakeInitialRelationsWith))]
    public static class Patch_Faction_TryMakeInitialRelationsWith
    {
        static void Postfix(Faction __instance, Faction other)
        {
            if (!CradleRelations.IsCradlePair(__instance, other))
            {
                return;
            }
            bool permanent = CradleRelations.IsRakklePlayerPair(__instance, other);
            int goodwill = permanent ? CradleRelations.PermanentGoodwill : CradleRelations.InitialGoodwill;
            FactionRelationKind kind = goodwill <= -10 ? FactionRelationKind.Hostile : FactionRelationKind.Neutral;
            SetRelation(__instance, other, goodwill, kind);
        }

        private static void SetRelation(Faction first, Faction second, int goodwill, FactionRelationKind kind)
        {
            FactionRelation relation = first.RelationWith(second, true);
            if (relation != null)
            {
                relation.baseGoodwill = goodwill;
                relation.kind = kind;
            }
            FactionRelation otherRelation = second.RelationWith(first, true);
            if (otherRelation != null)
            {
                otherRelation.baseGoodwill = goodwill;
                otherRelation.kind = kind;
            }
        }
    }

    [HarmonyPatch(typeof(FactionDef), nameof(FactionDef.PermanentlyHostileTo))]
    public static class Patch_FactionDef_PermanentlyHostileTo
    {
        static void Postfix(FactionDef __instance, FactionDef otherFactionDef, ref bool __result)
        {
            if (__result || __instance?.defName != CradleRelations.CradleDefName)
            {
                return;
            }
            if (otherFactionDef?.defName == CradleRelations.RakklePlayerFactionDefName)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(Faction), nameof(Faction.CanChangeGoodwillFor))]
    public static class Patch_Faction_CanChangeGoodwillFor
    {
        static bool Prefix(Faction __instance, Faction other, ref bool __result)
        {
            if (!CradleRelations.IsRakklePlayerPair(__instance, other))
            {
                return true;
            }
            __result = false;
            return false;
        }
    }
}
