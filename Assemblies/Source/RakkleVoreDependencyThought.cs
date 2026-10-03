using RimWorld;
using Verse;

namespace Rakkle
{
    public class ThoughtWorker_VoreDependency : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn pawn)
        {
            HediffDef dependencyDef = DefDatabase<HediffDef>.GetNamedSilentFail("RS_VoreDependency");
            Hediff dependency = dependencyDef == null
                ? null
                : pawn.health?.hediffSet?.GetFirstHediffOfDef(dependencyDef);

            if (dependency == null || dependency.Severity < 0.2f)
            {
                return ThoughtState.Inactive;
            }

            int stage = dependency.Severity >= 0.85f ? 2 : dependency.Severity >= 0.55f ? 1 : 0;
            return ThoughtState.ActiveAtStage(stage);
        }
    }
}