using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimVore2;
using UnityEngine;
using Verse;

namespace Rakkle
{
    /// <summary>
    /// A mental break that can be forced when the dependency reaches its maximum severity.
    /// </summary>
    public class VoreDependencyMentalBreak
    {
        public MentalStateDef mentalState;
        public float weight = 1f;
    }

    /// <summary>
    /// "Predation dependency": the pawn's engineered metabolism expects to digest live prey.
    /// Every completed digestion stage lowers the dependency by 0.5 per point of prey body size;
    /// go too long without a meal and the hediff severity climbs, weakening the pawn.
    /// </summary>
    public class HediffCompProperties_VoreDependency : HediffCompProperties
    {
        /// <summary>
        /// Part goals that count as "digestion completed" the moment their stage ends.
        /// The default "Digest" part goal is the stomach digestion stage.
        /// </summary>
        public List<string> feedingPartGoals = new List<string>() { "Digest" };
        /// <summary>
        /// Severity removed per point of prey body size when a digestion stage finishes.
        /// A normal human-sized prey (body size 1) removes 0.5 by default.
        /// </summary>
        public float severityReductionPerBodySize = 0.5f;
        /// <summary>Days without feeding before the pawn reaches the hediff's max severity.</summary>
        public float starvationDays = 1.5f;
        /// <summary>
        /// Mental breaks forced once the dependency hits its max severity. One is picked by weight,
        /// among those that currently have valid targets.
        /// </summary>
        public List<VoreDependencyMentalBreak> maxSeverityMentalBreaks = new List<VoreDependencyMentalBreak>();

        public HediffCompProperties_VoreDependency()
        {
            compClass = typeof(HediffComp_VoreDependency);
        }
    }

    public class HediffComp_VoreDependency : HediffComp
    {
        private const int CheckIntervalTicks = 250;
        private const int MentalBreakCheckIntervalTicks = 2500;

        private int lastFedTick = -1;
        private int lastCheckTick = -1;
        private int lastMentalBreakCheckTick = -1;
        private bool forcedMentalBreakTriggered;

        private HediffCompProperties_VoreDependency Props => (HediffCompProperties_VoreDependency)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            lastFedTick = Find.TickManager.TicksGame;
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

            float starvationTicks = Mathf.Max(1f, Props.starvationDays * GenDate.TicksPerDay);
            float progress = Mathf.Clamp01((now - lastFedTick) / starvationTicks);
            parent.Severity = SeverityForProgress(progress);
            UpdateForcedMentalBreak(pawn);
        }

        /// <summary>
        /// Snaps the pawn once the dependency bottoms out at its max severity. The flag resets
        /// after the pawn eats, so a single starvation episode causes a single break.
        /// </summary>
        private void UpdateForcedMentalBreak(Pawn pawn)
        {
            if (Props.maxSeverityMentalBreaks.NullOrEmpty())
            {
                return;
            }
            if (parent.Severity < MaxSeverity - 0.0001f)
            {
                forcedMentalBreakTriggered = false;
                return;
            }
            if (forcedMentalBreakTriggered || pawn.InMentalState || pawn.mindState?.mentalStateHandler == null)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            // finding valid prey is a map wide scan, so do not retry too often
            if (lastMentalBreakCheckTick >= 0 && now - lastMentalBreakCheckTick < MentalBreakCheckIntervalTicks)
            {
                return;
            }
            lastMentalBreakCheckTick = now;
            MentalStateDef breakDef = PickMentalBreak(pawn);
            if (breakDef == null)
            {
                // no valid targets right now, try again on the next check
                return;
            }
            if (pawn.mindState.mentalStateHandler.TryStartMentalState(breakDef, parent.def.label, forceWake: true))
            {
                forcedMentalBreakTriggered = true;
            }
        }

        private MentalStateDef PickMentalBreak(Pawn pawn)
        {
            float totalWeight = 0f;
            foreach (VoreDependencyMentalBreak entry in Props.maxSeverityMentalBreaks)
            {
                if (IsMentalBreakAvailable(entry, pawn))
                {
                    totalWeight += entry.weight;
                }
            }
            if (totalWeight <= 0f)
            {
                return null;
            }
            float roll = Rand.Value * totalWeight;
            MentalStateDef lastValid = null;
            foreach (VoreDependencyMentalBreak entry in Props.maxSeverityMentalBreaks)
            {
                if (!IsMentalBreakAvailable(entry, pawn))
                {
                    continue;
                }
                lastValid = entry.mentalState;
                roll -= entry.weight;
                if (roll <= 0f)
                {
                    return entry.mentalState;
                }
            }
            return lastValid;
        }

