using UnityEngine;
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

            if (dependency == null)
            {
                return ThoughtState.Inactive;
            }

            // The first hediff stage is "sated"; map every later stage onto a mood stage.
            int dependencyStage = dependency.CurStageIndex;
            int thoughtStages = def.stages.Count;
            if (dependencyStage <= 0 || thoughtStages <= 0)
            {
                return ThoughtState.Inactive;
            }
            return ThoughtState.ActiveAtStage(Mathf.Min(dependencyStage - 1, thoughtStages - 1));
        }
    }
}