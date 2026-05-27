using System.Collections.Generic;
using System;
using UnityEngine;

public abstract class StateMachine<EState> : MonoBehaviour where EState : Enum
{
    protected Dictionary<EState, BaseState<EState>> States = new();
    public BaseState<EState> CurrentState { get; protected set; }

    private EState queuedState;
    private bool hasQueuedState = false;

    protected virtual void Start()
    {
        if (CurrentState == null)
            return;

        CurrentState.EnterState();
    }

    protected virtual void Update()
    {
        if (CurrentState == null)
            return;

        CurrentState.UpdateState();

        if (hasQueuedState)
        {
            TransitionToState(queuedState);
            hasQueuedState = false;
        }
    }

    protected void TransitionToState(EState stateKey)
    {
        if (!States.TryGetValue(stateKey, out BaseState<EState> nextState))
            return;

        if (CurrentState != null && CurrentState.StateKey.Equals(stateKey)) return;

        CurrentState?.ExitState();
        CurrentState = nextState;
        CurrentState.EnterState();
    }

    public void QueueNextState(EState stateKey)
    {
        queuedState = stateKey;
        hasQueuedState = true;
    }
}
