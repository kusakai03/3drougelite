using System;
using UnityEngine;

[Flags]
public enum BlessingStackReset
{
    None = 0,
    OnConditionFalse = 1 << 0,
    OnPlayerDeath = 1 << 1
}
[CreateAssetMenu(menuName = "Game/Blessing")]
public class BlessingData : ItemData
{
    public BlessingTrigger trigger;
    public BlessingCondition condition;
    public int maxStacks = 1;
    public BonusStats bonusPerStack = new();
    public BlessingStackReset stackReset;

    private void OnValidate()
    {
        itemType = ItemType.Blessing;
    }
}

[Serializable]
public class BlessingInstance
{
    public string instanceID;
    public BlessingData data;
    public int stacks;

    public BlessingInstance(BlessingData data)
    {
        instanceID = Guid.NewGuid().ToString();
        this.data = data;

        stacks = data.trigger == BlessingTrigger.Passive ? 1 : 0;
    }
}
public enum BlessingTrigger
{
    Passive,
    OnHit,
    OnKill,
    OnTakeDamage,
    OnRoomStart
}

public enum BlessingCondition
{
    None,
    HpBelow50,
    HpBelow30,
    TargetIsBoss
}