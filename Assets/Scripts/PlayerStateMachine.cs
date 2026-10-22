using UnityEngine;

public enum PlayerState
{
    Normal,
    Attack
}

public class PlayerStateMachine : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; } = PlayerState.Normal;

    private float attackEndTime;

    public bool CanMove => CurrentState != PlayerState.Attack;
    public bool CanAttack => CurrentState != PlayerState.Attack;

    private void Update()
    {
        if (CurrentState == PlayerState.Attack && Time.time >= attackEndTime)
            CurrentState = PlayerState.Normal;
    }

    public void EnterAttackUntil(float endTime)
    {
        attackEndTime = endTime;
        CurrentState = PlayerState.Attack;
    }
}