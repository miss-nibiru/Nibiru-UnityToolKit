namespace MissNibiru.Enemies.Projectiles
{
    public interface IProjectileEmitter
    {
        bool TryEmit(
            ProjectileSpawnRequest request);
    }
}