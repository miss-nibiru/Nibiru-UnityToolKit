namespace MissNibiru.Combat.Projectiles

{
    public interface IProjectileEmitter
    {
        bool TryEmit(
            ProjectileSpawnRequest request);
    }
}