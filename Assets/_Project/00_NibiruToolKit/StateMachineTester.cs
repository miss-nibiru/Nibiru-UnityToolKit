using MissNibiru.Core.StateMachine;
using UnityEngine;

public sealed class StateMachineTester : MonoBehaviour
{
    private StateMachine<StateMachineTester> _stateMachine;

    private IdleState _idleState;
    private ActiveState _activeState;

    private void Awake()
    {
        _stateMachine =
            new StateMachine<StateMachineTester>(this);

        _idleState = new IdleState();
        _activeState = new ActiveState();

        _stateMachine.StateChanged += HandleStateChanged;
        _stateMachine.ChangeState(_idleState);
    }

    private void OnDestroy()
    {
        if (_stateMachine != null)
        {
            _stateMachine.StateChanged -= HandleStateChanged;
            _stateMachine.Clear();
        }
    }

    [ContextMenu("Test/Switch To Idle")]
    private void SwitchToIdle()
    {
        _stateMachine?.ChangeState(_idleState);
    }

    [ContextMenu("Test/Switch To Active")]
    private void SwitchToActive()
    {
        _stateMachine?.ChangeState(_activeState);
    }

    [ContextMenu("Test/Tick Current State")]
    private void TickCurrentState()
    {
        _stateMachine?.Tick();
    }

    [ContextMenu("Test/Clear State Machine")]
    private void ClearStateMachine()
    {
        _stateMachine?.Clear();
    }

    private void HandleStateChanged(
        IState<StateMachineTester> previousState,
        IState<StateMachineTester> currentState)
    {
        string previousName =
            previousState?.GetType().Name ?? "None";

        string currentName =
            currentState?.GetType().Name ?? "None";

        Debug.Log(
            $"State changed: {previousName} → {currentName}");
    }

    private sealed class IdleState :
        IState<StateMachineTester>
    {
        public void Enter(StateMachineTester context)
        {
            Debug.Log("Entered Idle State");
        }

        public void Tick(StateMachineTester context)
        {
            Debug.Log("Ticked Idle State");
        }

        public void Exit(StateMachineTester context)
        {
            Debug.Log("Exited Idle State");
        }
    }

    private sealed class ActiveState :
        IState<StateMachineTester>
    {
        public void Enter(StateMachineTester context)
        {
            Debug.Log("Entered Active State");
        }

        public void Tick(StateMachineTester context)
        {
            Debug.Log("Ticked Active State");
        }

        public void Exit(StateMachineTester context)
        {
            Debug.Log("Exited Active State");
        }
    }
}