using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rakkle
{
    /// <summary>
    /// Grants (and upgrades) the engulfing tongue ability once the pawn has digested enough prey.
    /// Attach to an always-present Rakkle hediff. Thresholds and abilities are read from XML so the
    /// progression can be tuned without touching code.
    /// </summary>
    public class HediffCompProperties_VoreTongueUpgrade : HediffCompProperties
    {
        /// <summary>RV2 record that counts how many prey this pawn has fully digested (RV2_Goal_Digest_Predator).</summary>
        public RecordDef digestionRecord;
        /// <summary>Digested prey required for each tier, matched by index with abilitiesToGrant.</summary>
        public List<int> upgradeThresholds = new List<int>();
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
        }
    }

    public class HediffComp_VoreTongueUpgrade : HediffComp
    {
        private const int CheckIntervalTicks = 250;

        private int lastCheckTick = -1;

        private HediffCompProperties_VoreTongueUpgrade Props => (HediffCompProperties_VoreTongueUpgrade)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
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
            if (pawn == null || pawn.Dead || pawn.abilities == null || pawn.records == null || Props.digestionRecord == null)
            {
                return;
            }
            if (Props.abilitiesToGrant.NullOrEmpty() || Props.upgradeThresholds.NullOrEmpty())
            {
                return;
            }

            float digested = pawn.records.GetValue(Props.digestionRecord);
            int earnedTier = -1;
            for (int i = 0; i < Props.abilitiesToGrant.Count && i < Props.upgradeThresholds.Count; i++)
            {
                if (digested >= Props.upgradeThresholds[i])
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
    }
}
