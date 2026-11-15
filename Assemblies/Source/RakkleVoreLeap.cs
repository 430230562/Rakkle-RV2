using RimWorld;
using RimVore2;
using Verse;

namespace Rakkle
{
    public class CompProperties_AbilityEffect_VoreLeap : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityEffect_VoreLeap()
        {
            compClass = typeof(CompAbilityEffect_VoreLeap);
        }
    }

    public class CompAbilityEffect_VoreLeap : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            return base.Valid(target, throwMessages) && CanTargetPrey(target.Pawn);
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            return CanTargetPrey(target.Pawn);
        }

        public void OnJumpCompleted(IntVec3 destination, LocalTargetInfo target)
        {
            ConstrictionUtility.TryTriggerGrapple(parent?.pawn, target.Pawn);
        }

        private bool CanTargetPrey(Pawn prey)
        {
            Pawn predator = parent?.pawn;
            return predator != null
                && prey != null
                && predator != prey
                && predator.Spawned
                && prey.Spawned
                && predator.Map == prey.Map
                && predator.CanVore(prey, out _);
        }
    }
}
