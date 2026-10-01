using RimWorld;
using Verse;

namespace Rakkle
{
    /// <summary>
    /// Projectile fired by the Rakkle engulfing tongue ability. When it hits a pawn, the muscular
    /// tongue latches on and drags the victim back next to the caster instead of dealing damage.
    /// </summary>
    public class Projectile_VoreTongue : Bullet
    {
        protected override void Impact(Thing hitThing, bool blockedByShield)
        {
            Pawn caster = Launcher as Pawn;
            Pawn prey = hitThing as Pawn;

            if (!blockedByShield && caster != null && prey != null && caster != prey && TryPull(caster, prey))
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            base.Impact(hitThing, blockedByShield);
        }

        private bool TryPull(Pawn caster, Pawn prey)
        {
            if (caster.Dead || prey.Dead || !caster.Spawned || !prey.Spawned || caster.Map != prey.Map)
            {
                return false;
            }

            Map map = caster.Map;
            IntVec3 cell = CellFinder.StandableCellNear(caster.Position, map, 2.9f, c => !c.Fogged(map));
            if (!cell.IsValid)
            {
                cell = caster.Position;
            }

            // yank the victim next to the tongue's owner
            prey.Position = cell;
            prey.Notify_Teleported(true, true);

            if (caster.Spawned && prey.Spawned)
            {
                prey.rotationTracker.FaceCell(caster.Position);
                caster.rotationTracker.FaceCell(prey.Position);
            }
            return true;
        }
    }
}

