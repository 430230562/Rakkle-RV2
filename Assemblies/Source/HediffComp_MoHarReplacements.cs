using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rakkle
{
    public class HediffCompProperties_Nullifier : HediffCompProperties
    {
        public List<HediffDef> hediffToNullify = new List<HediffDef>();
        public int limitedUsageNumber;

        public HediffCompProperties_Nullifier()
        {
            compClass = typeof(HediffComp_Nullifier);
        }
    }

    public class HediffComp_Nullifier : HediffComp
    {
        private HediffCompProperties_Nullifier CompProps => (HediffCompProperties_Nullifier)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            RemoveTargetHediffs(CompProps.limitedUsageNumber);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (CompProps.limitedUsageNumber <= 0 && Find.TickManager.TicksGame % 60 == 0)
            {
                RemoveTargetHediffs(0);
            }
        }

        private void RemoveTargetHediffs(int limit)
        {
            if (parent?.pawn?.health?.hediffSet == null || CompProps.hediffToNullify == null)
            {
                return;
            }

            List<Hediff> hediffs = new List<Hediff>(parent.pawn.health.hediffSet.hediffs);
            int removed = 0;
            foreach (Hediff hediff in hediffs)
            {
                if (!CompProps.hediffToNullify.Contains(hediff.def))
                {
                    continue;
                }

                parent.pawn.health.RemoveHediff(hediff);
                removed++;
                if (limit > 0 && removed >= limit)
                {
                    break;
                }
            }
        }
    }

    public class HediffCompProperties_Spawner : HediffCompProperties
    {
        public ThingDef thingToSpawn;
        public int spawnCount = 1;
        public float minDaysB4Next = 1f;
        public float maxDaysB4Next = 1f;
        public float graceDays;
        public int spawnMaxAdjacent = 40;
        public bool spawnForbidden;
        public string spawnVerb;

        public HediffCompProperties_Spawner()
        {
            compClass = typeof(HediffComp_Spawner);
        }
    }

    public class HediffComp_Spawner : HediffComp
    {
        private int nextSpawnTick = -1;
        private HediffCompProperties_Spawner CompProps => (HediffCompProperties_Spawner)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            ScheduleNextSpawn(Find.TickManager.TicksGame);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (parent?.pawn?.Map == null)
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            if (nextSpawnTick < 0)
            {
                ScheduleNextSpawn(currentTick);
            }

            if (currentTick >= nextSpawnTick)
            {
                SpawnThings();
                ScheduleNextSpawn(currentTick);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextSpawnTick, "nextSpawnTick", -1);
        }

        private void ScheduleNextSpawn(int currentTick)
        {
            float minDays = Mathf.Max(0.001f, CompProps.minDaysB4Next);
            float maxDays = Mathf.Max(minDays, CompProps.maxDaysB4Next);
            float delayDays = Rand.Range(minDays, maxDays) + Mathf.Max(0f, CompProps.graceDays);
            nextSpawnTick = currentTick + Mathf.RoundToInt(delayDays * GenDate.TicksPerDay);
        }

        private void SpawnThings()
        {
            Pawn pawn = parent.pawn;
            Map map = pawn.Map;
            if (CompProps.thingToSpawn == null || map == null)
            {
                return;
            }

            List<IntVec3> cells = new List<IntVec3>();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, CompProps.spawnMaxAdjacent, true))
            {
                if (cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map))
                {
                    cells.Add(cell);
                }
            }

            if (cells.Count == 0)
            {
                return;
            }

            for (int i = 0; i < CompProps.spawnCount; i++)
            {
                Thing thing = ThingMaker.MakeThing(CompProps.thingToSpawn);
                thing.SetForbidden(CompProps.spawnForbidden, false);
                IntVec3 cell = cells[Rand.Range(0, cells.Count)];
                GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
        }
    }
}