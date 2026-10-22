using UnityEngine;

public abstract class EnemyAttack
{
    protected EntityAgent owner;
    protected CombatInstance target;

    protected EnemyAttack(EntityAgent owner)
    {
        this.owner = owner;
    }

    public abstract void StartAttack(CombatInstance target);
    public abstract void UpdateAttack();
    public abstract bool IsFinished();
}