        private bool IsMentalBreakAvailable(VoreDependencyMentalBreak entry, Pawn pawn)
        {
            return entry != null
                && entry.mentalState != null
                && entry.weight > 0f
                && entry.mentalState.Worker.StateCanOccur(pawn);
        }

        /// <summary>
        /// Called whenever one of the predator's vore stages ends. Digestion progress is consumed
        /// when the digestion stage finishes, not when the whole vore is resolved.
        /// </summary>
        public void NotifyStageCompleted(VoreStageDef stage, float preyBodySize)
        {
            if (!IsFeedingStage(stage))
            {
                return;
            }
            float reduction = Props.severityReductionPerBodySize * Mathf.Max(0f, preyBodySize);
            ReduceSeverity(reduction);
        }

        private bool IsFeedingStage(VoreStageDef stage)
        {
            PartGoalDef partGoal = stage?.partGoal;
            if (partGoal == null || Props.feedingPartGoals.NullOrEmpty())
            {
                return false;
            }
            return Props.feedingPartGoals.Contains(partGoal.defName);
        }

        // severity runs from the hediff's initialSeverity (sated) up to its maxSeverity (starved)
        private float MinSeverity => parent.def.initialSeverity;
        private float MaxSeverity => Mathf.Max(MinSeverity, parent.def.maxSeverity);

        private float SeverityForProgress(float progress)
        {
            return Mathf.Lerp(MinSeverity, MaxSeverity, Mathf.Clamp01(progress));
        }

        private float ProgressForSeverity(float severity)
        {
            if (MaxSeverity <= MinSeverity)
            {
                return 0f;
            }
            return Mathf.Clamp01((severity - MinSeverity) / (MaxSeverity - MinSeverity));
        }

        /// <summary>
        /// Lowers the dependency severity by the given amount, shifting the "last fed" timestamp
        /// so the time based severity calculation stays consistent.
        /// </summary>
        private void ReduceSeverity(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }
            int now = Find.TickManager.TicksGame;
            if (lastFedTick < 0)
            {
                lastFedTick = now;
            }
            float starvationTicks = Mathf.Max(1f, Props.starvationDays * GenDate.TicksPerDay);
            float currentProgress = Mathf.Clamp01((now - lastFedTick) / starvationTicks);
            float currentSeverity = SeverityForProgress(currentProgress);
            float nextSeverity = Mathf.Max(MinSeverity, currentSeverity - amount);
            lastFedTick = now - Mathf.RoundToInt(ProgressForSeverity(nextSeverity) * starvationTicks);
            lastCheckTick = now;
            parent.Severity = nextSeverity;
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref lastFedTick, "RS_lastFedTick", -1);
            Scribe_Values.Look(ref forcedMentalBreakTriggered, "RS_forcedMentalBreak", false);
        }
    }

    public static class VoreDependencyUtility
    {
        public static void NotifyStageCompleted(Pawn predator, VoreStageDef stage, float preyBodySize)
        {
            List<Hediff> hediffs = predator?.health?.hediffSet?.hediffs;
            if (hediffs.NullOrEmpty() || stage == null)
            {
                return;
            }
            for (int i = 0; i < hediffs.Count; i++)
            {
                hediffs[i].TryGetComp<HediffComp_VoreDependency>()?.NotifyStageCompleted(stage, preyBodySize);
            }
        }
    }

    /// <summary>
    /// Consumes predation dependency when a digestion stage ends, instead of waiting for the
    /// whole vore to be resolved (churn, processing and dumping can take a long time afterwards).
    /// </summary>
    [HarmonyPatch(typeof(VoreStage), nameof(VoreStage.End))]
    public static class Patch_VoreStage_End
    {
        static void Postfix(VoreStage __instance, VoreTrackerRecord record)
        {
            if (record == null)
            {
                return;
            }
            float preyBodySize = record.Prey?.BodySize ?? 1f;
            VoreDependencyUtility.NotifyStageCompleted(record.Predator, __instance?.def, preyBodySize);
        }
    }
}
