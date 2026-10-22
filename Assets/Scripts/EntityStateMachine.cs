using UnityEngine;

public enum EntityState
{
    Idle,
    Chase,
    Attack,
    Dead
}

public class EntityStateMachine : MonoBehaviour
{
    public EntityState CurrentState { get; private set; } = EntityState.Idle;

    public void SetState(EntityState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
    }

    public bool IsDead => CurrentState == EntityState.Dead;
}