using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rakkle
{
    /// <summary>
    /// "Predation dependency": the pawn's engineered metabolism expects to digest live prey.
    /// While it keeps feeding (its RV2 feeding records keep increasing) nothing happens; go too long
    /// without a meal and the hediff severity climbs, weakening the pawn.
    /// </summary>
    public class HediffCompProperties_VoreDependency : HediffCompProperties
    {
        /// <summary>RV2 records that are summed to detect that the pawn has recently fed on prey.</summary>
        public List<RecordDef> feedingRecords = new List<RecordDef>();
        /// <summary>Days without feeding before the pawn is fully starved (severity 1).</summary>
        public float starvationDays = 1.5f;

        public HediffCompProperties_VoreDependency()
        {
            compClass = typeof(HediffComp_VoreDependency);
        }
    }

    public class HediffComp_VoreDependency : HediffComp
    {
        private const int CheckIntervalTicks = 250;

        private float lastRecordTotal = -1f;
        private int lastFedTick = -1;
        private int lastCheckTick = -1;

        private HediffCompProperties_VoreDependency Props => (HediffCompProperties_VoreDependency)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            lastFedTick = Find.TickManager.TicksGame;
            lastRecordTotal = CurrentRecordTotal();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead || !pawn.Spawned)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (lastFedTick < 0)
            {
                lastFedTick = now;
            }
            if (lastCheckTick >= 0 && now - lastCheckTick < CheckIntervalTicks)
            {
                return;
            }
            lastCheckTick = now;

            float total = CurrentRecordTotal();
            if (lastRecordTotal < 0f)
            {
                lastRecordTotal = total;
            }
            if (total > lastRecordTotal)
            {
                // the pawn digested something, it is sated again
                lastRecordTotal = total;
                lastFedTick = now;
            }

            float starvationTicks = Mathf.Max(1f, Props.starvationDays * GenDate.TicksPerDay);
            float severity = Mathf.Clamp01((now - lastFedTick) / starvationTicks);
            // keep a sliver of severity so the hediff is never treated as fully gone
            parent.Severity = Mathf.Max(0.001f, severity);
        }

        private float CurrentRecordTotal()
        {
            Pawn pawn = parent?.pawn;
            if (pawn?.records == null || Props.feedingRecords.NullOrEmpty())
            {
                return 0f;
            }

            float total = 0f;
            foreach (RecordDef record in Props.feedingRecords)
            {
                if (record != null)
                {
                    total += pawn.records.GetValue(record);
                }
            }
            return total;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastRecordTotal, "RS_lastFeedingTotal", -1f);
            Scribe_Values.Look(ref lastFedTick, "RS_lastFedTick", -1);
        }
    }
}
