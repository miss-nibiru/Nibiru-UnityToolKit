using MissNibiru.Core.StateMachine;
using NUnit.Framework;

namespace MissNibiru.Core.Tests.StateMachine
{
    public sealed class StateMachineTests
    {
        private sealed class TestContext
        {
            public int EnterCount;
            public int TickCount;
            public int ExitCount;
        }

        private sealed class CountingState : IState<TestContext>
        {
            public void Enter(TestContext context)
            {
                context.EnterCount++;
            }

            public void Tick(TestContext context)
            {
                context.TickCount++;
            }

            public void Exit(TestContext context)
            {
                context.ExitCount++;
            }
        }

        [Test]
        public void ChangeState_EntersNewState()
        {
            TestContext context = new TestContext();

            StateMachine<TestContext> stateMachine =
                new StateMachine<TestContext>(context);

            CountingState state = new CountingState();

            stateMachine.ChangeState(state);

            Assert.AreSame(state, stateMachine.CurrentState);
            Assert.AreEqual(1, context.EnterCount);
        }

        [Test]
        public void Tick_TicksCurrentState()
        {
            TestContext context = new TestContext();

            StateMachine<TestContext> stateMachine =
                new StateMachine<TestContext>(context);

            stateMachine.ChangeState(new CountingState());
            stateMachine.Tick();

            Assert.AreEqual(1, context.TickCount);
        }

        [Test]
        public void ChangingState_ExitsPreviousAndEntersNext()
        {
            TestContext context = new TestContext();

            StateMachine<TestContext> stateMachine =
                new StateMachine<TestContext>(context);

            CountingState firstState = new CountingState();
            CountingState secondState = new CountingState();

            stateMachine.ChangeState(firstState);
            stateMachine.ChangeState(secondState);

            Assert.AreEqual(2, context.EnterCount);
            Assert.AreEqual(1, context.ExitCount);
            Assert.AreSame(secondState, stateMachine.CurrentState);
        }

        [Test]
        public void Clear_ExitsAndRemovesCurrentState()
        {
            TestContext context = new TestContext();

            StateMachine<TestContext> stateMachine =
                new StateMachine<TestContext>(context);

            stateMachine.ChangeState(new CountingState());
            stateMachine.Clear();

            Assert.AreEqual(1, context.ExitCount);
            Assert.IsNull(stateMachine.CurrentState);
        }
    }
}