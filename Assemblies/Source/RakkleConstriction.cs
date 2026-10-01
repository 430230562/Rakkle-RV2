using HarmonyLib;
using RimWorld;
using RimVore2;
using Verse;
using Verse.AI;

namespace Rakkle
{
    [DefOf]
    public static class RakkleHediffDefOf
    {
        public static HediffDef RS_ConstrictCharge;

        static RakkleHediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RakkleHediffDefOf));
        }
    }

    /// <summary>
    /// Rakkle melee attacks build up a "constriction charge" on themselves. Once the charge is full,
    /// the Rakkle automatically tries to seize the victim in a RimVore2 grapple.
    /// </summary>
    public static class ConstrictionUtility
    {
        public const string RakkleRaceDefName = "Rakkle";

        public static void OnPawnMeleeDamaged(Pawn victim, DamageInfo dinfo)
        {
            if (victim == null || dinfo.Def == null || dinfo.Def.isRanged)
            {
                return;
            }
            // only count genuine melee strikes; ranged and explosive damage carry no Tool
            if (dinfo.Tool == null)
            {
                return;
            }
            if (!(dinfo.Instigator is Pawn attacker) || attacker == victim)
            {
                return;
            }
            if (!IsRakkle(attacker) || !attacker.Spawned || !victim.Spawned)
            {
                return;
            }
            if (RakkleHediffDefOf.RS_ConstrictCharge == null)
            {
                return;
            }

            Hediff hediff = attacker.health.hediffSet.GetFirstHediffOfDef(RakkleHediffDefOf.RS_ConstrictCharge);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(RakkleHediffDefOf.RS_ConstrictCharge, attacker);
                attacker.health.AddHediff(hediff);
            }
            hediff.TryGetComp<HediffComp_ConstrictCharge>()?.NotifyHit(victim);
        }

        public static bool TryTriggerGrapple(Pawn predator, Pawn prey)
        {
            if (predator == null || prey == null || predator == prey)
            {
                return false;
            }
            if (predator.Dead || prey.Dead || !predator.Spawned || !prey.Spawned)
            {
                return false;
            }
            if (!predator.CanVore(prey, out _))
            {
                return false;
            }
            if (CombatUtility.IsInvolvedInGrapple(predator) || CombatUtility.IsInvolvedInGrapple(prey))
            {
                return false;
            }

            Job grappleJob = JobMaker.MakeJob(VoreJobDefOf.RV2_VoreGrapple, prey);
            predator.jobs.StartJob(grappleJob, JobCondition.InterruptForced);
            return true;
        }

        private static bool IsRakkle(Pawn pawn)
        {
            return pawn?.def != null && pawn.def.defName == RakkleRaceDefName;
        }
    }

    public class HediffCompProperties_ConstrictCharge : HediffCompProperties
    {
        /// <summary>Charge added to the attacker per landed melee hit.</summary>
        public float severityPerHit = 0.25f;
        /// <summary>Charge lost per in-game day while not attacking.</summary>
        public float decayPerDay = 2f;
        /// <summary>Charge required to trigger an automatic vore grapple.</summary>
        public float triggerThreshold = 1f;

        public HediffCompProperties_ConstrictCharge()
        {
            compClass = typeof(HediffComp_ConstrictCharge);
        }
    }

    public class HediffComp_ConstrictCharge : HediffComp
    {
        private Pawn lastTarget;
        private int lastHitTick = -1;

        private HediffCompProperties_ConstrictCharge Props => (HediffCompProperties_ConstrictCharge)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;
            if (lastHitTick >= 0 && now - lastHitTick > 120)
            {
                severityAdjustment -= Props.decayPerDay / GenDate.TicksPerDay;
            }

            if (parent.Severity <= 0.0001f && now - lastHitTick > 600)
            {
                pawn.health.RemoveHediff(parent);
            }
        }

        public void NotifyHit(Pawn victim)
        {
            Pawn pawn = parent?.pawn;
            if (pawn == null || pawn.Dead || victim == null)
            {
                return;
            }

            // switching targets resets the wind-up
            if (lastTarget != null && lastTarget != victim)
            {
                parent.Severity = 0f;
            }

            lastTarget = victim;
            lastHitTick = Find.TickManager.TicksGame;
            parent.Severity = MathfMin(Props.triggerThreshold, parent.Severity + Props.severityPerHit);

            if (parent.Severity >= Props.triggerThreshold)
            {
                ConstrictionUtility.TryTriggerGrapple(pawn, victim);
                pawn.health.RemoveHediff(parent);
            }
        }

        private static float MathfMin(float a, float b) => a < b ? a : b;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref lastTarget, "RS_lastConstrictTarget");
            Scribe_Values.Look(ref lastHitTick, "RS_lastConstrictHitTick", -1);
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage))]
    public static class Patch_Pawn_PostApplyDamage
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            ConstrictionUtility.OnPawnMeleeDamaged(__instance, dinfo);
        }
    }

    [StaticConstructorOnStartup]
    public static class RakkleHarmonyInit
    {
        static RakkleHarmonyInit()
        {
            Harmony harmony = new Harmony("jkviolet.rakkle.rv2combat");
            harmony.PatchAll();
            Log.Message("[Rakkle-RV2] Applied vore combat harmony patches.");
        }
    }
}
