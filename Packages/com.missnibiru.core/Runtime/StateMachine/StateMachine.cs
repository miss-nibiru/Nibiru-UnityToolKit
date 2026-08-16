using System;

namespace MissNibiru.Core.StateMachine
{
    public sealed class StateMachine<TContext>
    {
        private readonly TContext _context;

        public IState<TContext> CurrentState { get; private set; }
        public event Action<IState<TContext>, IState<TContext>> StateChanged;

        public StateMachine(TContext context)
        {
            this._context = context;
        }

        public void ChangeState(IState<TContext> newState)
        {
            if (newState == null)
            {
                throw new ArgumentNullException(nameof(newState));
            }

            IState<TContext> previousState = CurrentState;
            previousState?.Exit(_context);

            CurrentState = newState;
            CurrentState.Enter(_context);

            StateChanged?.Invoke(previousState, CurrentState);
        }

        public void Tick()
        {
            CurrentState?.Tick(_context);
        }

        public void Clear()
        {
            IState<TContext> previousState = CurrentState;

            previousState?.Exit(_context);
            CurrentState = null;

            if (previousState != null)
            {
                StateChanged?.Invoke(previousState, null);
            }
        }
    }
}