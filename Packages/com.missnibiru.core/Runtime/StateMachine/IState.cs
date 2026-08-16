namespace MissNibiru.Core.StateMachine
{
    public interface IState<in TContext> // this marks that depending on the context of the game is the state machine it will start creating
    {
        void Enter(TContext context);

        void Tick(TContext context);

        void Exit(TContext context);
    }
}