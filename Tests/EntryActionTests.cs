﻿using System;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class EntryActionTests : AbstractReactiveStateMachineTest
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

        #region single entry action

        [Test]
        public void SingleEntryActionIsCalled()
        {
            var evt = new ManualResetEvent(false);
            var entryActionCalled = false;

            Action entryAction = () => entryActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddEntryAction(TestStates.FadingIn, entryAction);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(entryActionCalled);
        }

        #endregion

        #region multiple entry actions

        [Test]
        public void MultipleEntryActionsAreCalled()
        {
            var evt = new ManualResetEvent(false);
            const int numEntryActionsToCall = 10;
            var numEntryActionsCalled = 0;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);

            Action entryAction = () => numEntryActionsCalled++;

            for (int i = 0; i < numEntryActionsToCall; i++)
            {
                StateMachine.AddEntryAction(TestStates.FadingIn, entryAction);
            }

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.AreEqual(numEntryActionsToCall, numEntryActionsCalled);
        }

        #endregion

        #region entry actions in series

        [Test]
        public void EntryActionsAreCalledInSeries()
        {
            var evt = new ManualResetEvent(false);
            const int numEntryActionsToCall = 5;
            var numEntryActionsCalled = 0;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddAutomaticTransition(TestStates.FadingIn, TestStates.Visible);
            StateMachine.AddAutomaticTransition(TestStates.Visible, TestStates.FadingOut);
            StateMachine.AddAutomaticTransition(TestStates.FadingOut, TestStates.NotStarted);

            Action entryAction = () => numEntryActionsCalled++;

            StateMachine.AddEntryAction(TestStates.Collapsed, entryAction);
            StateMachine.AddEntryAction(TestStates.FadingIn, entryAction);
            StateMachine.AddEntryAction(TestStates.Visible, entryAction);
            StateMachine.AddEntryAction(TestStates.FadingOut, entryAction);
            StateMachine.AddEntryAction(TestStates.NotStarted, entryAction);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.NotStarted).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.AreEqual(numEntryActionsToCall, numEntryActionsCalled);
        }

        #endregion

        #region conditional entry actions

        [Test]
        public void ConditionalEntryActionIsCalled()
        {
            var evt = new ManualResetEvent(false);
            var entryActionCalled = false;

            Action entryAction = () => entryActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddEntryAction(TestStates.FadingIn, entryAction, () => true);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                _stateChangedSubscription.Dispose();
                evt.Set();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(entryActionCalled);
        }

        [Test]
        public void ConditionalEntryActionIsNotCalled()
        {
            var evt = new ManualResetEvent(false);
            var entryActionCalled = false;

            Action entryAction = () => entryActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddEntryAction(TestStates.FadingIn, entryAction, () => false);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                evt.Set();
                _stateChangedSubscription.Dispose();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.False(entryActionCalled);
        }

        #endregion

        #region entry actions for specific transitions

        [Test]
        public void EntryActionIsCalledOnSpecificTransition()
        {
            var evt = new ManualResetEvent(false);
            var entryActionCalled = false;

            Action entryAction = () => entryActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddEntryAction(TestStates.FadingIn, TestStates.Collapsed, entryAction);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                evt.Set();
                _stateChangedSubscription.Dispose();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(entryActionCalled);
        }

        [Test]
        public void EntryActionIsNotCalledOnOtherTransitions()
        {
            var evt = new ManualResetEvent(false);
            var entryActionCalled = false;

            Action entryAction = () => entryActionCalled = true;

            StateMachine.AddAutomaticTransition(TestStates.Collapsed, TestStates.FadingIn);
            StateMachine.AddEntryAction(TestStates.FadingIn, TestStates.NotStarted, entryAction);
            StateMachine.AddEntryAction(TestStates.FadingIn, TestStates.FadingOut, entryAction);
            StateMachine.AddEntryAction(TestStates.FadingIn, TestStates.Visible, entryAction);

            _stateChangedSubscription = StateChanged.Where(args => args.ToState == TestStates.FadingIn).Subscribe(args =>
            {
                evt.Set();
                _stateChangedSubscription.Dispose();
            });

            StateMachine.Start();

            evt.WaitOne();

            Assert.False(entryActionCalled);
        }

        #endregion

        #region entry action on start state

        [Test]
        public void EntryActionIsCalledOnStartState()
        {
            var evt = new ManualResetEvent(false);
            var entryActionCalled = false;

            Action entryAction = () => entryActionCalled = true;

            StateMachine.AddEntryAction(TestStates.Collapsed, entryAction);

            StateMachine.StateMachineStarted += (sender, args) => evt.Set();

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(entryActionCalled);
        }

        #endregion

        #region exception in entry action

        [Test]
        public void ExceptionInEntryActionIsHandledAndReported()
        {
            var evt = new ManualResetEvent(false);
            var exceptionHandledAndReported = false;

            Action entryAction = () => { throw new Exception(); };

            StateMachine.AddEntryAction(TestStates.Collapsed, entryAction);

            StateMachine.StateMachineStarted += (sender, args) => evt.Set();

            StateMachine.StateMachineException += (sender, args) => exceptionHandledAndReported = true;

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(exceptionHandledAndReported);
        }

        #endregion

        #region exception in condition of entry action

        [Test]
        public void ExceptionInConditionOfEntryActionIsHandledAndReported()
        {
            var evt = new ManualResetEvent(false);
            var exceptionHandledAndReported = false;

            Action entryAction = () => { };
            Func<bool> condition = () =>{throw new Exception();};

            StateMachine.AddEntryAction(TestStates.Collapsed, entryAction, condition);

            StateMachine.StateMachineStarted += (sender, args) => evt.Set();

            StateMachine.StateMachineException += (sender, args) => exceptionHandledAndReported = true;

            StateMachine.Start();

            evt.WaitOne();

            Assert.True(exceptionHandledAndReported);
        }

        #endregion

    }
}
