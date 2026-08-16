using MissNibiru.Enemies.Actor;

namespace MissNibiru.Enemies.Attacks
{
    public interface IEnemyAttackBehaviour
    {
        void Initialize(EnemyContext context);
        void Activate();
        void Tick(float deltaTime);
        void Deactivate();
    }
}