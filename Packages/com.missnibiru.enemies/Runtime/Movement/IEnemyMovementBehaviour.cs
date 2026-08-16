using MissNibiru.Enemies.Actor;

namespace MissNibiru.Enemies.Movement
{
    public interface IEnemyMovementBehaviour
    {
        void Initialize(EnemyContext context);
        void Activate();
        void Tick(float deltaTime);
        void Deactivate();
    }
}