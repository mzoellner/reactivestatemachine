﻿using System;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class ExitActionTests : AbstractReactiveStateMachineTest
    {
        IDisposable _stateChangedSubscription;

        #region parameter checks

        [Test]
        public void ThrowsIfActionIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => StateMachine.AddEntryAction(TestStates.Collapsed, null));
            Assert.Throws<ArgumentNullException>(() => StateMachine.AddEntryAction(TestStates.Collapsed, null, () => true));
            Assert.Throws<ArgumentNullException>(() => StateMachine.AddEntryAction(TestStates.Collapsed, TestStates.FadingIn, null));
            Assert.Throws<ArgumentNullException>(() => StateMachine.AddEntryAction(TestStates.Collapsed, TestStates.FadingIn, null, () => true));
        }

        [Test]
        public void ThrowsIfInternalTransition()
        {
            Assert.Throws<InvalidOperationException>(() => StateMachine.AddEntryAction(TestStates.Collapsed, TestStates.Collapsed, () => { }));
        }

        #endregion

        #region single exit action

        [Test]
        public void SingleExitActionIsCalled()
        {
            var evt = new ManualResetEvent(false);
            var exitActionCalled = false;

            Action exitAction = () => exitActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddExitAction(TestStates.Collapsed, exitAction);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(exitActionCalled);
        }

        #endregion

        #region multiple exit actions

        [Test]
        public void MultipleExitActionsAreCalled()
        {
            var evt = new ManualResetEvent(false);
            const int numExitActionsToCall = 10;
            var numExitActionsCalled = 0;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);

            Action exitAction = () => numExitActionsCalled++;

            for (int i = 0; i < numExitActionsToCall; i++)
            {
                StateMachine.AddExitAction(TestStates.Collapsed, exitAction);
            }

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.AreEqual(numExitActionsToCall, numExitActionsCalled);
        }

        #endregion

        #region exit actions in series

        [Test]
        public void ExitActionsAreCalledInSeries()
        {
            var evt = new ManualResetEvent(false);
            const int numExitActionsToCall = 4;
            var numExitActionsCalled = 0;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddAutomaticTransition(TestStates.FadingIn, TestStates.Visible);
            StateMachine.AddAutomaticTransition(TestStates.Visible, TestStates.FadingOut);
            StateMachine.AddAutomaticTransition(TestStates.FadingOut, TestStates.NotStarted);

            Action entryAction = () => numExitActionsCalled++;

            StateMachine.AddExitAction(TestStates.Collapsed, entryAction);
            StateMachine.AddExitAction(TestStates.FadingIn, entryAction);
            StateMachine.AddExitAction(TestStates.Visible, entryAction);
            StateMachine.AddExitAction(TestStates.FadingOut, entryAction);
            
            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.NotStarted).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.AreEqual(numExitActionsToCall, numExitActionsCalled);
        }

        #endregion

        #region conditional exit actions

        [Test]
        public void ConditionalExitActionIsCalled()
        {
            var evt = new ManualResetEvent(false);
            var exitActionCalled = false;

            Action exitAction = () => exitActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddExitAction(TestStates.Collapsed, exitAction, () => true);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(exitActionCalled);
        }

        [Test]
        public void ConditionalExitActionIsNotCalled()
        {
            var evt = new ManualResetEvent(false);
            var exitActionCalled = false;

            Action exitAction = () => exitActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddExitAction(TestStates.Collapsed, exitAction, () => false);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                evt.Set();
                _stateChangedSubscription.Dispose();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.False(exitActionCalled);
        }

        #endregion

        #region exit actions for specific transitions

        [Test]
        public void ExitActionIsCalledOnSpecificTransition()
        {
            var evt = new ManualResetEvent(false);
            var exitActionCalled = false;

            Action exitAction = () => exitActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddExitAction(TestStates.Collapsed, TestStates.FadingIn, exitAction);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                evt.Set();
                _stateChangedSubscription.Dispose();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(exitActionCalled);
        }

        [Test]
        public void ExitActionIsNotCalledOnOtherTransitions()
        {
            var evt = new ManualResetEvent(false);
            var exitActionCalled = false;

            Action exitAction = () => exitActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddExitAction(TestStates.Collapsed, TestStates.NotStarted, exitAction);
            StateMachine.AddExitAction(TestStates.Collapsed, TestStates.FadingOut, exitAction);
            StateMachine.AddExitAction(TestStates.Collapsed, TestStates.Visible, exitAction);


            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                evt.Set();
                _stateChangedSubscription.Dispose();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.False(exitActionCalled);
        }

        #endregion

        #region exception in exit action

        [Test]
        public void ExceptionInExitActionIsHandledAndReported()
        {
            var evt = new ManualResetEvent(false);
            var exceptionHandledAndReported = false;

            Action exitAction = () => { throw new Exception(); };

            StateMachine.AddExitAction(TestStates.Collapsed, exitAction);
            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);

            StateMachine.StateChanged += (sender, args) => evt.Set();

            StateMachine.StateMachineException += (sender, args) => exceptionHandledAndReported = true;

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(exceptionHandledAndReported);
        }

        #endregion

        #region exception in condition of exit action

        [Test]
        public void ExceptionInConditionOfExitActionIsHandledAndReported()
        {
            var evt = new ManualResetEvent(false);
            var exceptionHandledAndReported = false;

            Action exitAction = () => { };
            Func<bool> condition = () => { throw new Exception(); };

            StateMachine.AddExitAction(TestStates.Collapsed, exitAction, condition);
            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);

            StateMachine.StateChanged += (sender, args) => evt.Set();

            StateMachine.StateMachineException += (sender, args) => exceptionHandledAndReported = true;

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(exceptionHandledAndReported);
        }

        #endregion

    }
}
