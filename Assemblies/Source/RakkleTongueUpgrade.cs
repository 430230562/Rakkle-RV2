using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimVore2;
using Verse;

namespace Rakkle
{
    /// <summary>
    /// Grants (and upgrades) the predatory leap ability once the pawn has digested enough prey.
    /// Attach to an always-present Rakkle hediff. Thresholds and abilities are read from XML so the
    /// progression can be tuned without touching code.
    /// </summary>
    public class HediffCompProperties_VoreTongueUpgrade : HediffCompProperties
    {
        /// <summary>Cumulative prey body size required for each tier, matched by index with abilitiesToGrant.</summary>
        public List<float> upgradeThresholds = new List<float>();
        /// <summary>The ability granted at each tier. Only the highest earned tier is kept.</summary>
        public List<AbilityDef> abilitiesToGrant = new List<AbilityDef>();

        public HediffCompProperties_VoreTongueUpgrade()
        {
            compClass = typeof(HediffComp_VoreTongueUpgrade);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (upgradeThresholds.NullOrEmpty() || abilitiesToGrant.NullOrEmpty())
            {
                yield return $"{nameof(HediffCompProperties_VoreTongueUpgrade)} on {parentDef.defName}: upgradeThresholds or abilitiesToGrant is empty";
            }
            else if (upgradeThresholds.Count != abilitiesToGrant.Count)
            {
                yield return $"{nameof(HediffCompProperties_VoreTongueUpgrade)} on {parentDef.defName}: upgradeThresholds and abilitiesToGrant must have the same length";
            }
            for (int i = 0; i < upgradeThresholds.Count; i++)
            {
                if (upgradeThresholds[i] <= 0f || (i > 0 && upgradeThresholds[i] <= upgradeThresholds[i - 1]))
                {
                    yield return $"{nameof(HediffCompProperties_VoreTongueUpgrade)} on {parentDef.defName}: upgradeThresholds must be positive and strictly increasing";
                    break;
                }
            }
        }
    }

    public class HediffComp_VoreTongueUpgrade : HediffComp
    {
        private const int CheckIntervalTicks = 250;

        private int lastCheckTick = -1;
        private float digestedPreyBodySize;

        private HediffCompProperties_VoreTongueUpgrade Props => (HediffCompProperties_VoreTongueUpgrade)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            InitializeNaturalProgress();
            TryCheckForUpgrade();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            int now = Find.TickManager.TicksGame;
            if (lastCheckTick >= 0 && now - lastCheckTick < CheckIntervalTicks)
            {
                return;
            }
            lastCheckTick = now;
            TryCheckForUpgrade();
        }

        private void TryCheckForUpgrade()
        {
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead || pawn.abilities == null)
            {
                return;
            }
            if (Props.abilitiesToGrant.NullOrEmpty() || Props.upgradeThresholds.NullOrEmpty())
            {
                return;
            }

            int earnedTier = -1;
            for (int i = 0; i < Props.abilitiesToGrant.Count && i < Props.upgradeThresholds.Count; i++)
            {
                if (digestedPreyBodySize >= Props.upgradeThresholds[i])
                {
                    earnedTier = i;
                }
            }
            if (earnedTier < 0)
            {
                return;
            }

            for (int i = 0; i < Props.abilitiesToGrant.Count; i++)
            {
                AbilityDef ability = Props.abilitiesToGrant[i];
                if (ability == null)
                {
                    continue;
                }

                bool shouldHave = i == earnedTier;
                bool has = pawn.abilities.GetAbility(ability, true) != null;

                if (shouldHave && !has)
                {
                    pawn.abilities.GainAbility(ability);
                    Messages.Message("RS_TongueUnlocked".Translate(pawn.LabelShort, ability.label), MessageTypeDefOf.PositiveEvent, false);
                }
                else if (!shouldHave && has)
                {
                    // a higher tier has been earned, replace the weaker tongue
                    pawn.abilities.RemoveAbility(ability);
                }
            }
        }

        private void InitializeNaturalProgress()
        {
            float roll = Rand.Value;
            if (roll < 0.30f)
            {
                digestedPreyBodySize = Rand.Range(0f, 8f);
            }
            else if (roll < 0.40f)
            {
                digestedPreyBodySize = Rand.Range(8f, 17f);
            }
            else if (roll < 0.45f)
            {
                digestedPreyBodySize = 17f;
            }
        }

        public void RegisterDigestedPrey(Pawn prey)
        {
            if (prey == null)
            {
                return;
            }

            digestedPreyBodySize += prey.BodySize;
            TryCheckForUpgrade();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref digestedPreyBodySize, "digestedPreyBodySize", 0f);
        }
    }

    [HarmonyPatch(typeof(PostVoreUtility), nameof(PostVoreUtility.ResolveVore))]
    public static class Patch_PostVoreUtility_ResolveVore_TongueGrowth
    {
        [HarmonyPostfix]
        public static void Postfix(VoreTrackerRecord record)
        {
            if (record == null
                || record.IsInterrupted
                || record.VoreGoal == null
                || !record.VoreGoal.IsLethal
                || record.VoreGoal.defName == "Reform"
                || record.Predator == null
                || record.Prey == null
                || record.Predator.def?.defName != ConstrictionUtility.RakkleRaceDefName)
            {
                return;
            }

            HediffDef growthDef = DefDatabase<HediffDef>.GetNamed("Hediff_VoreTongueGrowth");
            Hediff growth = record.Predator.health.hediffSet.GetFirstHediffOfDef(growthDef);
            if (growth == null)
            {
                growth = HediffMaker.MakeHediff(growthDef, record.Predator);
                record.Predator.health.AddHediff(growth);
            }

            growth.TryGetComp<HediffComp_VoreTongueUpgrade>()?.RegisterDigestedPrey(record.Prey);
        }
    }
}
